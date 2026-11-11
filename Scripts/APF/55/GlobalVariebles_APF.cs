using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class GlobalVariebles_APF : MonoBehaviour
{
    public List<chaser_APF> chasers;  // 5个chaser
    public List<runner_APF> runners;  // 5个runner
    public static int RunnerGetCaughtNum;
    public static bool IsGameOver;

    private List<string> episodeData = new List<string>();
    private List<string> summaryData = new List<string>();

    private int currentEpisode = 0;
    private bool isEpisodeActive = false;

    private int chaserObstacleHitCount = 0;
    private int chaserDroneHitCount = 0;
    private int runnerObstacleHitCount = 0;
    private int runnerDroneHitCount = 0;

    public float episodeTime = 30f;
    private float currentEpisodeTime = 0f;

    private int totalRuns = 0;
    public int maxRuns = 1000;

    private string basePath = @"C:\Users\63510\Desktop\Data_PE\5V5\APF_Results";

    void Start()
    {
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        episodeData.Add("Episode,EpisodeTime,CatchRate,ChaserObstacleRate,ChaserDroneRate,RunnerObstacleRate,RunnerDroneRate");
        summaryData.Add("Category,Value");

        StartNewEpisode();
    }

    void Update()
    {
        if (!isEpisodeActive) return;

        currentEpisodeTime += Time.deltaTime;

        if (currentEpisodeTime >= episodeTime || CheckAllRunnersCaught() || IsGameOver)
        {
            EndCurrentEpisode();

            if (totalRuns < maxRuns)
            {
                StartCoroutine(StartNewEpisodeDelayed(1f));
            }
            else
            {
                ExportFinalStatistics();
                Debug.Log($"已完成 {maxRuns} 次5v5 APF运行");
            }
        }
    }

    IEnumerator StartNewEpisodeDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNewEpisode();
    }

    void StartNewEpisode()
    {
        currentEpisode++;
        totalRuns++;
        currentEpisodeTime = 0f;
        isEpisodeActive = true;

        RunnerGetCaughtNum = 0;
        IsGameOver = false;
        chaserObstacleHitCount = 0;
        chaserDroneHitCount = 0;
        runnerObstacleHitCount = 0;
        runnerDroneHitCount = 0;

        ResetAllAgents();

        Debug.Log($"开始5v5 APF回合 {currentEpisode} ({totalRuns}/{maxRuns})");
    }

    void EndCurrentEpisode()
    {
        isEpisodeActive = false;
        RecordEpisodeData();
    }

    bool CheckAllRunnersCaught()
    {
        if (runners == null || runners.Count == 0) return false;

        foreach (var runner in runners)
        {
            if (runner != null && !runner.isCaught)
                return false;
        }
        return true;
    }

    public void RecordChaserObstacleCollision()
    {
        chaserObstacleHitCount++;
    }

    public void RecordChaserDroneCollision()
    {
        chaserDroneHitCount++;
    }

    public void RecordRunnerObstacleCollision()
    {
        runnerObstacleHitCount++;
    }

    public void RecordRunnerDroneCollision()
    {
        runnerDroneHitCount++;
    }

    public void ResetAllAgents()
    {
        foreach (var chaser in chasers)
        {
            if (chaser != null)
                chaser.ResetEpisode();
        }

        foreach (var runner in runners)
        {
            if (runner != null)
                runner.ResetEpisode();
        }
    }

    private void RecordEpisodeData()
    {
        // 5v5：每个被抓的runner贡献1/5=0.2
        float catchRate = ((float)RunnerGetCaughtNum / 5f);

        // 5v5：每个单位贡献1/5=0.2
        float chaserObstacleRate = Mathf.Clamp(((float)chaserObstacleHitCount / 5f), 0f, 1f);
        float chaserDroneRate = Mathf.Clamp(((float)chaserDroneHitCount / 5f), 0f, 1f);
        float runnerObstacleRate = Mathf.Clamp(((float)runnerObstacleHitCount / 5f), 0f, 1f);
        float runnerDroneRate = Mathf.Clamp(((float)runnerDroneHitCount / 5f), 0f, 1f);

        string row = $"{currentEpisode},{currentEpisodeTime:F2},{catchRate:F4},{chaserObstacleRate:F4},{chaserDroneRate:F4},{runnerObstacleRate:F4},{runnerDroneRate:F4}";
        episodeData.Add(row);

        Debug.Log($"5v5 APF回合 {currentEpisode} 统计:");
        Debug.Log($"  时间: {currentEpisodeTime:F2}s");
        Debug.Log($"  抓捕率: {catchRate:P2} (抓到{RunnerGetCaughtNum}/5个runner)");
        Debug.Log($"  Chaser障碍碰撞率: {chaserObstacleRate:P2} ({chaserObstacleHitCount}次)");
        Debug.Log($"  Chaser互撞率: {chaserDroneRate:P2} ({chaserDroneHitCount}次)");
        Debug.Log($"  Runner障碍碰撞率: {runnerObstacleRate:P2} ({runnerObstacleHitCount}次)");
        Debug.Log($"  Runner互撞率: {runnerDroneRate:P2} ({runnerDroneHitCount}次)");

        ExportToCSV();
    }

    private void ExportToCSV()
    {
        try
        {
            string episodePath = Path.Combine(basePath, "apf_5v5_results.csv");
            File.WriteAllLines(episodePath, episodeData, Encoding.UTF8);

            if (currentEpisode % 10 == 0)
            {
                Debug.Log($"5v5 APF数据已保存到: {episodePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存CSV文件失败: {e.Message}");
        }
    }

    void ExportFinalStatistics()
    {
        try
        {
            if (totalRuns == 0) return;

            float totalCatchRate = 0f;
            float totalChaserObstacleRate = 0f;
            float totalChaserDroneRate = 0f;
            float totalRunnerObstacleRate = 0f;
            float totalRunnerDroneRate = 0f;

            for (int i = 1; i < episodeData.Count; i++)
            {
                string[] values = episodeData[i].Split(',');
                if (values.Length >= 7)
                {
                    totalCatchRate += float.Parse(values[2]);
                    totalChaserObstacleRate += float.Parse(values[3]);
                    totalChaserDroneRate += float.Parse(values[4]);
                    totalRunnerObstacleRate += float.Parse(values[5]);
                    totalRunnerDroneRate += float.Parse(values[6]);
                }
            }

            int dataCount = episodeData.Count - 1;
            float avgCatchRate = totalCatchRate / dataCount;
            float avgChaserObstacleRate = totalChaserObstacleRate / dataCount;
            float avgChaserDroneRate = totalChaserDroneRate / dataCount;
            float avgRunnerObstacleRate = totalRunnerObstacleRate / dataCount;
            float avgRunnerDroneRate = totalRunnerDroneRate / dataCount;

            summaryData.Clear();
            summaryData.Add("Category,Value");
            summaryData.Add($"总回合数,{totalRuns}");
            summaryData.Add($"平均抓捕率,{avgCatchRate:F4}");
            summaryData.Add($"平均Chaser障碍碰撞率,{avgChaserObstacleRate:F4}");
            summaryData.Add($"平均Chaser互撞率,{avgChaserDroneRate:F4}");
            summaryData.Add($"平均Runner障碍碰撞率,{avgRunnerObstacleRate:F4}");
            summaryData.Add($"平均Runner互撞率,{avgRunnerDroneRate:F4}");
            summaryData.Add($"单回合时间(秒),{episodeTime}");
            summaryData.Add($"模式,APF_5v5");

            string summaryPath = Path.Combine(basePath, "apf_5v5_summary.csv");
            File.WriteAllLines(summaryPath, summaryData, Encoding.UTF8);

            Debug.Log($"=== APF 5v5测试完成 ===");
            Debug.Log($"总回合数: {totalRuns}");
            Debug.Log($"平均抓捕率: {avgCatchRate:P2}");
            Debug.Log($"5v5 APF数据已保存到: {summaryPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存统计文件失败: {e.Message}");
        }
    }
}