using UnityEngine;
using Ursaanimation.CubicFarmAnimals;

public class runner_PID_5v5 : MonoBehaviour
{
    Rigidbody rigidBody;
    public float maxSpeed = 8f;
    public float speed = 4f;
    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;

    public Transform target;
    public target_PID_5v5 tgt;

    // 每回合只重置飞行障碍物和地面动物
    public SimpleFlyingObstacle[] flyingObstacles;
    public AnimationController[] groundAnimals;

    // 5v5模式：共有5个chaser
    public Transform chaser1;
    public Transform chaser2;
    public Transform chaser3;
    public Transform chaser4;
    public Transform chaser5;

    // 5v5模式：有4个友方runner
    public Transform runnerFriend1;
    public Transform runnerFriend2;
    public Transform runnerFriend3;
    public Transform runnerFriend4;

    public bool isCaught = false;
    private bool hasCollisionEnded = false;
    private bool IsReachTarget = false;

    public LayerMask obstacleLayer;
    public float spawnRadius = 0.5f;

    // PID控制器参数
    public float Kp = 2.0f;
    public float Ki = 0.05f;
    public float Kd = 0.5f;

    private Vector3 integralError = Vector3.zero;
    private Vector3 lastError = Vector3.zero;

    private Vector3 initialspawn;
    private Quaternion initialrot;
    private float lastDistanceToTarget;
    private float maxDistance;
    public obstacle[] obstacles;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        initialspawn = transform.localPosition;
        initialrot = transform.localRotation;

        integralError = Vector3.zero;
        lastError = Vector3.zero;
    }

    public void TakeOff()
    {
        Vector3 newPosition = new Vector3(
            Random.Range(-7.75f, 8.05f),
            Random.Range(1.45f, 3.66f),
            Random.Range(8.43f, 9.75f)
        );

        transform.localPosition = newPosition;
        transform.localRotation = initialrot;
        rigidBody.useGravity = true;

        // 初始化距离
        if (target != null)
        {
            maxDistance = Vector3.Distance(transform.localPosition, target.localPosition);
            lastDistanceToTarget = maxDistance;
        }
    }

    // PID控制器
    private Vector3 PIDControl(Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.localPosition;
        Vector3 error = targetPosition - currentPosition;

        integralError += error * Time.fixedDeltaTime;
        integralError = Vector3.ClampMagnitude(integralError, 5f);

        Vector3 derivativeError = (error - lastError) / Time.fixedDeltaTime;
        lastError = error;

        Vector3 controlOutput = Kp * error + Ki * integralError + Kd * derivativeError;

        return controlOutput;
    }

    void FixedUpdate()
    {
        if (isCaught || GlobalVariebles_PID_5v5.IsGameOver)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        if (target == null)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        // 使用PID控制器向目标移动
        Vector3 pidOutput = PIDControl(target.localPosition);

        rawElv = Mathf.Clamp(pidOutput.y, -1f, 1f);
        rawRoll = Mathf.Clamp(pidOutput.x, -1f, 1f);
        rawPitch = Mathf.Clamp(pidOutput.z, -1f, 1f);

        // 应用物理
        rigidBody.AddForce(transform.up * 9.81f);
        elv = rawElv * speed;
        roll = rawRoll * speed;
        pitch = rawPitch * speed;
        yaw = rawYaw * speed;

        // 计算速度
        Vector3 velocity = rigidBody.velocity;
        var yawAngle = -Mathf.PI / 180f * transform.localRotation.eulerAngles.y;

        velocity.x = roll * Mathf.Cos(yawAngle) - pitch * Mathf.Sin(yawAngle);
        velocity.y = elv;
        velocity.z = roll * Mathf.Sin(yawAngle) + pitch * Mathf.Cos(yawAngle);

        rigidBody.velocity = velocity;

        // 限制最大速度
        if (rigidBody.velocity.magnitude > maxSpeed)
        {
            rigidBody.velocity = rigidBody.velocity.normalized * maxSpeed;
        }

        // 更新距离
        float currentDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        lastDistanceToTarget = currentDistanceToTarget;
    }

    public void ResetEpisode()
    {   
        TakeOff();
        GetComponent<Collider>().enabled = true;
        rigidBody.detectCollisions = true;
        isCaught = false;
        hasCollisionEnded = false;
        IsReachTarget = false;

        rigidBody.inertiaTensor = new Vector3(1, 1, 1);
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.velocity = Vector3.zero;

        // 重置PID控制器
        integralError = Vector3.zero;
        lastError = Vector3.zero;

        // 重新生成目标位置
        if (tgt != null) tgt.spawn();

        // 只重置飞行障碍物和地面动物
        foreach (var obstacle in flyingObstacles)
        {
            if (obstacle != null)
            {
                obstacle.ResetObstacle();
            }
        }

        foreach (var animal in groundAnimals)
        {
            if (animal != null)
            {
                animal.ResetObstacle();
            }
        }

        foreach (obstacle obs in obstacles)
        {
            obs.ResetObstacle();
        }
    }

    public void getCaught()
    {
        if (isCaught) return;

        GlobalVariebles_PID_5v5.RunnerGetCaughtNum++;
        isCaught = true;
        GetComponent<Collider>().enabled = false;
        rigidBody.detectCollisions = false;
        rigidBody.velocity = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasCollisionEnded) return;

        if (other.gameObject.CompareTag("wall") || other.gameObject.CompareTag("obstacle"))
        {
            hasCollisionEnded = true;
            getCaught();
            FindObjectOfType<GlobalVariebles_PID_5v5>().RecordRunnerObstacleCollision();
        }

        if (other.gameObject.CompareTag("chaser"))
        {
            getCaught();
        }

        if (other.gameObject.CompareTag("target"))
        {
            if (IsReachTarget) return;
            IsReachTarget = true;

            GlobalVariebles_PID_5v5.IsGameOver = true;

            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }

        if (other.gameObject.CompareTag("runner"))
        {
            hasCollisionEnded = true;
            getCaught();
            FindObjectOfType<GlobalVariebles_PID_5v5>().RecordRunnerDroneCollision();
        }
    }
}