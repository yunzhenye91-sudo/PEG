using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System.IO;

public class GlobalVariebles : MonoBehaviour
{
    public List<chasertk> chasers;
    public List<runnertk> runners;

    public static int RunnerGetCaughtNum;
    public static bool IsGameOver;
    public static bool IsChaserCrashed;
    public static bool IsRunnerCrashed;

    private List<string> chaserData = new List<string>();
    private List<string> runnerData = new List<string>();

    private int currentEpisode = 0;
    private bool isEndingEpisode = false;

    // 5v5，所以每抓到/撞到一个就是 0.2
    public int teamSize = 5;

    public static int chaserObstacleCount = 0;
    public static int chaserDroneCount = 0;
    public static int runnerObstacleCount = 0;
    public static int runnerDroneCount = 0;

    // 保留这个变量，防止你其他脚本还在用
    public static bool hasRecordedThisEpisode = false;

    // 关键：内部防重复，不让其他 Agent 的 OnEpisodeBegin 乱改
    private bool localRecordedThisEpisode = false;

    // 防止同一帧或者连续几帧重复记录
    private int lastRecordFrame = -9999;

    void Start()
    {
        chaserData.Add("Episode,Chaser_CaptureRate,Chaser_ObstacleCollisionRate");
        runnerData.Add("Episode,Runner_CaughtRate,Runner_ObstacleCollisionRate");

        ExportToCSV();

        Debug.Log("抓捕率/障碍物碰撞率统计系统已启动");
    }

    void Update()
    {
        // 记录结束后，等几帧再允许下一轮记录
        // 这样可以防止同一批 EndEpisode 连续触发多次，写出很多 0.00
        if (localRecordedThisEpisode && Time.frameCount > lastRecordFrame + 5)
        {
            localRecordedThisEpisode = false;
            hasRecordedThisEpisode = false;
        }
    }

    public void RecordChaserObstacleCollision()
    {
        chaserObstacleCount++;
    }

    public void RecordRunnerObstacleCollision()
    {
        runnerObstacleCount++;
    }

    // 保留，避免其他地方调用报错
    // 但是 CSV 不统计互相碰撞
    public void RecordChaserDroneCollision()
    {
        chaserDroneCount++;
    }

    public void RecordRunnerDroneCollision()
    {
        runnerDroneCount++;
    }

    private void ExportToCSV()
    {
        string basePath = @"C:\Users\63510\Desktop\Data_PE\dynamicScenario\results";

        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        string chaserPath = Path.Combine(basePath, "chaser_episode_stats.csv");
        string runnerPath = Path.Combine(basePath, "runner_episode_stats.csv");

        File.WriteAllLines(chaserPath, chaserData);
        File.WriteAllLines(runnerPath, runnerData);

        Debug.Log($"CSV已导出:");
        Debug.Log($"Chaser: {chaserPath}");
        Debug.Log($"Runner: {runnerPath}");
    }

    public void SetInitialTarget()
    {
        if (chasers.Count > 0 && runners.Count > 0)
        {
            foreach (var chaser in chasers)
            {
                if (chaser != null)
                {
                    chaser.currentTarget = runners[0];
                }
            }

            Debug.Log("设置初始固定目标: Runner 0");
        }
    }

    public void SwitchToNextTarget()
    {
        int nextIndex = RunnerGetCaughtNum;

        if (nextIndex < runners.Count)
        {
            foreach (var chaser in chasers)
            {
                if (chaser == null) continue;

                chaser.currentTarget = runners[nextIndex];

                chaser.maxDistance = Vector3.Distance(
                    chaser.transform.localPosition,
                    chaser.currentTarget.transform.localPosition
                );

                chaser.lastDistanceToTarget = chaser.maxDistance;
            }

            Debug.Log($"切换到下一个固定目标: Runner {nextIndex}");
        }
        else
        {
            Debug.Log("所有Runner抓捕完成！");
        }
    }

    public void EndAllChasers()
    {
        EndAllAgents();
    }

    public void EndAllRunners()
    {
        EndAllAgents();
    }

    public void EndAllAgents()
    {
        // 同一个 Episode 只允许结束一次
        if (isEndingEpisode)
        {
            return;
        }

        isEndingEpisode = true;

        // 只写入一次 CSV
        RecordEpisodeData();

        // 所有 Agent 同步结束
        foreach (var c in chasers)
        {
            if (c != null)
            {
                c.EndEpisode();
            }
        }

        foreach (var r in runners)
        {
            if (r != null)
            {
                r.EndEpisode();
            }
        }

        StartCoroutine(UnlockNextEpisode());
    }

    private IEnumerator UnlockNextEpisode()
    {
        // 等待一帧，避开上一轮残留的重复触发
        yield return null;

        isEndingEpisode = false;
        hasRecordedThisEpisode = false;
        IsGameOver = false;
    }

    private void RecordEpisodeData()
    {
        if (hasRecordedThisEpisode)
        {
            return;
        }

        hasRecordedThisEpisode = true;

        float unitRate = 1f / teamSize;

        // runner 被抓到，或者 runner 自己撞障碍物导致 getCaught()
        // 都会增加 RunnerGetCaughtNum
        float chaserCaptureRate = Mathf.Clamp01(RunnerGetCaughtNum * unitRate);

        // 你的定义：Runner_CaughtRate 和 Chaser_CaptureRate 同步
        float runnerCaughtRate = chaserCaptureRate;

        // 只统计障碍物碰撞，不统计互相碰撞
        float chaserObstacleRate = Mathf.Clamp01(chaserObstacleCount * unitRate);
        float runnerObstacleRate = Mathf.Clamp01(runnerObstacleCount * unitRate);

        string chaserRow = $"{currentEpisode},{chaserCaptureRate:F2},{chaserObstacleRate:F2}";
        string runnerRow = $"{currentEpisode},{runnerCaughtRate:F2},{runnerObstacleRate:F2}";

        chaserData.Add(chaserRow);
        runnerData.Add(runnerRow);

        Debug.Log($"Episode {currentEpisode} 记录完成");
        Debug.Log($"RunnerGetCaughtNum: {RunnerGetCaughtNum}/{teamSize}");
        Debug.Log($"Chaser 抓捕率: {chaserCaptureRate:F2}, Chaser 障碍物碰撞率: {chaserObstacleRate:F2}");
        Debug.Log($"Runner 被抓捕率: {runnerCaughtRate:F2}, Runner 障碍物碰撞率: {runnerObstacleRate:F2}");

        ExportToCSV();

        currentEpisode++;

        // 统一在这里清零，准备下一回合
        RunnerGetCaughtNum = 0;

        chaserObstacleCount = 0;
        runnerObstacleCount = 0;

        chaserDroneCount = 0;
        runnerDroneCount = 0;
    }
}