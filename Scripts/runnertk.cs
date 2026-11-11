using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using Ursaanimation.CubicFarmAnimals;
using SimpleDroneController;

public class runnertk : Agent
{
    Rigidbody rigidBody;
    public float inputDrag, drag;
    public float maxSpeed;
    public float speed;
    public DroneController droneController;

    [Header("Bee Animation")]
    public Animator beeAnimator;
    public string idleAnimationName = "Idle";
    public string dieAnimationName = "Die";

    [Header("Simple Death Fall")]
    public float deathGroundY = -0.6f;
    public float deathFallSpeed = 3.0f;
    private Coroutine deathFallCoroutine;

    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;
    public Transform target;
    public target tgt;
    //public obstacle obs1;
    //public obstacle obs2;
    //public obstacle obs3;
    //public obstacle obs4;

    public Transform chaser1;
    public Transform chaser2;
    public Transform chaser3;
    public Transform chaser4;
    public Transform chaser5;

    public Transform runnerFriend1;//添加另一个友方无人机
    public Transform runnerFriend2;//添加另一个友方无人机
    public Transform runnerFriend3;//添加另一个友方无人机
    public Transform runnerFriend4;//添加另一个友方无人机

    public Transform[] obstacleCenters;
    public float obstacleCollisionRadius = 0.5f;


    public float repusiveDis = 0.5f;
    public float attraDis = 0.5f;
    private float orgdistance;
    private float currentdistance;
    private float currentdistance1;
    private float improvement;
    private Vector3 directiontotarget;
    private Vector3 initialspawn;
    private Quaternion initialrot;
    public bool isCaught = false;//判断无人机是否被捕
    private bool hasCollisionEnded = false;
    private bool IsReachTarget = false;
    //public int getCaughtNum = 0;
    //public int isCaughtNum = 0;//设置计数器，并与chaser共享这个信息

    private float success_count;
    private float episode_count;
    private float success_rate;
    private float mean_steps;
    private int total_steps;
    public LayerMask obstacleLayer; // 障碍物所在的图层
    public float maxObstacleDetectionRange = 15f; // 最大检测范围
    public float minVelocityThreshold = 0.01f; // 速度阈值
    private float lastDistanceToTarget;
    private float maxDistance;
    private float lastDistanceToChaser1;
    private float lastDistanceToChaser2;
    // 定义障碍物检测相关变量
    public float spawnRadius = 0.1f; // 初始位置生成的安全半径

    private Queue<float> rewardWindow; // 滑动窗口，用队列实现
    private int windowSize = 1000; // 窗口大小（算平均奖励用的）
    private float bestAverageReward = float.MinValue; // 历史最佳平均奖励
    private float recentAverageReward; // 当前滑动窗口的平均奖励
    private float stopTrainingThreshold = 0.05f; // 停止训练的阈值 (5%)
    public bool shouldStop = false; // 是否应该停止训练
    private int stepsToRunAfterStopCondition; // 满足条件后要额外运行后达到的总步数
    private int checkpointInterval = 200000; // 检查点间隔
    private int startCheckStep = 1000000; // 开始检查点的步数
    private Queue<bool> recentSuccesses = new Queue<bool>(); // 存储最近的成功状态 true 表示成功，false 表示失败
    private int maxWindowSize = 1000; // 滑动窗口的最大大小（算成功率用的）
    //public event UnityAction<int> OnRunnerCollision; // 事件：0代表runner撞墙导致失败，chaser成功，1代表chaser撞墙导致失败，runner成功，2代表runner成功碰到target，chaser失败，4代表chaser相互碰撞

    public BufferSensorComponent chaserAttentionBuffer;
    public BufferSensorComponent teammateAttentionBuffer;
    public SimpleFlyingObstacle[] flyingObstacles;
    public AnimationController[] groundAnimals;
    public obstacle[] obstacles;

    public void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        if (droneController == null)
        {
            droneController = GetComponent<DroneController>();
        }

        if (beeAnimator == null)
        {
            beeAnimator = GetComponentInChildren<Animator>();
        }

        initialspawn = this.transform.localPosition;
        initialrot = this.transform.localRotation;
        //isCaught = false;
        if (chaserAttentionBuffer == null)
        {
            Debug.LogWarning($"{gameObject.name}: ChaserAttention_R BufferSensor is not assigned.");
        }

        if (teammateAttentionBuffer == null)
        {
            Debug.LogWarning($"{gameObject.name}: TeammateAttention_R BufferSensor is not assigned.");
        }
        success_count = 0;
        episode_count = 0;
        success_rate = 0.0f;
        total_steps = 0;
    }

    // public void TakeOff()
    // {
    //     this.transform.localRotation = initialrot;
    //     this.transform.localPosition = new Vector3(Random.value * 3.2f - 1.2f, Random.value * 2.2f - 0.5f, Random.value * 4.2f - 0.2f); // train
    //     // this.transform.localPosition = new Vector3(1.0f,0f, 0.20f) + Random.insideUnitSphere * 0.2f; // test
    //     gameObject.GetComponent<Rigidbody>().useGravity = true;
    // }

    public void TakeOff()
    {
        int maxAttempts = 1000; // 最大尝试次数
        int attempts = 0;
        Vector3 newPosition;

        do
        {
            // 随机生成位置
            newPosition = new Vector3(
                    Random.Range(-7.75f, 8.05f), // X 轴范围
                    Random.Range(1.45f, 3.66f), // Y 轴范围
                    Random.Range(8.43f, 9.75f)  // Z 轴范围
            );

            if (IsPositionSafe(newPosition))
            {
                attempts++;
            }
        }
        while (IsTooCloseToRunnerFriend(newPosition) && attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Failed to find a safe position for runner spawning.");
        }

        // 设置最终确定的位置
        this.transform.localPosition = newPosition;
        this.transform.localRotation = initialrot;

        // 启用重力
        gameObject.GetComponent<Rigidbody>().useGravity = true;
    }

    private bool IsTooCloseToRunnerFriend(Vector3 position)
    {
        Transform[] friends =
        {
        runnerFriend1,
        runnerFriend2,
        runnerFriend3,
        runnerFriend4
    };

        foreach (Transform friend in friends)
        {
            if (friend == null)
            {
                continue;
            }

            if (Vector3.Distance(position, friend.localPosition) < 0.5f)
            {
                return true;
            }
        }

        return false;
    }
    //public void getCaught()
    //{
    //    this.iscaught = true;
    //    this.rigidBody.volocity = vector3.zero;
    //    rigidBody.enable = false;
    //    this.Transform.localPosition = ve
    //}

    private bool IsPositionSafe(Vector3 position)
    {
        // 检查给定位置是否与障碍物重叠
        return Physics.OverlapSphere(position, spawnRadius, obstacleLayer).Length == 0;
    }
    private void CheckMemoryUsage()
    {
        if (StepCount % 1000 == 0)
        {
            Debug.Log($"Step {StepCount}: 内存使用2 {System.GC.GetTotalMemory(false) / 1024 / 1024}MB");
        }
    }
    public void getCaught()
    {
        if (isCaught == true)
        {
            return;
        }

        GlobalVariebles.RunnerGetCaughtNum++;
        isCaught = true;
        GetComponent<Collider>().enabled = false;
        rigidBody.detectCollisions = false;

        if (beeAnimator != null)
        {
            beeAnimator.Play(dieAnimationName, 0, 0f);
        }

        if (droneController != null)
        {
            droneController.SetExternalInput(0f, 0f, 0f, 0f);
            droneController.SetEngineState(false);
        }

        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        if (deathFallCoroutine != null)
        {
            StopCoroutine(deathFallCoroutine);
        }

        deathFallCoroutine = StartCoroutine(SimpleFallToGround());
    }

    private IEnumerator SimpleFallToGround()
    {
        while (transform.localPosition.y > deathGroundY)
        {
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.MoveTowards(
                pos.y,
                deathGroundY,
                deathFallSpeed * Time.deltaTime
            );

            transform.localPosition = pos;

            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;

            yield return null;
        }

        Vector3 finalPos = transform.localPosition;
        finalPos.y = deathGroundY;
        transform.localPosition = finalPos;

        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
    }

    private bool IsInObstacleCollisionRadius()
    {
        Vector2 runnerXZ = new Vector2(transform.localPosition.x, transform.localPosition.z);

        foreach (Transform obs in obstacleCenters)
        {
            if (obs == null) continue;

            Vector2 obsXZ = new Vector2(obs.localPosition.x, obs.localPosition.z);

            float distanceXZ = Vector2.Distance(runnerXZ, obsXZ);

            if (distanceXZ <= obstacleCollisionRadius)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleRunnerObstacleCollision()
    {
        if (hasCollisionEnded) return;

        hasCollisionEnded = true;
        getCaught();

        AddReward(-1000.0f);

        FindObjectOfType<GlobalVariebles>().RecordRunnerObstacleCollision();
        Debug.Log("runner hit obstacle by radius");

        if (GlobalVariebles.RunnerGetCaughtNum >= 5)
        {
            UpdateRecentSuccess(false);
            transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        rawElv = actionBuffers.ContinuousActions[0];
        rawRoll = actionBuffers.ContinuousActions[1];
        rawPitch = actionBuffers.ContinuousActions[2];

        // // (1) APF algorithm
        // // Repusive force from chasers
        // Vector3 chaserToRunner1 = this.transform.localPosition - chaser1.localPosition;
        // Vector3 chaserToRunner2 = this.transform.localPosition - chaser2.localPosition;
        // Vector3 runnerToGoal = target.localPosition - this.transform.localPosition;

        // Vector3 rep1 = new Vector3(0.0f, 0.0f, 0.0f);
        // Vector3 rep2 = new Vector3(0.0f, 0.0f, 0.0f);
        // Vector3 attr = new Vector3(0.0f, 0.0f, 0.0f);

        // float xi = 2.0f, zeta = 2.0f;


        // if (chaserToRunner1.magnitude <= repusiveDis){
        //     rep1 = zeta * chaserToRunner1 * (1.0f/chaserToRunner1.magnitude - 1.0f/repusiveDis)/
        //     (chaserToRunner1.magnitude*chaserToRunner1.magnitude);
        // }

        // if (chaserToRunner2.magnitude <= repusiveDis){
        //     rep2 = zeta * chaserToRunner2 * (1.0f/chaserToRunner2.magnitude - 1.0f/repusiveDis)/
        //     (chaserToRunner2.magnitude * chaserToRunner2.magnitude);
        // }

        // if (runnerToGoal.magnitude <= attraDis){
        //     attr = runnerToGoal * xi;
        // }
        // else {
        //     attr = runnerToGoal * xi * attraDis / runnerToGoal.magnitude;
        // }

        // Vector3 totalForce = rep1 + rep2 + attr;

        // rawElv = totalForce.y;
        // rawRoll = totalForce.x;
        // rawPitch = totalForce.z;
        // // End the APF

        rigidBody.AddForce(transform.up * 9.81f);
        elv = rawElv * speed;
        roll = rawRoll * speed;
        pitch = rawPitch * speed;
        yaw = rawYaw * speed;
        if (isCaught)
        {
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            return;
        }
        var angularVelocity = rigidBody.angularVelocity;
        var velocity = rigidBody.velocity;
        var yawAngle = -Mathf.PI / 180f * this.transform.localRotation.eulerAngles.y;
        velocity.x = roll * Mathf.Cos(yawAngle) - pitch * Mathf.Sin(yawAngle);
        velocity.y = elv;
        velocity.z = roll * Mathf.Sin(yawAngle) + pitch * Mathf.Cos(yawAngle);
        angularVelocity.y = yaw;

        if (droneController != null)
        {
            droneController.SetExternalInput(rawRoll, rawElv, rawPitch, rawYaw);
        }
        else
        {
            rigidBody.velocity = velocity;
            rigidBody.angularVelocity = angularVelocity;
        }

        if (rigidBody.velocity.magnitude > maxSpeed)
        {
            rigidBody.velocity = rigidBody.velocity.normalized * maxSpeed;
        }
        if (!hasCollisionEnded && !isCaught && IsInObstacleCollisionRadius())
        {
            HandleRunnerObstacleCollision();
            return;
        }
        //AddReward(-10000 / this.MaxStep);
        //固定负奖励
        //if(GlobalVariebles.RunnerGetCaughtNum >= 3 )
        //{
        //    UpdateRecentSuccess(false); // 更新成功率的滑动窗口
        //    AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗
        //    Debug.Log(" All runners r caught by chaser");
        //    transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
        //}




        //if(isCaught)
        //{
        //    return;
        //}
        //这里在chaser里面修改通知
        // 奖励逻辑部分
        float currentDistanceToFriend = Vector3.Distance(this.transform.localPosition, runnerFriend1.localPosition);
        float currentDistanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);
        float currentVelocity = rigidBody.velocity.magnitude;
        float normalizedDistance = currentDistanceToTarget / maxDistance;
        //float timePenalty = StepCount / (float)this.MaxStep;
        // 1. 减少静止时间奖励
        if (isCaught == false)
        {
            //if (currentVelocity < minVelocityThreshold)
            //{
            //    AddReward(-1.0f); // 轻微惩罚
            //                      //Debug.Log("Punished for being stationary");
            //    return;  // 不再进行其他奖励计算
            //}

            // 2. 奖励差分策略
            AddReward(-0.03f);
            if (currentDistanceToTarget < lastDistanceToTarget)
            {
                AddReward(1.0f); // 固定奖励
                                 //Debug.Log("Rewarded for getting closer to the target");
            }
            else
            {
                AddReward(-0.6f); // 轻微惩罚
                                  //    //Debug.Log("Punished for moving away from the target");
            }
            float distanceReward = 1.0f - Mathf.Pow(normalizedDistance, 0.3f);
            AddReward(distanceReward * 0.3f);
        }
        //if (currentVelocity < minVelocityThreshold)
        //{
        //    AddReward(-1.0f); // 轻微惩罚
        //    //Debug.Log("Punished for being stationary");
        //    return;  // 不再进行其他奖励计算
        //}

        //// 2. 奖励差分策略
        //AddReward(0.01f);
        //if (currentDistanceToTarget < lastDistanceToTarget)
        //{
        //    AddReward(1.0f); // 固定奖励
        //    //Debug.Log("Rewarded for getting closer to the target");
        //}
        //else
        //{
        //    AddReward(-0.5f); // 轻微惩罚
        //                      //    //Debug.Log("Punished for moving away from the target");
        //}
        //if(Vector3.Distance(this.transform.localPosition, chaser1.localPosition) > lastDistanceToChaser1)
        //{
        //    AddReward(-0.5f);
        //}
        //else
        //{
        //    AddReward(-0.5f);
        //}
        //if (Vector3.Distance(this.transform.localPosition, chaser1.localPosition) > lastDistanceToChaser2)
        //{
        //    AddReward(-0.5f);
        //}
        //else
        //{
        //    AddReward(-0.5f);
        //}
        // 3. 优化奖励曲线
        //float normalizedDistance = currentDistanceToTarget / maxDistance; // 归一化距离（值范围 0 ~ 1）
        //float timePenalty = StepCount / (float)this.MaxStep; // 时间惩罚归一化
        // 距离越近奖励越大，使用反归一化公式
        //float distanceReward = Mathf.Clamp(1.0f - normalizedDistance, 0.01f, 1.0f); // 反归一化奖励.奖励范围 0.01~1.0
        //float distanceReward = 1.0f - Mathf.Pow(normalizedDistance, 0.3f);
        //AddReward(distanceReward*0.3f);
        //AddReward(distanceReward * 0.5f); // 调整比例（试过0.1，感觉有点小，现改为0;5）
        //Debug.Log($"Distance reward added: {distanceReward * 0.1f}");

        // 更新上一帧距离
        //CheckMemoryUsage();
        lastDistanceToTarget = currentDistanceToTarget;
    }

    private void AddRewardToSlidingWindow(float episodeReward)
    {
        // 将新奖励加入滑动窗口
        if (rewardWindow.Count >= windowSize)
        {
            rewardWindow.Dequeue(); // 超过窗口大小时移除最早的奖励
        }
        rewardWindow.Enqueue(episodeReward);
    }




    public override void OnEpisodeBegin()
    {
        GetComponent<RandomOpponentModel>()?.ApplyModeAndRandomModel();
        if (rewardWindow == null)
        {
            rewardWindow = new Queue<float>(windowSize);
        }

        // 如果滑动窗口已满，则计算滑动窗口平均奖励
        //if (rewardWindow.Count == windowSize)
        if (rewardWindow.Count >= windowSize)
        {
            // 计算当前滑动窗口的平均奖励
            recentAverageReward = rewardWindow.Average();

            // 更新历史最佳滑动窗口平均奖励
            if (recentAverageReward > bestAverageReward)
            {
                bestAverageReward = recentAverageReward;
            }

            // 记录到 TensorBoard
            Academy.Instance.StatsRecorder.Add("Custom/RecentAverageRewardRunner", recentAverageReward);
            Academy.Instance.StatsRecorder.Add("Custom/BestAverageRewardRunner", bestAverageReward);

            // 检查是否满足停止训练条件
            if (Academy.Instance.StepCount > startCheckStep && !shouldStop)
            {
                if (bestAverageReward != 0 &&
                Mathf.Abs(bestAverageReward - recentAverageReward) / Mathf.Abs(bestAverageReward) < stopTrainingThreshold
                && (bestAverageReward != recentAverageReward))
                {
                    Debug.Log($"Stopping condition(runner) met at step {Academy.Instance.StepCount}: recentAverageReward = {recentAverageReward}, bestAverageReward = {bestAverageReward}");
                    // 标记为满足停止条件
                    shouldStop = true;

                    // 计算需要再运行的步数
                    int currentStep = Academy.Instance.StepCount;
                    stepsToRunAfterStopCondition = currentStep + (checkpointInterval - (currentStep % checkpointInterval)) + 2; // +2 为保险值
                }
            }

        }

        // 如果已经满足停止条件
        if (shouldStop)
        {
            // 检查全局步数是否达到目标
            if (Academy.Instance.StepCount >= stepsToRunAfterStopCondition)
            {
                Debug.Log($"Stopping runner training after additional steps: recentAverageReward = {recentAverageReward}, bestAverageReward = {bestAverageReward}");
                //Academy.Instance.Dispose(); // 停止训练
            }
        }

        // 初始化最大距离为智能体初始位置到目标的距离
        maxDistance = Vector3.Distance(this.transform.localPosition, target.localPosition);
        lastDistanceToTarget = maxDistance;

        // test block
        //if(episode_count == 500)
        if (episode_count > 0 && (episode_count % 500 == 0))
        {
            //Debug.Log("Runner success rate:  " + success_rate);
            mean_steps = total_steps / success_count;
            // mean_steps = total_steps/episode_count;
            Debug.Log("Runner mean_steps: " + mean_steps);
            Academy.Instance.StatsRecorder.Add("Custom/RunnerMeanSteps", mean_steps); // 记录到 TensorBoard
            // 添加成功率到 TensorBoard
            //StatsRecorder.Add("Custom/RunnerSuccessRate", success_rate);
            //上面这行报错，改用下面这行
            //Academy.Instance.StatsRecorder.Add("Custom/RunnerSuccessRate", success_rate);

            float recentSuccessRate = GetRecentSuccessRate(); // 获取近期成功率
            Debug.Log($"Episode: {episode_count}, Overall Runner Success Rate: {success_rate:F2}, Recent Runner Success Rate: {recentSuccessRate:F2}");
            Academy.Instance.StatsRecorder.Add("Custom/RecentRunnerSuccessRate", recentSuccessRate); // 记录到 TensorBoard

            // 记录到文件
            //LogSuccessRateToFile();
        }
        //GlobalVariebles.RunnerGetCaughtNum;
        foreach (var animal in groundAnimals)
        {
            if (animal != null)
            {
                animal.ResetObstacle();
            }
        }

        foreach (var obstacle in flyingObstacles)
        {
            if (obstacle != null)
            {
                obstacle.ResetObstacle();
            }
        }

        foreach (obstacle obs in obstacles)
        {
            obs.ResetObstacle();
        }
        TakeOff();

        if (episode_count != 0)
        {
            success_rate = success_count / episode_count;
            // m_Recorder.Add("success rate", success_rate);
            // m_Recorder.Add("hidden probability", 1 - hidden_probability);
        }

        episode_count++;
        //GetComponent<Agent>().enabled = true;
        GetComponent<Collider>().enabled = true;
        rigidBody.detectCollisions = true;
        isCaught = false;
        hasCollisionEnded = false;
        IsReachTarget = false;

        if (deathFallCoroutine != null)
        {
            StopCoroutine(deathFallCoroutine);
            deathFallCoroutine = null;
        }

        if (beeAnimator != null)
        {
            beeAnimator.Play(idleAnimationName, 0, 0f);
        }

        if (droneController != null)
        {
            droneController.SetEngineState(true);
            droneController.SetExternalInput(0f, 0f, 0f, 0f);
        }
        GlobalVariebles.hasRecordedThisEpisode = false;
        GlobalVariebles.runnerObstacleCount = 0;
        GlobalVariebles.runnerDroneCount = 0;
        //GlobalVariebles.IsGameOver = false;
        //GlobalVariebles.IsRunnerCrashed = false;

        //getCaughtNum = 0;
        rigidBody.inertiaTensor = new Vector3(1, 1, 1);
        this.rigidBody.angularVelocity = Vector3.zero;
        this.rigidBody.velocity = Vector3.zero;
        orgdistance = Vector3.Distance(this.transform.localPosition, target.localPosition);
        tgt.spawn();
        //obs1.spawn();
        //obs2.spawn();
        //obs3.spawn();
        //obs4.spawn();

    }

    private void LogSuccessRateToFile()
    {
        string filePath = Application.dataPath + $"/Runnersuccess_rate_log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        float recentSuccessRate = GetRecentSuccessRate(); // 获取近期成功率
        //string log = $"Episode: {episode_count}, RunnerSuccess Rate: {success_rate:F2}\n";
        string log = $"Episode: {episode_count}, Runner Success Rate: {success_rate:F2}, RecentSuccess Rate: {recentSuccessRate:F2}\n";
        System.IO.File.AppendAllText(filePath, log);
    }

    // private float GetClosestObstacleDistance()
    // {
    //     Collider[] obstacles = Physics.OverlapSphere(this.transform.position, maxObstacleDetectionRange, obstacleLayer);
    //     float closestDistance = maxObstacleDetectionRange; // 初始化为最大检测范围

    //     foreach (Collider obstacle in obstacles)
    //     {
    //         float distance = Vector3.Distance(this.transform.position, obstacle.transform.position);
    //         if (distance < closestDistance)
    //         {
    //             closestDistance = distance;
    //         }
    //     }
    //     Debug.Log($"Closest Obstacle Distance: {closestDistance:F3}");
    //     return closestDistance;
    // }

    //返回最近的 N 个障碍物的距离
    private List<float> GetClosestObstacleDistances(int numObstacles)
    {
        Collider[] obstacles = Physics.OverlapSphere(this.transform.position, maxObstacleDetectionRange, obstacleLayer);
        List<float> distances = new List<float>();

        foreach (Collider obstacle in obstacles)
        {
            distances.Add(Vector3.Distance(this.transform.position, obstacle.transform.position));
        }

        distances.Sort(); // 按距离升序排列

        // 返回最近的 N 个障碍物距离
        while (distances.Count < numObstacles)
        {
            distances.Add(maxObstacleDetectionRange);
        }
        return distances.GetRange(0, numObstacles);

    }


    //返回最近 N 个障碍物的方向
    private List<Vector3> GetDirectionsToClosestObstacles(int numObstacles)
    {
        Collider[] obstacles = Physics.OverlapSphere(this.transform.position, maxObstacleDetectionRange, obstacleLayer);
        List<Vector3> directions = new List<Vector3>();
        List<float> distances = new List<float>();

        foreach (Collider obstacle in obstacles)
        {
            distances.Add(Vector3.Distance(this.transform.position, obstacle.transform.position));
            directions.Add((obstacle.transform.position - this.transform.position).normalized);
        }

        // 按距离升序排序
        var sortedObstacles = distances
            .Select((dist, index) => new { dist, dir = directions[index] })
            .OrderBy(o => o.dist)
            .Take(numObstacles);

        while (directions.Count < numObstacles)
        {
            directions.Add(Vector3.zero);
        }
        // 返回最近 N 个障碍物的方向
        return sortedObstacles.Select(o => o.dir).ToList();
    }

    private void CollectChaserBufferObservations()
    {
        if (chaserAttentionBuffer == null)
        {
            return;
        }

        AppendTransformObservationToBuffer(chaserAttentionBuffer, chaser1);
        AppendTransformObservationToBuffer(chaserAttentionBuffer, chaser2);
        AppendTransformObservationToBuffer(chaserAttentionBuffer, chaser3);
        AppendTransformObservationToBuffer(chaserAttentionBuffer, chaser4);
        AppendTransformObservationToBuffer(chaserAttentionBuffer, chaser5);

    }

    private void CollectTeammateBufferObservations()
    {
        if (teammateAttentionBuffer == null)
        {
            return;
        }

        AppendTransformObservationToBuffer(teammateAttentionBuffer, runnerFriend1);
        AppendTransformObservationToBuffer(teammateAttentionBuffer, runnerFriend2);
        AppendTransformObservationToBuffer(teammateAttentionBuffer, runnerFriend3);
        AppendTransformObservationToBuffer(teammateAttentionBuffer, runnerFriend4);

    }

    private void AppendTransformObservationToBuffer(BufferSensorComponent buffer, Transform other)
    {
        if (buffer == null || other == null)
        {
            return;
        }

        Vector3 relativePos = other.localPosition - transform.localPosition;

        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        Vector3 relativeVel = Vector3.zero;

        if (otherRb != null && rigidBody != null)
        {
            relativeVel = otherRb.velocity - rigidBody.velocity;
        }

        buffer.AppendObservation(new float[]
        {
        relativePos.x,
        relativePos.y,
        relativePos.z,

        relativeVel.x,
        relativeVel.y,
        relativeVel.z
        });
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CollectChaserBufferObservations();
        CollectTeammateBufferObservations();

        // 目标点相对自己的位置
        sensor.AddObservation(target.localPosition - transform.localPosition); // 3

        // 自己速度
        sensor.AddObservation(rigidBody.velocity); // 3

        // 障碍物距离
        List<float> closestObstacleDistances = GetClosestObstacleDistances(10);
        for (int i = 0; i < 10; i++)
        {
            if (i < closestObstacleDistances.Count)
            {
                sensor.AddObservation(closestObstacleDistances[i]);
            }
            else
            {
                sensor.AddObservation(maxObstacleDetectionRange);
            }
        }

        // 障碍物方向
        List<Vector3> closestObstacleDirections = GetDirectionsToClosestObstacles(10);
        for (int i = 0; i < 10; i++)
        {
            if (i < closestObstacleDirections.Count)
            {
                sensor.AddObservation(closestObstacleDirections[i]);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Keyboard Elv");
        continuousActionsOut[1] = Input.GetAxis("Keyboard Roll");
        continuousActionsOut[2] = Input.GetAxis("Keyboard Pitch");
    }

    private void UpdateRecentSuccess(bool success)
    {
        // 如果窗口已满，移除最早的记录
        if (recentSuccesses.Count >= maxWindowSize)
        {
            recentSuccesses.Dequeue();
        }
        // 添加当前回合的结果
        recentSuccesses.Enqueue(success);
    }
    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log(StepCount);
        // total_steps += StepCount;

        if (other.gameObject.CompareTag("wall"))
        {
            if (hasCollisionEnded) return;
            hasCollisionEnded = true;

            getCaught();

            AddReward(-1000.0f);
            //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
            Debug.Log("runner hit wall");
            FindObjectOfType<GlobalVariebles>().RecordRunnerObstacleCollision();
            if (GlobalVariebles.RunnerGetCaughtNum >= 5)
            {
                UpdateRecentSuccess(false); // 更新成功率的滑动窗口
                transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
            }
            //transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
            //OnRunnerCollision?.Invoke(0); // 通知所有 Chaser：Runner 撞墙导致失败

            //EndEpisode();
        }

        //else if (other.gameObject.CompareTag("obstacle"))
        //{
        //    if (hasCollisionEnded) return;
        //    hasCollisionEnded = true;
        //    getCaught();

        //    AddReward(-1000.0f);
        //    //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
        //    FindObjectOfType<GlobalVariebles>().RecordRunnerObstacleCollision();
        //    Debug.Log("runner hit obstacle");
        //    if (GlobalVariebles.RunnerGetCaughtNum >= 3)
        //    {
        //        UpdateRecentSuccess(false); // 更新成功率的滑动窗口
        //        transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
        //    }
        //    //transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();

        //}
        else if (other.gameObject.CompareTag("obstacle"))
        {
            HandleRunnerObstacleCollision();
        }
        else if (other.gameObject.CompareTag("chaser"))
        {
            //getCaught();
            //getCaught();

            Debug.Log(" one runner caught by chaser");
            AddReward(-1000.0f);
            //AddReward(-1000.0f);
            //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗

            //OnRunnerCollision?.Invoke(3); // 通知所有 Chaser：Runner被抓导致失败

            //EndEpisode();
        }

        else if (other.gameObject.CompareTag("target"))
        {
            if (IsReachTarget == true) return;
            IsReachTarget = true;
            GlobalVariebles.IsGameOver = true;
            AddReward(1000.0f);
            //GlobalVariebles.IsGameOver = true;
            Debug.Log("runner found blue box");
            success_count++;
            UpdateRecentSuccess(true); // 更新成功率的滑动窗口
            AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
            Debug.Log(StepCount);
            total_steps += StepCount;
            transform.parent.GetComponent<GlobalVariebles>().EndAllRunners();
        }
        else if (other.gameObject.CompareTag("runner"))
        {
            if (hasCollisionEnded) return;
            hasCollisionEnded = true;
            AddReward(-1000.0f);
            getCaught();
            FindObjectOfType<GlobalVariebles>().RecordRunnerDroneCollision();
            //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
            //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
            Debug.Log("friendly collision");
            if (GlobalVariebles.RunnerGetCaughtNum >= 5)
            {
                UpdateRecentSuccess(false); // 更新成功率的滑动窗口
                transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
            }
            //transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();

            //OnRunnerCollision?.Invoke(5); // 通知 Runner：Chaser互撞导致失败

            //EndEpisode();
        }
    }

    private float GetRecentSuccessRate()
    {
        // 如果窗口为空，返回 0
        if (recentSuccesses.Count == 0)
        {
            return 0f;
        }

        // 计算滑动窗口中成功的比例
        int successCount = 0;
        foreach (var success in recentSuccesses)
        {
            if (success) successCount++;
        }
        return (float)successCount / recentSuccesses.Count;
    }
}


// 使用 protected 覆盖父类的 OnEnable() 方法
//protected override void OnEnable()
//{
//    Transform environment = transform.parent; // 获取当前训练环境的父对象
//    if (environment != null)
//    {
//        chasertk chaser = environment.GetComponentInChildren<chasertk>(); // 找到当前环境的 Chaser
//        if (chaser != null)
//        {
//            base.OnEnable(); // 保证父类的 OnEnable 被执行
//            chaser.OnChaserCollision += HandleChaserCollision;
//            Debug.Log($"Runner {gameObject.name} is listening to Chaser {chaser.gameObject.name}");
//        }
//    }
//}

//protected override void OnDisable()
//{
//    Transform environment = transform.parent; // 获取当前训练环境的父对象
//    if (environment != null)
//    {
//        chasertk chaser = environment.GetComponentInChildren<chasertk>(); // 找到当前环境的 Chaser
//        if (chaser != null)
//        {
//            base.OnDisable(); // 保证父类的 OnDisable 被执行
//            chaser.OnChaserCollision -= HandleChaserCollision;
//        }
//    }
//}

//对于方案一而言这函数其实没起作用
//private void HandleChaserCollision(int state)
//// 事件：0代表runner撞墙导致失败，chaser成功，  1代表chaser撞墙导致失败，runner成功，
////       2代表runner成功碰到target，runner成功，3代表chaser成功抓到所有runner，chaser成功，注意（2,3）需要进行探讨。需要对3进行判断修改
////       4代表chaser相互碰撞，runner成功，      5代表runner相互碰撞，chaser成功
////       在runner代码中，监听chaser的行为1,4导致的结果，以及3是触发共同行为产生的结果。
////       在chaser代码中，监听runner的行为导致0,2,5的结果，以及3
////       更新代码，此时在chaser中添加5.
//{
//    if (state == 0)
//    {
//        AddReward(-1000.0f);
//UpdateRecentSuccess(false); // 更新成功率的滑动窗口
//        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口

//        EndEpisode();
//    }
//    else if (state == 1)
//    {
//        Debug.Log("Runner notified: Chaser hit the wall and failed.");
//        //OnRunnerCollision?.Invoke(1);
//        //AddReward(500.0f);
//        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
//        //Debug.Log(StepCount);
//        OnRunnerCollision?.Invoke(1);

//        EndEpisode();
//    }
//    else if (state == 2)
//    {
//        //AddReward(300.0f);
//        success_count++;
//        UpdateRecentSuccess(true); // 更新成功率的滑动窗口
//        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
//        Debug.Log("runners reach the target,and succeed");
//        total_steps += StepCount;
//        EndEpisode();
//    }
//    else if (state == 3)
//    {
//        //由chaser来决定什么时候结束episode。
//        AddReward(-500.0f);
//        OnRunnerCollision?.Invoke(3);

//        //OnRunnerCollision?.Invoke(3);
//        UpdateRecentSuccess(false); // 更新成功率的滑动窗口
//        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
//        Debug.Log(" ALL runners caught by chasers");
//        //OnRunnerCollision?.Invoke(3);

//        EndEpisode();
//        //Debug.Log("Runner notified: Chaser caught the runner and succeeded.");
//    }
//    else if (state == 4)
//    {
//        //AddReward(500.0f);
//        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
//        Debug.Log("Runner notified: friendly collision between chasers occured");
//        EndEpisode();
//    }
//    else if (state == 6)
//    {
//        AddReward(-500.0f);
//        Debug.Log(" one runner is caught by chasers");
//        getCaughtNum++;
//        OnRunnerCollision?.Invoke(6);//这里通知chaser，做一个通信接口,dengxixa yong
//    }
//}