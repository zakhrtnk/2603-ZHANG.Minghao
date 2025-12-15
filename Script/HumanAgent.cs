using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.IO;

public class HumanAgent : Agent
{
    public string fileName = "intersection_01_traj_ped_filtered.csv";   //ファイル名
    public int targetId = 0;    //歩行者ID

    private List<PedestrianData> dataset = new List<PedestrianData>();
    private int currentFrame = 1;   //現在のフレーム
    private int maxFrame = 0;   //再生時最大フレーム数

    private Vector3 currentPosition;    //現在フレームの座標
    private Vector3 nextPosition;   //次のフレームの座標
    //次のフレームの座標と現在フレームの座標の差から向く方向を計算するため

    private Rigidbody agentRb;  // 物理挙動用のRigidbody
    private Transform targetTransform;  // ターゲットの位置
    //位置や状態を読み取るため

    public static bool humanIsFinished = false; // CSV再生完了フラグ
    //別のスクリプトの引数を同時に扱うため

    [Header("Mode")]
    public bool forceCsvFacing = false;   //CSVファイル再生時
    public bool manualControl = false;    //手動及び訓練時

    [Header("Movement Settings")]
    [Tooltip("速度")]
    public float moveSpeed = 2.0f;
    [Tooltip("角速度")]
    public float turnSpeedDeg = 540f;
    [Range(0f, 1f), Tooltip("向き調整")]
    public float faceSlerp = 0.2f;

    private float lastDistanceToTarget = 0f;

    [Header("Reward Settings")]
    public bool distanceReward_switch = false;
    public float distanceRewardScale = 0.002f;


    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();    

        if (agentRb != null)
        {
            agentRb.interpolation = RigidbodyInterpolation.Interpolate; //見た目を滑らかに
            agentRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;  //倒れ防止
        }

        //24FPSに固定
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 24;

        //CSV読み込み
        string path = Path.Combine(Application.streamingAssetsPath, fileName);  
        dataset = LoadCSV(path);

        //最大フレームを取得
        foreach (var d in dataset)
            if (d.frame > maxFrame) maxFrame = d.frame;

        //ターゲット取得（ターゲットとの距離を計算するため）
        var targetGO = GameObject.FindGameObjectWithTag("Target"); 
        if (targetGO) targetTransform = targetGO.transform; 
        else Debug.LogWarning("HumanAgent: can't find the object named Tag=Target");
    }

    private void Update()
    {
    }

    public override void OnEpisodeBegin()
    {
        //歩行者が終わったことを車に通知するための引数
        humanIsFinished = false;

        //車両のリセットフラグ
        CarMove.needReset = true;
        CarMove1.needReset = true;
        CarMove2.needReset = true;
        CarMove3.needReset = true;



        currentFrame = 1;

        //1フレーム目と2フレーム目の位置を取得（差を取ることで、次の座標に向かう向きを算出するため）
        PedestrianData d1 = dataset.Find(row => row.frame == currentFrame && row.id == targetId);
        PedestrianData d2 = dataset.Find(row => row.frame == (currentFrame + 1) && row.id == targetId);

        currentPosition = (d1 != null) ? new Vector3(d1.x, 0.0f, -d1.y) : Vector3.zero;
        nextPosition = (d2 != null) ? new Vector3(d2.x, 0.0f, -d2.y) : currentPosition;

        transform.localPosition = currentPosition;

        //向き調整
        if (forceCsvFacing && currentPosition != nextPosition)
        {
            Vector3 dir = nextPosition - currentPosition;
            if (dir.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        //スピードリセット
        if (agentRb != null)
        {
            agentRb.velocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
        }

        //距離の報酬（必要な時にオンオフができる）
        if (distanceReward_switch && targetTransform)
        {
            lastDistanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (humanIsFinished)
        {
            EndEpisode();
            return;
        }

        //前進後退
        float throttle = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        float turn = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        float forwardSpeed = throttle * moveSpeed;
        float yawSpeedDeg = turn * turnSpeedDeg;
        float dt = Time.deltaTime;

        //向き更新
        if (!forceCsvFacing)
        {
            Quaternion deltaRot = Quaternion.Euler(0f, yawSpeedDeg * dt, 0f);
            transform.rotation = deltaRot * transform.rotation;
        }
        else
        {
            //次フレーム方向へスムーズに向く（CSVファイルを読み取るだけでは細かい振動が存在する）
            if (currentPosition != nextPosition)
            {
                Vector3 dir = (nextPosition - currentPosition);
                if (dir.sqrMagnitude > 1e-6f)
                {
                    Quaternion look = Quaternion.LookRotation(dir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, faceSlerp);
                }
            }
        }

        //前進
        Vector3 delta = transform.forward * forwardSpeed * dt;
        transform.localPosition += delta;

        if (transform.localPosition.y < -1f)
        {
            SetReward(-1.0f);
            EndEpisode();
            return;
        }

        //距離の報酬
        if (distanceReward_switch && targetTransform)
        {
            float dist = Vector3.Distance(transform.position, targetTransform.position);
            float deltad = (lastDistanceToTarget - dist);
            SetReward(deltad * distanceRewardScale);
            lastDistanceToTarget = dist;
        }

        //时间ペナルティ
        AddReward(-1f / MaxStep);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //速度角速度向きの観察
        if (agentRb != null)
            sensor.AddObservation(transform.InverseTransformDirection(agentRb.velocity));
        else
            sensor.AddObservation(Vector3.zero);

        //回転の観察
        sensor.AddObservation(transform.forward); //3
        sensor.AddObservation(transform.right);   //3

        //CSV座標追跡の観察
        Vector3 toNext = (nextPosition - transform.localPosition);
        sensor.AddObservation(toNext); //3
    }

    //CSVで読み取ったデータを手動モードで出力する
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.ContinuousActions;

        if (manualControl)
        {
            float throttle = 0f;
            float turn = 0f;
            if (Input.GetKey(KeyCode.W)) throttle += 1f;
            if (Input.GetKey(KeyCode.S)) throttle -= 1f;
            if (Input.GetKey(KeyCode.A)) turn -= 1f;
            if (Input.GetKey(KeyCode.D)) turn += 1f;

            a[0] = Mathf.Clamp(throttle, -1f, 1f);
            a[1] = Mathf.Clamp(turn, -1f, 1f);
            return;
        }

        //CSV再生モード
        if (currentFrame >= maxFrame){ 
            humanIsFinished = true; 
        }
        
        currentFrame++;

        if (currentFrame < maxFrame)
        {
            PedestrianData d1 = dataset.Find(row => row.frame == currentFrame && row.id == targetId);
            PedestrianData d2 = dataset.Find(row => row.frame == (currentFrame + 1) && row.id == targetId);

            if (d1 != null) currentPosition = new Vector3(d1.x, 0f, -d1.y);
            nextPosition = (d2 != null) ? new Vector3(d2.x, 0f, -d2.y) : currentPosition;
        }
        else
        {
            nextPosition = currentPosition;
        }

        Vector3 dir = (nextPosition - currentPosition);
        float throttleCsv = 0f;
        float turnCsv = 0f;
        if (dir.sqrMagnitude > 1e-6f)
        {
            Vector3 localDir = transform.InverseTransformDirection(dir.normalized);
            throttleCsv = Mathf.Clamp(localDir.z, -1f, 1f);
            turnCsv = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z), -1f, 1f);
        }

        a[0] = throttleCsv;
        a[1] = turnCsv;
    }

    //ゴールに触れたら報酬を与える
    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            SetReward(2.0f);
            EndEpisode();
        }
    }

    //障害物に接触したらペナルティを与える
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    //CSV読み込み関数
    List<PedestrianData> LoadCSV(string filePath)
    {
        var dataList = new List<PedestrianData>();
        using (var reader = new StreamReader(filePath))
        {
            bool isFirstLine = true;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (isFirstLine) { isFirstLine = false; continue; }

                var values = line.Split(',');
                var data = new PedestrianData()
                {
                    id = int.Parse(values[0]),
                    frame = int.Parse(values[1]),
                    label = values[2],
                    x = float.Parse(values[3]),
                    y = float.Parse(values[4]),
                    vx = float.Parse(values[5]),
                    vy = float.Parse(values[6])
                };
                dataList.Add(data);
            }
        }
        return dataList;
    }
}

//歩行者データ構造
public class PedestrianData
{
    public int id;
    public int frame;
    public string label;
    public float x;
    public float y;
    public float vx;
    public float vy;
}