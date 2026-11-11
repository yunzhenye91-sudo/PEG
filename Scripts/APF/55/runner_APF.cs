using System.Collections.Generic;
using UnityEngine;
using Ursaanimation.CubicFarmAnimals;

public class runner_APF : MonoBehaviour
{
    Rigidbody rigidBody;
    public float maxSpeed = 0.8f;  // runner比chaser慢一点
    public float speed = 0.8f;
    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;

    public Transform target;
    public target_APF tgt;

    // 每回合重置森林场景中的飞行障碍物和地面动物
    public SimpleFlyingObstacle[] flyingObstacles;
    public AnimationController[] groundAnimals;

    // 5v5：5个chaser
    public Transform chaser1;
    public Transform chaser2;
    public Transform chaser3;
    public Transform chaser4;
    public Transform chaser5;

    // 5v5：4个友方runner
    public Transform runnerFriend1;
    public Transform runnerFriend2;
    public Transform runnerFriend3;
    public Transform runnerFriend4;

    // APF参数
    public float attractiveGain = 2.0f;
    public float repulsiveGain = 3.0f;
    public float obstacleRepulsiveGain = 2.0f;
    public float attractiveRange = 50.0f;  // 增大
    public float repulsiveRange = 8.0f;

    public bool isCaught = false;
    private bool hasCollisionEnded = false;
    private bool IsReachTarget = false;

    public LayerMask obstacleLayer;
    public float spawnRadius = 0.5f;

    private Vector3 initialspawn;
    private Quaternion initialrot;
    private float lastDistanceToTarget;
    public obstacle[] obstacles;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        initialspawn = transform.localPosition;
        initialrot = transform.localRotation;
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

        if (target != null)
        {
            lastDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        }
    }

    private Vector3 ArtificialPotentialField()
    {
        Vector3 totalForce = Vector3.zero;

        if (target == null)
            return totalForce;

        // 1. 目标吸引力
        Vector3 toTarget = target.localPosition - transform.localPosition;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget > 0.1f)
        {
            if (distanceToTarget <= attractiveRange)
            {
                totalForce += attractiveGain * toTarget;
            }
            else
            {
                totalForce += attractiveGain * toTarget.normalized;
            }
        }

        // 2. 追击者排斥力（5个chaser）
        Transform[] chasers = { chaser1, chaser2, chaser3, chaser4, chaser5 };

        foreach (var chaser in chasers)
        {
            if (chaser != null)
            {
                Vector3 toChaser = transform.localPosition - chaser.localPosition;
                float distanceToChaser = toChaser.magnitude;

                if (distanceToChaser < repulsiveRange && distanceToChaser > 0.1f)
                {
                    totalForce += repulsiveGain * toChaser.normalized *
                                 (1.0f / distanceToChaser - 1.0f / repulsiveRange) /
                                 (distanceToChaser * distanceToChaser);
                }
            }
        }

        //// 3. 障碍物排斥力
        //Collider[] obstacles = Physics.OverlapSphere(transform.position, repulsiveRange, obstacleLayer);
        //foreach (var obstacle in obstacles)
        //{
        //    Vector3 toObstacle = transform.localPosition - obstacle.transform.localPosition;
        //    float distanceToObstacle = toObstacle.magnitude;

        //    if (distanceToObstacle < repulsiveRange && distanceToObstacle > 0.1f)
        //    {
        //        totalForce += obstacleRepulsiveGain * toObstacle.normalized *
        //                     (1.0f / distanceToObstacle - 1.0f / repulsiveRange) /
        //                     (distanceToObstacle * distanceToObstacle);
        //    }
        //}

        // 4. 边界排斥力
        Vector3 boundaryForce = CalculateBoundaryForce();
        totalForce += boundaryForce;

        return totalForce;
    }

    private Vector3 CalculateBoundaryForce()
    {
        Vector3 boundaryForce = Vector3.zero;
        Vector3 position = transform.localPosition;

        float minX = -6.5f, maxX = 7.0f;
        float minY = 0.5f, maxY = 4.5f;
        float minZ = -7.0f, maxZ = 10.0f;

        if (position.x < minX + 1.0f)
            boundaryForce.x += repulsiveGain * (1.0f / (position.x - minX));
        else if (position.x > maxX - 1.0f)
            boundaryForce.x -= repulsiveGain * (1.0f / (maxX - position.x));

        if (position.y < minY + 1.0f)
            boundaryForce.y += repulsiveGain * (1.0f / (position.y - minY));
        else if (position.y > maxY - 1.0f)
            boundaryForce.y -= repulsiveGain * (1.0f / (maxY - position.y));

        if (position.z < minZ + 1.0f)
            boundaryForce.z += repulsiveGain * (1.0f / (position.z - minZ));
        else if (position.z > maxZ - 1.0f)
            boundaryForce.z -= repulsiveGain * (1.0f / (maxZ - position.z));

        return boundaryForce;
    }

    void FixedUpdate()
    {
        if (isCaught || GlobalVariebles_APF.IsGameOver)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        if (target == null)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        Vector3 apfForce = ArtificialPotentialField();

        float maxForce = 2f;
        if (apfForce.magnitude > maxForce)
            apfForce = apfForce.normalized * maxForce;

        rawElv = Mathf.Clamp(apfForce.y, -1f, 1f);
        rawRoll = Mathf.Clamp(apfForce.x, -1f, 1f);
        rawPitch = Mathf.Clamp(apfForce.z, -1f, 1f);

        rigidBody.AddForce(transform.up * 9.81f);
        elv = rawElv * speed;
        roll = rawRoll * speed;
        pitch = rawPitch * speed;
        yaw = rawYaw * speed;

        Vector3 velocity = rigidBody.velocity;
        var yawAngle = -Mathf.PI / 180f * transform.localRotation.eulerAngles.y;

        velocity.x = roll * Mathf.Cos(yawAngle) - pitch * Mathf.Sin(yawAngle);
        velocity.y = elv;
        velocity.z = roll * Mathf.Sin(yawAngle) + pitch * Mathf.Cos(yawAngle);

        rigidBody.velocity = velocity;

        if (rigidBody.velocity.magnitude > maxSpeed)
        {
            rigidBody.velocity = rigidBody.velocity.normalized * maxSpeed;
        }

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

        if (tgt != null) tgt.spawn();

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

        GlobalVariebles_APF.RunnerGetCaughtNum++;
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
            FindObjectOfType<GlobalVariebles_APF>().RecordRunnerObstacleCollision();
        }

        if (other.gameObject.CompareTag("chaser"))
        {
            getCaught();
        }

        if (other.gameObject.CompareTag("target"))
        {
            if (IsReachTarget) return;
            IsReachTarget = true;

            GlobalVariebles_APF.IsGameOver = true;

            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }

        if (other.gameObject.CompareTag("runner"))
        {
            hasCollisionEnded = true;
            getCaught();
            FindObjectOfType<GlobalVariebles_APF>().RecordRunnerDroneCollision();
        }
    }
}