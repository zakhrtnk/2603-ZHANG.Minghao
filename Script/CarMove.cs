using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarMove : MonoBehaviour
{
    public string fileName = "intersection_02_traj_veh_filtered.csv";   //ファイル名
    public int targetId = 0;    //車のID

    private List<VehicleData> dataset = new List<VehicleData>();
    private int currentFrame = 1;   //現在のフレーム
    private int maxFrame = 0;   //再生時最大フレーム数

    private Vector3 currentPosition;    //現在フレームの座標
    private Vector3 nextPosition;   //次のフレームの座標
    public static bool carIsFinished = false;   //車の終了フラグ
    public static bool needReset = false;   //HumanAgentから受けたリセット指令と合わせてリセットするかを判断

    private void Awake()
    {
        //フレームレートを24FPSに固定
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 24;
        //CSV読み込み
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        dataset = LoadCSV(path);
        foreach (var d in dataset)
        {
            if (d.frame > maxFrame)
                maxFrame = d.frame;
        }
    }
    
    //Awakeの後、一度だけ呼ばれる
    void Start()
    {
        //車が終わったことを全体に通知するための引数
        carIsFinished = false;
        currentFrame = 1;
        VehicleData d1 = dataset.Find(row => row.frame == currentFrame && row.id == targetId);
        VehicleData d2 = dataset.Find(row => row.frame == (currentFrame + 1) && row.id == targetId);
        if (d1 != null)
        {
            currentPosition = new Vector3(d1.x, 0.0f, -d1.y);
        }
        else
        {
            currentPosition = Vector3.zero;
        }
        if (d2 != null)
        {
            nextPosition = new Vector3(d1.x, 0.0f, -d2.y);
        }
        else
        {
            nextPosition = currentPosition;
        }
        transform.localPosition = currentPosition;
        if (currentPosition != nextPosition)
        {
            Vector3 direction = nextPosition - currentPosition;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation * Quaternion.Euler(0, 0, 0);
        }
    }

    //毎フレーム呼ばれる
    void Update()
    {
        //HumanAgentからリセット要求が来ていたら、最初の状態に戻す
        if (needReset)
        {
            ResetScene();
        }

        //現在フレームが最後のフレームなら終了と宣言する
        if (currentFrame == maxFrame)
        {
            carIsFinished = true;
        }
        currentFrame++;
        if (currentFrame < maxFrame)
        {
            VehicleData d1 = dataset.Find(row => row.frame == currentFrame && row.id == targetId);
            VehicleData d2 = dataset.Find(row => row.frame == (currentFrame + 1) && row.id == targetId);
            if (d1 != null)
            {
                currentPosition = new Vector3(d1.x, 0.0f, -d1.y);
            }
            if (d2 != null)
            {
                nextPosition = new Vector3(d2.x, 0.0f, -d2.y);
            }
            else
            {
                nextPosition = currentPosition;
            }
        }
        else
        {
            nextPosition = currentPosition;
        }

        float moveX = nextPosition.x - currentPosition.x;
        float moveZ = nextPosition.z - currentPosition.z;
        Vector3 moveVec = new Vector3(moveX, 0, moveZ);
        

        if (currentPosition != nextPosition)
        {
            Vector3 direction = nextPosition - currentPosition;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation * Quaternion.Euler(0, 0, 0);
        }
        transform.localPosition += moveVec;
    }
    //エピソード開始時などに車を初期状態に戻す
    void ResetScene()
    {
        carIsFinished = false;
        needReset = false;
        currentFrame = 1;
        VehicleData d1 = dataset.Find(row => row.frame == currentFrame && row.id == targetId);
        VehicleData d2 = dataset.Find(row => row.frame == (currentFrame + 1) && row.id == targetId);
        if (d1 != null)
        {
            currentPosition = new Vector3(d1.x, 0.0f, -d1.y);
        }
        else
        {
            currentPosition = Vector3.zero;
        }
        if (d2 != null)
        {
            nextPosition = new Vector3(d2.x, 0.0f, -d2.y);
        }
        else
        {
            nextPosition = currentPosition;
        }
        transform.localPosition = currentPosition;
        if (currentPosition != nextPosition)
        {
            Vector3 direction = nextPosition - currentPosition;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation * Quaternion.Euler(0, 0, 0);
        }
    }

    //CSV読み込み関数
    List<VehicleData> LoadCSV(string filePath)
    {
        List<VehicleData> dataList = new List<VehicleData>();

        using (var reader = new StreamReader(filePath))
        {
            bool isFirstLine = true;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                var values = line.Split(',');
                VehicleData data = new VehicleData()
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


//車のデータ構造
public class VehicleData
{
    public int id;
    public int frame;
    public string label;
    public float x;
    public float y;
    public float vx;
    public float vy;
}