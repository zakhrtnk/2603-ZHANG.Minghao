using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarMove1 : MonoBehaviour
{
    public string fileName = "intersection_02_traj_veh_filtered.csv";
    public int targetId = 0;

    private List<VehicleData> dataset = new List<VehicleData>();
    private int currentFrame = 1;
    private int maxFrame = 0;

    private Vector3 currentPosition;
    private Vector3 nextPosition;
    public static bool carIsFinished = false;
    public static bool needReset = false;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 24;
        string path = Path.Combine(Application.streamingAssetsPath, fileName); ;
        dataset = LoadCSV(path);
        foreach (var d in dataset)
        {
            if (d.frame > maxFrame)
                maxFrame = d.frame;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {
        if (needReset)
        {
            ResetScene();
        }
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

