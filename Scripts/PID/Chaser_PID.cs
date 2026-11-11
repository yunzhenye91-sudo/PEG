using System.Collections.Generic;
using UnityEngine;

public class chaser_PID_3v3 : MonoBehaviour
{
    Rigidbody rigidBody;
    public float maxSpeed = 10f;
    public float speed = 5f;
    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;

    public List<runner_PID_3v3> runners;  // 3个runner
    public runner_PID_3v3 currentTarget;
    private GlobalVariebles_PID_3v3 envRunners;

    // 3v3模式：只有2个友方chaser
    public Transform chaserfriend1;
    public Transform chaserfriend2;

    public bool isCrashed = false;
    public bool hasCollisionEnded = false;

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

    void Start()
    {
        initialspawn = transform.localPosition;
        initialrot = transform.localRotation;
        rigidBody = GetComponent<Rigidbody>();
        envRunners = transform.parent.GetComponent<GlobalVariebles_PID_3v3>();

        integralError = Vector3.zero;
        lastError = Vector3.zero;
    }

    public void TakeOff()
    {
        Vector3 newPosition = new Vector3(
            Random.Range(-5.0f, 7.0f),
            Random.Range(1.2f, 4.5f),
            Random.Range(-6.5f, -3.23f)
        );

        transform.localPosition = newPosition;
        transform.localRotation = initialrot;

        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody>();

        rigidBody.useGravity = true;
    }

    public void ChooseNearestTarget()
    {
        if (currentTarget != null && !currentTarget.isCaught)
        {
            return;
        }

        float minDistance = float.MaxValue;
        runner_PID_3v3 nearestRunner = null;

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
        }
        else
        {
            currentTarget = null;
            lastDistanceToTarget = float.MaxValue;
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
        if (isCrashed || GlobalVariebles_PID_3v3.IsGameOver)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        // 选择最近目标
        ChooseNearestTarget();

        if (currentTarget == null)
        {
            // 悬停
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        // 使用PID控制器
        Vector3 pidOutput = PIDControl(currentTarget.transform.localPosition);

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

        // 更新距离记录
        if (currentTarget != null)
        {
            float currentDistanceToTarget = Vector3.Distance(transform.localPosition, currentTarget.transform.localPosition);
            lastDistanceToTarget = currentDistanceToTarget;
        }
    }

    public void ResetEpisode()
    {
        TakeOff();
        GetComponent<Collider>().enabled = true;
        rigidBody.detectCollisions = true;
        hasCollisionEnded = false;
        isCrashed = false;

        rigidBody.inertiaTensor = new Vector3(1, 1, 1);
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.velocity = Vector3.zero;

        // 重置PID控制器
        integralError = Vector3.zero;
        lastError = Vector3.zero;

        currentTarget = null;
        ChooseNearestTarget();
    }

    public void getCrashed()
    {
        if (isCrashed) return;
        isCrashed = true;
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
            getCrashed();
            FindObjectOfType<GlobalVariebles_PID_3v3>().RecordChaserObstacleCollision();
        }

        if (other.gameObject.CompareTag("chaser"))
        {
            hasCollisionEnded = true;
            getCrashed();
            FindObjectOfType<GlobalVariebles_PID_3v3>().RecordChaserDroneCollision();
        }

        if (other.gameObject.CompareTag("runner"))
        {
            runner_PID_3v3 r = other.GetComponent<runner_PID_3v3>();
            if (r != null && !r.isCaught)
            {
                r.getCaught();

                // 如果抓到的是当前目标，重新选择目标
                if (r == currentTarget)
                {
                    ChooseNearestTarget();
                }
            }
        }
    }
}