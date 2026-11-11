using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class GlobalVariebles_PID_3v3 : MonoBehaviour
{
    public List<chaser_PID_3v3> chasers;      // 3个chaser
    public List<runner_PID_3v3> runners;      // 3个runner
    public static int RunnerGetCaughtNum;
    public static bool IsGameOver;

    // 文件数据
    private List<string> episodeData = new List<string>();
    private List<string> summaryData = new List<string>();

    // 当前回合统计
    private int currentEpisode = 0;
    private bool isEpisodeActive = false;

    // 碰撞计数
    private int chaserObstacleHitCount = 0;
    private int chaserDroneHitCount = 0;
    private int runnerObstacleHitCount = 0;
    private int runnerDroneHitCount = 0;

    // 回合计时
    public float episodeTime = 30f;
    private float currentEpisodeTime = 0f;

    // 运行参数
    private int totalRuns = 0;
    public int maxRuns = 1000;

    // 文件路径
    private string basePath = @"C:\Users\63510\Desktop\Data_PE\3V3\PID_Results";

    void Start()
    {
        // 创建目录
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        // 初始化表头
        episodeData.Add("Episode,EpisodeTime,CatchRate,ChaserObstacleRate,ChaserDroneRate,RunnerObstacleRate,RunnerDroneRate");
        summaryData.Add("Category,Value");

        // 开始第一个回合
        StartNewEpisode();
    }

    void Update()
    {
        if (!isEpisodeActive) return;

        currentEpisodeTime += Time.deltaTime;

        // 检查回合结束条件
        if (currentEpisodeTime >= episodeTime || CheckAllRunnersCaught() || IsGameOver)
        {
            EndCurrentEpisode();

            // 开始新回合
            if (totalRuns < maxRuns)
            {
                StartCoroutine(StartNewEpisodeDelayed(1f));
            }
            else
            {
                ExportFinalStatistics();
                Debug.Log($"已完成 {maxRuns} 次运行");
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

        // 重置计数器
        RunnerGetCaughtNum = 0;
        IsGameOver = false;
        chaserObstacleHitCount = 0;
        chaserDroneHitCount = 0;
        runnerObstacleHitCount = 0;
        runnerDroneHitCount = 0;

        // 重置所有智能体
        ResetAllAgents();

        Debug.Log($"开始回合 {currentEpisode} ({totalRuns}/{maxRuns}) - 3v3模式");
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

    // 记录碰撞（3v3模式：每个单位贡献1/3≈0.333）
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
        // 3v3模式：每个被抓的runner贡献1/3≈0.333
        float catchRate = ((float)RunnerGetCaughtNum / 3f);

        // 3v3模式：每个chaser撞障碍物一次贡献1/3≈0.333，3个都撞一次就是1.0
        float chaserObstacleRate = Mathf.Clamp(((float)chaserObstacleHitCount / 3f), 0f, 1f);

        // 3v3模式：每个chaser互撞一次贡献1/3≈0.333
        float chaserDroneRate = Mathf.Clamp(((float)chaserDroneHitCount / 3f), 0f, 1f);

        // 3v3模式：每个runner撞障碍物一次贡献1/3≈0.333
        float runnerObstacleRate = Mathf.Clamp(((float)runnerObstacleHitCount / 3f), 0f, 1f);

        // 3v3模式：每个runner互撞一次贡献1/3≈0.333
        float runnerDroneRate = Mathf.Clamp(((float)runnerDroneHitCount / 3f), 0f, 1f);

        // 构建数据行
        string row = $"{currentEpisode},{currentEpisodeTime:F2},{catchRate:F4},{chaserObstacleRate:F4},{chaserDroneRate:F4},{runnerObstacleRate:F4},{runnerDroneRate:F4}";
        episodeData.Add(row);

        // 输出到控制台
        Debug.Log($"回合 {currentEpisode} 统计 (3v3):");
        Debug.Log($"  时间: {currentEpisodeTime:F2}s");
        Debug.Log($"  抓捕率: {catchRate:P2} (抓到{RunnerGetCaughtNum}/3个runner)");
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
            string episodePath = Path.Combine(basePath, "pid_3v3_results.csv");
            File.WriteAllLines(episodePath, episodeData, Encoding.UTF8);

            if (currentEpisode % 10 == 0)
            {
                Debug.Log($"数据已保存到: {episodePath}");
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

            // 计算所有回合的平均值
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

            // 构建汇总数据
            summaryData.Clear();
            summaryData.Add("Category,Value");
            summaryData.Add($"总回合数,{totalRuns}");
            summaryData.Add($"平均抓捕率,{avgCatchRate:F4}");
            summaryData.Add($"平均Chaser障碍碰撞率,{avgChaserObstacleRate:F4}");
            summaryData.Add($"平均Chaser互撞率,{avgChaserDroneRate:F4}");
            summaryData.Add($"平均Runner障碍碰撞率,{avgRunnerObstacleRate:F4}");
            summaryData.Add($"平均Runner互撞率,{avgRunnerDroneRate:F4}");
            summaryData.Add($"单回合时间(秒),{episodeTime}");
            summaryData.Add($"模式,3v3");

            // 保存汇总数据
            string summaryPath = Path.Combine(basePath, "pid_3v3_summary.csv");
            File.WriteAllLines(summaryPath, summaryData, Encoding.UTF8);

            Debug.Log($"=== 3v3 PID测试完成 ===");
            Debug.Log($"总回合数: {totalRuns}");
            Debug.Log($"平均抓捕率: {avgCatchRate:P2}");
            Debug.Log($"平均Chaser障碍碰撞率: {avgChaserObstacleRate:P2}");
            Debug.Log($"数据已保存到: {summaryPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存统计文件失败: {e.Message}");
        }
    }
}