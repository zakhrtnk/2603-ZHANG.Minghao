using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;

//二つの軌跡の平均類似度を計算するため
public class HumanAgentLogger : MonoBehaviour
{
    [Header("Agent to Track")]
    public HumanAgent agent;

    private List<string> logLines = new List<string>();
    private int stepCount = 0;
    private string filePath;

    //ファイルの保存場所
    private string customSaveDir = @"C:\unity project\config";

    void Start()
    {

        if (!Directory.Exists(customSaveDir))
        {
            Directory.CreateDirectory(customSaveDir);
            Debug.Log($"Created directory: {customSaveDir}");
        }


        string time = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(customSaveDir, $"HumanAgent_{time}.csv");

        //        logLines.Add("step,x,z");
        Debug.Log($"Logging to: {filePath}");
    }

    void Update()
    {
        if (agent == null) return;

        //Agentの座標の取得
        Vector3 pos = agent.transform.position;

        //xとy座標の記録
        string line = $"{stepCount},{pos.x.ToString(CultureInfo.InvariantCulture)},{pos.z.ToString(CultureInfo.InvariantCulture)}";
        logLines.Add(line);
        stepCount++;
    }

    void OnApplicationQuit()
    {
        SaveCSV();
    }

    public void SaveCSV()
    {
        if (logLines.Count == 0) return;

        try
        {
            File.WriteAllLines(filePath, logLines);
            Debug.Log($"HumanAgent log saved to: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save log: {ex.Message}");
        }
    }
}
