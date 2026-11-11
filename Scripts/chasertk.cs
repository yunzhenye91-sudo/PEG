using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using SimpleDroneController;

public class chasertk : Agent
{
    Rigidbody rigidBody;
    public DroneController droneController;

    [Header("Bee Animation")]
    public Animator beeAnimator;
    public string idleAnimationName = "Idle";
    public string dieAnimationName = "Die";

    [Header("Simple Death Fall")]
    public float deathGroundY = -0.5f;
    public float deathFallSpeed = 3.0f;
    private Coroutine deathFallCoroutine;

    public float inputDrag, drag;
    public float maxSpeed;
    public float speed;
    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;
    public Transform target1;
    public Transform target2;//增加第二个target
    public Transform target3;//增加第二个target
    //public Transform target4;//增加第二个target
    //public Transform target5;//增加第二个target

    //public Transform currentTarget;
    public Transform initialTarget;

    public List<runnertk> runners;
    public runnertk currentTarget;
    private GlobalVariebles envRunners;


    //private Transform closestTarget;//初始化最近的目标
    private Transform inspectorPosition;
    //private Transform currentTarget;//实时当前最近的目标；
    public Transform chaserfriend1;
    public Transform chaserfriend2;
    //public Transform chaserfriend3;
    //public Transform chaserfriend4;
    public Transform RunnerTarget;

    public Transform[] obstacleCenters;
    public float obstacleCollisionRadius = 0.5f;


    public bool IsChaserDefence;
    public bool IsChaserDefenceReachArea;
    public bool IsChaserDefenceChangeAttack;

    public bool IsChaserAttack;

    //public bool Caught = false;
    //public int CatchingNum = 0;
    //public int FriendChaserCatchingNum = 0;
    // public Transform box;
    private float orgdistance;
    private float currentdistance;
    private float currentdistance1;


    private float improvement;
    private Vector3 directiontotarget;
    private Vector3 initialspawn;
    private Quaternion initialrot;
    private float timePenalty;
    public bool isCrashed = false;
    public bool hasCollisionEnded = false;

    public LayerMask obstacleLayer; // 障碍物所在的图层
    public float maxObstacleDetectionRange = 15f; // 最大检测范围
    public float minVelocityThreshold = 0.01f; // 速度阈值
    public float lastDistanceToTarget;
    private float lastDistanceToTargetArea;

    public float maxDistance;
    private float maxDistanceToArea;
    // 定义障碍物检测相关变量
    public float spawnRadius = 0.1f; // 初始位置生成的安全半径
    private Queue<float> rewardWindow; // 滑动窗口，用队列实现
    private int windowSize = 1000; // 窗口大小（算平均奖励用的）
    private float bestAverageReward = float.MinValue; // 历史最佳平均奖励
    private float recentAverageReward; // 当前滑动窗口的平均奖励
    private float stopTrainingThreshold = 0.05f; // 停止训练的阈值 (5%)
    //private float episode_count;
    public bool shouldStop = false; // 是否应该停止训练
    private int stepsToRunAfterStopCondition; // 满足条件后要额外运行后达到的总步数
    private int checkpointInterval = 200000; // 检查点间隔
    private int startCheckStep = 1000000; // 开始检查点的步数
    private float mean_steps;
    private int total_steps;
    //private float success_count;
    //private float success_rate;
    private float partial_success_count = 0f; // 部分成功次数（按比例计算）
    private float episode_count = 0f;
    private float success_rate = 0f;
    private Queue<float> recentSuccessRates = new Queue<float>(); // 存储每次的成功率
    //private int maxWindowSize = 100; // 滑动窗口大小
    //private Queue<bool> recentSuccesses = new Queue<bool>(); // 存储最近的成功状态 true 表示成功，false 表示失败
    private int maxWindowSize = 1000; // 滑动窗口的最大大小（算成功率用的）
    //public event UnityAction<int> OnChaserCollision;  // 事件：0代表runner撞墙导致失败，chaser成功，1代表chaser撞墙导致失败，runner成功，2代表runner成功碰到target，chaser失败，3代表chaser成功抓到runner，runner失败

    public BufferSensorComponent runnerAttentionBuffer;
    public BufferSensorComponent teammateAttentionBuffer;

    public void Start()
    {
        //initialspawn = this.transform.localPosition;
        initialspawn = this.transform.localPosition;
        initialrot = this.transform.localRotation;
        rigidBody = GetComponent<Rigidbody>();
        if (droneController == null)
        {
            droneController = GetComponent<DroneController>();
        }

        if (beeAnimator == null)
        {
            beeAnimator = GetComponentInChildren<Animator>();
        }
        envRunners = transform.parent.GetComponent<GlobalVariebles>();

        if (runnerAttentionBuffer == null)
        {
            Debug.LogWarning($"{gameObject.name}: RunnerAttention BufferSensor is not assigned.");
        }

        if (teammateAttentionBuffer == null)
        {
            Debug.LogWarning($"{gameObject.name}: TeammateAttention BufferSensor is not assigned.");
        }
        //chooseTarget();
        episode_count = 0;
        inspectorPosition = initialTarget;
        //success_count = 0;
        success_rate = 0.0f;
        total_steps = 0;

    }

    // public void TakeOff()
    // {
    //     this.transform.localRotation = initialrot;
    //     //this.transform.localPosition = new Vector3(Random.value * 3.2f - 1.2f, Random.value * 2.2f - 0.5f, Random.value * 4.2f - 0.2f);
    //     do
    //     { 
    //         this.transform.localPosition = new Vector3(Random.value * 3.2f - 1.2f, Random.value * 2.2f - 0.5f, Random.value * 4.2f - 0.2f);
    //     } while (Vector3.Distance(this.transform.localPosition, chaserfriend.localPosition) < 1.0f);
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
                    Random.Range(-5.11f, -3.53f)  // Z 轴范围
            );

            if (IsPositionSafe(newPosition))
            {
                attempts++;
            }
        }
        while (((Vector3.Distance(this.transform.localPosition, chaserfriend1.localPosition) < 0.5f) ||
               (Vector3.Distance(this.transform.localPosition, chaserfriend2.localPosition) < 0.5f)) &&
                                 attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Failed to find a safe position for chaser spawning.");
        }

        // 设置最终确定的位置
        this.transform.localPosition = newPosition;
        this.transform.localRotation = initialrot;

        // 启用重力
        gameObject.GetComponent<Rigidbody>().useGravity = true;

    }

    public void getCrashed()
    {
        if (isCrashed == true)
        {
            return;
        }

        isCrashed = true;
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
        Vector2 chaserXZ = new Vector2(transform.localPosition.x, transform.localPosition.z);

        foreach (Transform obs in obstacleCenters)
        {
            if (obs == null) continue;

            Vector2 obsXZ = new Vector2(obs.localPosition.x, obs.localPosition.z);

            float distanceXZ = Vector2.Distance(chaserXZ, obsXZ);

            if (distanceXZ <= obstacleCollisionRadius)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleChaserObstacleCollision()
    {
        if (hasCollisionEnded) return;

        hasCollisionEnded = true;
        getCrashed();

        AddReward(-1300.0f);

        Debug.Log("chaser hit obstacle by radius");

        FindObjectOfType<GlobalVariebles>().RecordChaserObstacleCollision();
    }


    private bool IsPositionSafe(Vector3 position)
    {
        // 检查给定位置是否与障碍物重叠
        return Physics.OverlapSphere(position, spawnRadius, obstacleLayer).Length == 0;
    }
    public void AddRewardToAllChasers(float reward, string reason = "")
    {
        foreach (var chaser in this.envRunners.chasers)
        {
            if (chaser != null)
            {
                chaser.AddReward(reward);
                if (!string.IsNullOrEmpty(reason))
                {
                    //Debug.Log($"{chaser.gameObject.name} 获得团队奖励: {reward} ({reason})");
                }
            }
        }
    }
    public void AddTeamRewardForRunnerCaught(runnertk caughtRunner, chasertk catcher)
    {
        // 抓捕者获得个人奖励
        catcher.AddReward(1000f);
        Debug.Log($"{catcher.gameObject.name} 获得抓捕奖励: 900");

        // 其他chaser获得团队协作奖励
        foreach (var chaser in this.envRunners.chasers)
        {
            if (chaser != this)
            {
                if (chaser != null && chaser != catcher)
                {
                    float teamReward = 300.0f; // 团队协作奖励
                    chaser.AddReward(teamReward);
                    Debug.Log($"{chaser.gameObject.name} 获得团队协作奖励: {teamReward}");
                }
            }
        }
    }
    public void ChooseNearestTarget()
    {
        if (currentTarget != null && !currentTarget.isCaught)
        {
            // 当前目标仍然有效，不需要重新选择
            return;
        }
        float minDistance = float.MaxValue;
        runnertk nearestRunner = null;


        foreach (var runner in envRunners.runners)
        {
            if (runner != null && !runner.isCaught)
            {
                float distance = Vector3.Distance(transform.localPosition, runner.transform.localPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestRunner = runner;
                }
            }
        }

        if (nearestRunner != null)
        {
            currentTarget = nearestRunner;
            maxDistance = minDistance;
            lastDistanceToTarget = maxDistance;
            //Debug.Log($"{gameObject.name} 选择了目标: {nearestRunner.gameObject.name}, 距离: {minDistance:F2}");
        }
        else
        {
            currentTarget = null;
            lastDistanceToTarget = float.MaxValue;
            Debug.Log($"{gameObject.name} 没有可用的目标");
        }
    }
    //private void InitialChooseClosestTarget()
    //{
    //    //Wait();
    //    if (Vector3.Distance(this.transform.localPosition, target1.localPosition) >= Vector3.Distance(this.transform.localPosition, target2.localPosition))
    //    {
    //        closestTarget = target2;
    //    }
    //    else
    //    {
    //        closestTarget = target1;
    //    }
    //}
    //private void chooseTarget()
    //{
    //    if( tgtToTarget1 < tgtToTarget2 && 
    //        tgtToTarget1 < tgtToTarget3 )
    //    {
    //        currentTarget = target1;
    //    }
    //    else if(tgtToTarget2 < tgtToTarget1 &&
    //            tgtToTarget2 < tgtToTarget3 )
    //    {
    //        currentTarget = target2;
    //    }
    //    else if(tgtToTarget3 < tgtToTarget1 &&
    //            tgtToTarget3 < tgtToTarget2 )
    //    {
    //        currentTarget = target3;
    //    }
    //    maxDistance = Vector3.Distance(this.transform.localPosition, currentTarget.localPosition);
    //    lastDistanceToTarget = maxDistance;
    //}
    //void chooseTarget()
    //{
    //    float minDist = float.MaxValue;
    //    runnertk best = null;
    //    foreach (var r in envRunners.runners)
    //    {
    //        if (!r.isCaught)
    //        {
    //            float d = Vector3.Distance(transform.position, r.transform.position);
    //            if (d < minDist)
    //            {
    //                minDist = d;
    //                best = r;
    //            }
    //        }
    //    }
    //    if (best != null && best != currentTarget)
    //    {

    //        currentTarget = best;
    //        Debug.Log("New target reset successfully");

    //    }

    //}
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        // (0) Get action from DRL
        rawElv = actionBuffers.ContinuousActions[0];
        rawRoll = actionBuffers.ContinuousActions[1];
        rawPitch = actionBuffers.ContinuousActions[2];

        // // (1) PID controller
        // Vector3 error =  target.localPosition - this.transform.localPosition;
        // rawElv = 2.0f*(error.y);
        // rawRoll = 2.0f*(error.x);
        // rawPitch = 2.0f*(error.z);

        // // (2) Random controller

        // rawElv = 2.0f*Random.value;
        // rawRoll = 2.0f*Random.value;
        // rawPitch = 2.0f*Random.value;
        ChooseNearestTarget();
        if (GlobalVariebles.IsGameOver == true)
        {
            GlobalVariebles.IsGameOver = false;
            Debug.Log("Chaser notified: Runner reached the target and succeeded.");
            OnEpisodeEndWithFailure();
            //AddReward(-400.0f); // Runner 成功到达target，Chaser 失败//方案二中使用，调整合理奖励数值
            //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
            AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
            transform.parent.GetComponent<GlobalVariebles>().EndAllChasers();

        }
        //rawYaw = actionBuffers.ContinuousActions[3];
        rigidBody.AddForce(transform.up * 9.81f);
        elv = rawElv * speed;
        roll = rawRoll * speed;
        pitch = rawPitch * speed;
        yaw = rawYaw * speed;
        if (isCrashed)
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
        if (!hasCollisionEnded && !isCrashed && IsInObstacleCollisionRadius())
        {
            HandleChaserObstacleCollision();
            return;
        }
        //AddReward(-10000 / this.MaxStep);
        //固定负奖励
        //ChooseNearestTarget();
        //if (GlobalVariebles.IsGameOver == true)
        //{
        //    GlobalVariebles.IsGameOver = false;
        //    Debug.Log("Chaser notified: Runner reached the target and succeeded.");
        //    OnEpisodeEndWithFailure();
        //    //AddReward(-400.0f); // Runner 成功到达target，Chaser 失败//方案二中使用，调整合理奖励数值
        //    //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
        //    AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
        //    transform.parent.GetComponent<GlobalVariebles>().EndAllChasers();

        //}

        //if (GlobalVariebles.IsChaserCrashed == true || GlobalVariebles.IsRunnerCrashed == true)
        //{
        //    GlobalVariebles.IsChaserCrashed = false;
        //    Debug.Log("chaser or runner crashed");

        //    //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
        //    transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();


        //}
        //这里在runner里面修改通知。

        // 奖励逻辑部分
        //float currentDistanceToTarget = Vector3.Distance(this.transform.localPosition, target1.localPosition);
        float currentDistanceToTarget1 = Vector3.Distance(this.transform.localPosition, target1.localPosition);
        //float currentDistanceToTarget2 = Vector3.Distance(this.transform.localPosition, target2.localPosition);
        //float currentDistanceToTarget3 = Vector3.Distance(this.transform.localPosition, target3.localPosition);
        float tgtToTarget1 = Vector3.Distance(target1.localPosition, RunnerTarget.localPosition);
        float tgtToTarget2 = Vector3.Distance(target2.localPosition, RunnerTarget.localPosition);
        float tgtToTarget3 = Vector3.Distance(target3.localPosition, RunnerTarget.localPosition);

        if (currentTarget == null)
        {
            return;
        }
        float currentDistanceToTarget = Vector3.Distance(this.transform.localPosition, currentTarget.transform.localPosition);//需要重点修改！！！！！！！！！！！！
        float normalizedDistance = currentDistanceToTarget / maxDistance;
        float currentVelocity = rigidBody.velocity.magnitude;
        float timePenalty = StepCount / (float)this.MaxStep; // 时间惩罚归一化
        float improvement = lastDistanceToTarget - currentDistanceToTarget;
        //float currentDistanceToRunnerTargetArea = Vector3.Distance(this.transform.localPosition, RunnerTarget.localPosition);
        //float normalizedDistanceToArea = currentDistanceToRunnerTargetArea / maxDistanceToArea;
        //防守无人机的奖励策略；
        //if (IsChaserDefence == true)
        //{
        //    if (IsChaserDefenceReachArea == false)
        //    {
        //        AddReward(-0.02f);
        //        if (currentDistanceToRunnerTargetArea < lastDistanceToTargetArea)
        //        {
        //            AddReward(1.0f); // 固定奖励
        //        }
        //        else
        //        {
        //            AddReward(-0.5f); // 轻微惩罚
        //        }
        //        float distanceReawardToArea = 1.5f - Mathf.Pow(normalizedDistanceToArea, 0.3f);
        //        AddReward(distanceReawardToArea * 0.2f);
        //        if (currentDistanceToRunnerTargetArea < 3.0f)
        //        {
        //            IsChaserDefenceReachArea = true;
        //            AddReward(100f);
        //        }
        //    }
        //    else if (IsChaserDefenceReachArea == true && IsChaserDefenceChangeAttack == false)
        //    {
        //        if (tgtToTarget1 < 5.0f || tgtToTarget2 < 5.0f || tgtToTarget3 < 5.0f)
        //        {
        //            chooseTarget();
        //            maxDistance = Vector3.Distance(this.transform.localPosition, initialTarget.localPosition);
        //            lastDistanceToTarget = maxDistance;
        //            IsChaserDefenceChangeAttack = true;
        //        }
        //        else
        //        {
        //            if (currentDistanceToRunnerTargetArea > 1.5f && currentDistanceToRunnerTargetArea < 3.5f)
        //            {
        //                AddReward(0.3f);
        //            }
        //            else
        //            {
        //                AddReward(-0.25f);
        //            }
        //        }
        //    }
        //    else if (IsChaserDefenceReachArea == true && IsChaserDefenceChangeAttack == true)
        //    {
        //        AddReward(-0.02f);
        //        if (currentVelocity < minVelocityThreshold)
        //        {
        //            AddReward(-1.0f); // 轻微惩罚
        //            return;  // 不再进行其他奖励计算
        //        }
        //        if (currentDistanceToTarget < lastDistanceToTarget)
        //        {
        //            AddReward(1.0f); // 固定奖励
        //                             //Debug.Log("Rewarded for getting closer to the target");
        //        }
        //        else
        //        {
        //            AddReward(-0.6f); // 轻微惩罚
        //                              //Debug.Log("Punished for moving away from the target");
        //        }
        //        float distanceReward = 1.0f - Mathf.Pow(normalizedDistance, 0.3f);
        //        AddReward(distanceReward * 0.3f);
        //        //？？？？？？？？？？可能需要调整，当runner靠近target时改变奖励，不然由于runner的移动，chaser也会增加奖励，导致不会发生追捕行为。
        //    }
        //}
        if (IsChaserAttack == true)
        {
            // 1. 基础时间惩罚（轻微）
            AddReward(-0.05f);

            // 2. 距离变化奖励（核心奖励）

            if (currentVelocity < minVelocityThreshold)
            {
                AddReward(-0.5f); // 轻微惩罚
                                  //Debug.Log("Punished for being stationary");  // 不再进行其他奖励计算
            }
            //AddReward(improvement * 10.0f); // 轻微惩罚

            //2.奖励差分策略

            if (currentDistanceToTarget < lastDistanceToTarget)
            {
                AddReward(1.2f); // 固定奖S励
                                 //Debug.Log("Rewarded for getting closer to the target");
            }
            else
            {
                AddReward(-1.0f); // 轻微惩罚
                                  //Debug.Log("Punished for moving away from the target");
            }
            float distanceReward = 1.0f - Mathf.Pow(normalizedDistance, 0.3f);
            AddReward(distanceReward * 0.3f);

        }


        //}
        // 3. 优化奖励曲线
        //float normalizedDistance = currentDistanceToTarget / maxDistance; // 归一化距离（值范围 0 ~ 1）
        //float timePenalty = StepCount / (float)this.MaxStep; // 时间惩罚归一化
        // 距离越近奖励越大，使用反归一化公式
        //float distanceReward = Mathf.Clamp(1.0f - normalizedDistance, 0.01f, 1.0f); // 反归一化奖励.奖励范围 0.01~1.0
        //float distanceReward = Mathf.Clamp((1.0f - normalizedDistance) * (1.0f - timePenalty), 0.01f, 1.0f);// 反归一化奖励.奖励范围 0.01~1.0
        //float distanceReward = 1.0f - Mathf.Pow(normalizedDistance, 0.3f);
        //AddReward(distanceReward*0.3f);
        //AddReward(distanceReward);
        //AddReward(distanceReward * 0.5f); // 调整比例（试过0.1，感觉有点小，现改为0.5）
        //Debug.Log($"Distance reward added: {distanceReward * 0.1f}");

        // 更新上一帧距离
        //CheckMemoryUsage();
        lastDistanceToTarget = currentDistanceToTarget;
        //lastDistanceToTargetArea = currentDistanceToRunnerTargetArea;

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
            Academy.Instance.StatsRecorder.Add("Custom/RecentAverageRewardChaser", recentAverageReward);
            Academy.Instance.StatsRecorder.Add("Custom/BestAverageRewardChaser", bestAverageReward);

            // 检查是否满足停止训练条件
            if (Academy.Instance.StepCount > startCheckStep && !shouldStop)
            {
                if (bestAverageReward != 0 &&
                    Mathf.Abs(bestAverageReward - recentAverageReward) / Mathf.Abs(bestAverageReward) < stopTrainingThreshold
                    && (bestAverageReward != recentAverageReward))
                {
                    Debug.Log($"Stopping condition(chaser) met at step {Academy.Instance.StepCount}: recentAverageReward = {recentAverageReward}, bestAverageReward = {bestAverageReward}");
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
                Debug.Log($"Stopping chaser training after additional steps: recentAverageReward = {recentAverageReward}, bestAverageReward = {bestAverageReward}");
                //Academy.Instance.Dispose(); // 停止训练
            }
        }

        if (episode_count > 0 && (episode_count % 500 == 0))
        {

            //mean_steps = total_steps / success_count;
            //Debug.Log("Chaser mean_steps: " + mean_steps);
            //Academy.Instance.StatsRecorder.Add("Custom/ChaserMeanSteps", mean_steps); // 记录到 TensorBoard

            float recentSuccessRate = GetRecentSuccessRate(); // 获取近期成功率
            // 发送到TensorBoard
            Academy.Instance.StatsRecorder.Add("SuccessRate/Overall", success_rate);
            Academy.Instance.StatsRecorder.Add("SuccessRate/Recent", recentSuccessRate);
            Academy.Instance.StatsRecorder.Add("SuccessRate/PartialSuccessCount", partial_success_count);
            Academy.Instance.StatsRecorder.Add("SuccessRate/TotalEpisodes", episode_count);

            // 每100回合打印一次日志
            if (episode_count % 1000 == 0)
            {
                Debug.Log($"Episode: {episode_count}, Overall Success Rate: {success_rate:P2}, Recent Success Rate: {recentSuccessRate:P2}");
            }

            // 记录到文件
            //LogSuccessRateToFile();
        }

        //if (episode_count != 0)
        //{
        //    success_rate = success_count / episode_count;
        //    // m_Recorder.Add("success rate", success_rate);
        //    // m_Recorder.Add("hidden probability", 1 - hidden_probability);
        //}

        TakeOff();
        //Caught = false;
        //InitialChooseClosestTarget();//这里有个伏笔，runner并未成功初始化延迟了怎么办。
        //orgdistance = Vector3.Distance(this.transform.localPosition, closestTarget.localPosition);
        GetComponent<Collider>().enabled = true;
        rigidBody.detectCollisions = true;
        IsChaserDefenceReachArea = false;
        IsChaserDefenceChangeAttack = false;
        hasCollisionEnded = false;
        isCrashed = false;

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

        //GlobalVariebles.RunnerGetCaughtNum = 0;
        GlobalVariebles.hasRecordedThisEpisode = false;
        GlobalVariebles.chaserObstacleCount = 0;
        GlobalVariebles.chaserDroneCount = 0;



        rigidBody.inertiaTensor = new Vector3(1, 1, 1);
        this.rigidBody.angularVelocity = Vector3.zero;
        this.rigidBody.velocity = Vector3.zero;

        //GlobalVariebles.IsChaserCrashed = false;
        //GlobalVariebles.RunnerGetCaughtNum = 0;
        // 初始化最大距离为智能体初始位置到目标的距离
        //currentTarget.transform = initialTarget;
        //chooseTarget();
        //runnertk nearestRunner = null;
        currentTarget = null;
        ChooseNearestTarget();
        //maxDistanceToArea = Vector3.Distance(this.transform.localPosition, RunnerTarget.localPosition);

        //lastDistanceToTargetArea = maxDistanceToArea;
        ////CathingNum = 0;
        episode_count++;
    }

    private void LogSuccessRateToFile()
    {
        string filePath = Application.dataPath + $"/Chasersuccess_rate_log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        float recentSuccessRate = GetRecentSuccessRate(); // 获取近期成功率
        //string log = $"Episode: {episode_count}, RunnerSuccess Rate: {success_rate:F2}\n";
        string log = $"Episode: {episode_count}, Chaser Success Rate: {success_rate:F2}, RecentSuccess Rate: {recentSuccessRate:F2}\n";
        System.IO.File.AppendAllText(filePath, log);
    }

    private float GetRecentSuccessRate()
    {
        if (recentSuccessRates.Count == 0) return 0f;

        float sum = 0f;
        foreach (var rate in recentSuccessRates)
        {
            sum += rate;
        }
        return sum / recentSuccessRates.Count;
    }
    private void UpdateRecentSuccessRate(float successRate)
    {
        if (recentSuccessRates.Count >= maxWindowSize)
        {
            recentSuccessRates.Dequeue();
        }
        recentSuccessRates.Enqueue(successRate);
    }
    private void OnEpisodeEndWithFailure()
    {
        // 计算本次回合的最终成功率
        float finalSuccessRate = (float)GlobalVariebles.RunnerGetCaughtNum / 3f;

        // 更新统计
        partial_success_count += finalSuccessRate;
        UpdateRecentSuccessRate(finalSuccessRate);

        Debug.Log($"回合结束，最终抓捕: {GlobalVariebles.RunnerGetCaughtNum}/3, 成功率: {finalSuccessRate:P2}");

        // 重置计数器
        //GlobalVariebles.RunnerGetCaughtNum = 0;
    }
    public override void CollectObservations(VectorSensor sensor)
    {

        CollectRunnerBufferObservations();
        CollectTeammateBufferObservations();

        if (currentTarget != null)
        {
            Vector3 relativePos = currentTarget.transform.localPosition - transform.localPosition;
            sensor.AddObservation(relativePos); // 3

            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            Vector3 relativeVel = Vector3.zero;

            if (targetRb != null && rigidBody != null)
            {
                relativeVel = targetRb.velocity - rigidBody.velocity;
            }

            sensor.AddObservation(relativeVel); // 3
            sensor.AddObservation(currentTarget.isCaught ? 1f : 0f); // 1
        }
        else
        {
            sensor.AddObservation(Vector3.zero); // 3
            sensor.AddObservation(Vector3.zero); // 3
            sensor.AddObservation(1f); // 1
        }
        //7
        sensor.AddObservation(rigidBody.velocity); // 自己速度 3



        //int teammateCount = 0;
        //foreach (var c in envRunners.chasers)
        //{
        //    if (c != this)
        //    {
        //        sensor.AddObservation(this.transform.localPosition - c.transform.localPosition); // 3
        //        sensor.AddObservation(c.GetComponent<Rigidbody>().velocity); // 3
        //        teammateCount++;
        //        if (teammateCount >= 2) break;
        //    }
        //}
        //for (int i = teammateCount; i < 2; i++)
        //{
        //    sensor.AddObservation(Vector3.zero); // 3
        //    sensor.AddObservation(Vector3.zero); // 3
        //}
        ////22


        // 添加最近 3 个障碍物的距离
        List<float> closestObstacleDistances = GetClosestObstacleDistances(10);
        // 确保有3个距离观测值
        for (int i = 0; i < 10; i++)
        {
            if (i < closestObstacleDistances.Count)
            {
                sensor.AddObservation(closestObstacleDistances[i]); // 距离占 1 维
                //Debug.Log("1111");
            }
            else
            {
                sensor.AddObservation(15f); // 默认距离值
                //Debug.Log("9999");

            }
        }
        // 32

        List<Vector3> closestObstacleDirections = GetDirectionsToClosestObstacles(10);
        // 确保有3个方向观测值
        for (int i = 0; i < 10; i++)
        {
            if (i < closestObstacleDirections.Count)
            {
                sensor.AddObservation(closestObstacleDirections[i]); // 每个方向占 3 维
            }
            else
            {
                sensor.AddObservation(Vector3.zero); // 默认方向值
            }
        }
        //53

    }

    private void CollectRunnerBufferObservations()
    {
        if (runnerAttentionBuffer == null || envRunners == null || envRunners.runners == null)
        {
            return;
        }

        if (currentTarget != null)
        {
            AppendRunnerObservationToBuffer(currentTarget);
        }

        foreach (var runner in envRunners.runners)
        {
            if (runner == null)
            {
                continue;
            }

            if (runner == currentTarget)
            {
                continue;
            }

            AppendRunnerObservationToBuffer(runner);
        }
    }

    private void AppendRunnerObservationToBuffer(runnertk runner)
    {
        Vector3 relativePos = runner.transform.localPosition - transform.localPosition;

        Rigidbody runnerRb = runner.GetComponent<Rigidbody>();
        Vector3 relativeVel = Vector3.zero;

        if (runnerRb != null && rigidBody != null)
        {
            relativeVel = runnerRb.velocity - rigidBody.velocity;
        }

        float isCurrentTarget = runner == currentTarget ? 1f : 0f;
        float isCaught = runner.isCaught ? 1f : 0f;

        runnerAttentionBuffer.AppendObservation(new float[]
        {
        isCurrentTarget,

        relativePos.x,
        relativePos.y,
        relativePos.z,

        relativeVel.x,
        relativeVel.y,
        relativeVel.z,

        isCaught
        });
    }

    private void CollectTeammateBufferObservations()
    {
        if (teammateAttentionBuffer == null || envRunners == null || envRunners.chasers == null)
        {
            return;
        }

        foreach (var chaser in envRunners.chasers)
        {
            if (chaser == null || chaser == this)
            {
                continue;
            }

            Vector3 relativePos = chaser.transform.localPosition - transform.localPosition;

            Rigidbody chaserRb = chaser.GetComponent<Rigidbody>();
            Vector3 relativeVel = Vector3.zero;

            if (chaserRb != null && rigidBody != null)
            {
                relativeVel = chaserRb.velocity - rigidBody.velocity;
            }

            teammateAttentionBuffer.AppendObservation(new float[]
            {
            relativePos.x,
            relativePos.y,
            relativePos.z,

            relativeVel.x,
            relativeVel.y,
            relativeVel.z
            });
        }
    }


    //返回最近的 N 个障碍物的距离
    private List<float> GetClosestObstacleDistances(int numObstacles)
    {
        Collider[] obstacles = Physics.OverlapSphere(
            this.transform.position,
            maxObstacleDetectionRange,
            obstacleLayer
        );

        List<float> distances = new List<float>();

        foreach (Collider obstacle in obstacles)
        {
            distances.Add(Vector3.Distance(this.transform.position, obstacle.transform.position));
        }

        distances.Sort();

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


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[3] = Input.GetAxis("Keyboard Yaw");
        continuousActionsOut[0] = Input.GetAxis("Keyboard Elv");
        continuousActionsOut[1] = Input.GetAxis("Keyboard Roll");
        continuousActionsOut[2] = Input.GetAxis("Keyboard Pitch");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("wall"))
        {
            if (hasCollisionEnded) return;
            hasCollisionEnded = true;
            getCrashed();

            AddReward(-1300.0f);
            //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口

            //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
            //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
            //GlobalVariebles.IsChaserCrashed = true;
            Debug.Log("chaser hit wall");

            // 记录障碍物碰撞
            FindObjectOfType<GlobalVariebles>().RecordChaserObstacleCollision();
            //transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();

        }
        //if (other.gameObject.CompareTag("obstacle"))
        //{
        //    if (hasCollisionEnded) return;
        //    hasCollisionEnded = true;
        //    getCrashed();

        //    AddReward(-1300.0f);
        //    //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口

        //    //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
        //    //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
        //    //GlobalVariebles.IsChaserCrashed = true;
        //    Debug.Log("chaser hit obstacle");
        //    // 记录障碍物碰撞
        //    FindObjectOfType<GlobalVariebles>().RecordChaserObstacleCollision();
        //    //transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();


        //}
        if (other.gameObject.CompareTag("obstacle"))
        {
            HandleChaserObstacleCollision();
        }
        if (other.gameObject.CompareTag("chaser"))
        {
            if (hasCollisionEnded) return;
            hasCollisionEnded = true;
            getCrashed();
            AddReward(-1200.0f);
            //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
            //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
            Debug.Log("friendly collision");

            FindObjectOfType<GlobalVariebles>().RecordChaserDroneCollision();
            //transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
        }
        if (other.gameObject.CompareTag("runner"))
        {
            runnertk r = other.GetComponent<runnertk>();
            if (r != null && !r.isCaught)
            {
                if (r == currentTarget)
                {
                    //GlobalVariebles.RunnerGetCaughtNum++;
                    //AddReward(1000.0f);
                    r.getCaught();
                    AddTeamRewardForRunnerCaught(r, this);
                    //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口

                    Debug.Log($"Runner caught! Total caught: {GlobalVariebles.RunnerGetCaughtNum}");
                    //r.getCaught();
                    //AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
                    ChooseNearestTarget();
                    float currentEpisodeSuccessRate = (float)GlobalVariebles.RunnerGetCaughtNum / 3f;

                    // 发送当前成功率到TensorBoard
                    Academy.Instance.StatsRecorder.Add("SuccessRate/Current", currentEpisodeSuccessRate);

                    Debug.Log($"抓捕进度: {GlobalVariebles.RunnerGetCaughtNum}/3, 当前成功率: {currentEpisodeSuccessRate:P2}");
                    //chooseTarget();
                    if (GlobalVariebles.RunnerGetCaughtNum >= 5)
                    {
                        AddRewardToAllChasers(500.0f, "found all runners");

                        partial_success_count += 1.0f; // 三个都抓到，成功率1.0
                        UpdateRecentSuccessRate(1.0f); // 记录本次成功率

                        //GlobalVariebles.RunnerGetCaughtNum = 0;
                        //success_count++;
                        //UpdateRecentSuccess(true); // 更新成功率的滑动窗口
                        Debug.Log(StepCount);
                        total_steps += StepCount;
                        //Debug.Log("chaser found all runners");
                        transform.parent.GetComponent<GlobalVariebles>().EndAllAgents();
                    }
                    else
                    {
                        Academy.Instance.StatsRecorder.Add("SuccessRate/CurrentProgress", currentEpisodeSuccessRate);
                    }
                }
                else
                {
                    r.getCaught();

                    Debug.Log("catch the wrong target");
                }

            }

        }
    }


    // 使用 protected 覆盖父类的 OnEnable() 方法
    //protected override void OnEnable()
    //{
    //    Transform environment = transform.parent; // 获取当前训练环境的父对象
    //    if (environment != null)
    //    {
    //        runnertk runner = environment.GetComponentInChildren<runnertk>(); // 找到当前环境的 Runner
    //        if (runner != null)
    //        {
    //            base.OnEnable(); // 保证父类的 OnEnable 被执行
    //            runner.OnRunnerCollision += HandleRunnerCollision;
    //            Debug.Log($"Chaser {gameObject.name} is listening to Runner {runner.gameObject.name}");
    //        }
    //    }
    //}

    //protected override void OnDisable()
    //{

    //    Transform environment = transform.parent; // 获取当前训练环境的父对象
    //    if (environment != null)
    //    {
    //        runnertk runner = environment.GetComponentInChildren<runnertk>(); // 找到当前环境的 Runner
    //        if (runner != null)
    //        {
    //            base.OnDisable(); // 保证父类的 OnDisable 被执行
    //            runner.OnRunnerCollision -= HandleRunnerCollision;
    //            Debug.Log($"Chaser {gameObject.name} is listening to Runner {runner.gameObject.name}");
    //        }
    //    }
    //}

    //private void HandleRunnerCollision(int state)
    //// 事件：0代表runner撞墙导致失败，chaser成功，  1代表chaser撞墙导致失败，runner成功，
    ////       2代表runner成功碰到target，runner成功，3代表chaser成功抓到所有runner，chaser成功，注意（2,3）需要进行探讨。需要对3进行判断修改
    ////       4代表chaser相互碰撞，runner成功，      5代表runner相互碰撞，chaser成功
    ////       在runner代码中，监听chaser的行为1,4导致的结果，以及3是触发共同行为产生的结果。
    ////       在chaser代码中，监听runner的行为导致0,2,5的结果，以及3
    ////       更新代码，此时在chaser中添加5.
    //{
    //    if (state == 2)
    //    {
    //        Debug.Log("Chaser notified: Runner reached the target and succeeded.");
    //        AddReward(-1000.0f); // Runner 成功到达target，Chaser 失败//方案二中使用，调整合理奖励数值
    //        UpdateRecentSuccess(false); // 更新成功率的滑动窗口
    //        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
    //        OnChaserCollision?.Invoke(2);

    //        EndEpisode();
    //    }
    //    else if (state == 0)
    //    {
    //        Debug.Log("Chaser notified: Runner hit the wall and failed.");
    //        //AddReward(1000.0f); // Runner 坠机，Chaser 获胜//方案二中使用，调整合理奖励数值
    //        //success_count++;
    //        //UpdateRecentSuccess(true); // 更新成功率的滑动窗口
    //        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
    //        //total_steps += StepCount;
    //        // 注意，这里不应该鼓励靠对手撞墙来获胜，违反了PE的目的。
    //        OnChaserCollision?.Invoke(0);//通知所有runner，同步

    //        EndEpisode();
    //    }
    //    else if (state == 1)
    //    {
    //        AddReward(-1000.0f);
    //        //UpdateRecentSuccess(false); // 更新成功率的滑动窗口
    //        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口

    //        Debug.Log("chaser hit wall or obstacle");
    //        //OnChaserCollision?.Invoke(1); // 通知 Runner：Chaser撞墙导致失败

    //        EndEpisode();
    //    }
    //    else if (state == 3)
    //    {
    //        //AddReward(1000.0f);
    //        success_count++;
    //        UpdateRecentSuccess(true); // 更新成功率的滑动窗口
    //        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
    //        total_steps += StepCount;
    //        Debug.Log("chaser found all runners");
    //        //OnChaserCollision?.Invoke(3); // 通知 Runner：Chaser成功抓到runner
    //        EndEpisode();
    //        //EndEpisode();
    //        //Debug.Log("Chaser notified: runner was caught and failed.");
    //    }
    //    else if (state == 5)
    //    {
    //        Debug.Log("Runner notified: friendly collision between chasers occured");
    //        //AddReward(1000.0f); // Runner 相互碰撞，chaser获胜。
    //        //success_count++;    
    //        //UpdateRecentSuccess(true); // 更新成功率的滑动窗口
    //        AddRewardToSlidingWindow(GetCumulativeReward());// 添加奖励到平均奖励的滑动窗口
    //        //total_steps += StepCount;

    //        EndEpisode();
    //    }
    //    else if (state == 6)
    //    {
    //        AddReward(600.0f);
    //        //timePenaltyFunction();
    //        //changeTarget();
    //        CathingNum++;

    //        Debug.Log("Chaser notified: one runner was caught and failed.CatingNum = 1");
    //    }
    //}

}