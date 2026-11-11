using System.Collections.Generic;
using UnityEngine;

public class chaser_APF : MonoBehaviour
{
    Rigidbody rigidBody;
    public float maxSpeed = 1f;
    public float speed = 1f;
    public float rawYaw, rawElv, rawRoll, rawPitch;
    private float yaw, elv, roll, pitch;

    public List<runner_APF> runners;  // 现在有5个runner
    public runner_APF currentTarget;
    private GlobalVariebles_APF envRunners;

    // APF参数
    public float repulsiveGain = 2.0f;
    public float attractiveGain = 2.0f;
    public float repulsiveRange = 5.0f;
    public float attractiveRange = 50.0f;  // 增大吸引力范围

    public bool isCrashed = false;
    public bool hasCollisionEnded = false;

    public LayerMask obstacleLayer;
    public float spawnRadius = 0.5f;

    private Vector3 initialspawn;
    private Quaternion initialrot;
    private float lastDistanceToTarget;

    void Start()
    {
        initialspawn = transform.localPosition;
        initialrot = transform.localRotation;
        rigidBody = GetComponent<Rigidbody>();
        envRunners = transform.parent.GetComponent<GlobalVariebles_APF>();
    }

    public void TakeOff()
    {
        Vector3 newPosition = new Vector3(
            Random.Range(-7.75f, 8.05f),
            Random.Range(1.45f, 3.66f),
            Random.Range(-5.11f, -3.53f)
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
        runner_APF nearestRunner = null;

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
            lastDistanceToTarget = minDistance;
        }
        else
        {
            currentTarget = null;
            lastDistanceToTarget = float.MaxValue;
        }
    }

    // APF人工势场法
    private Vector3 ArtificialPotentialField()
    {
        Vector3 totalForce = Vector3.zero;

        if (currentTarget == null)
            return totalForce;

        // 1. 目标吸引力
        Vector3 toTarget = currentTarget.transform.localPosition - transform.localPosition;
        float distanceToTarget = toTarget.magnitude;

        // 无论距离多远都有基础吸引力
        if (distanceToTarget > 0.1f)
        {
            if (distanceToTarget <= attractiveRange)
            {
                totalForce += attractiveGain * toTarget;
            }
            else
            {
                // 远距离：归一化的吸引力
                totalForce += attractiveGain * toTarget.normalized;
            }
        }

        // 2. 障碍物排斥力
        //Collider[] obstacles = Physics.OverlapSphere(transform.position, repulsiveRange, obstacleLayer);
        //foreach (var obstacle in obstacles)
        //{
        //    Vector3 toObstacle = transform.localPosition - obstacle.transform.localPosition;
        //    float distanceToObstacle = toObstacle.magnitude;

        //    if (distanceToObstacle < repulsiveRange && distanceToObstacle > 0.1f)
        //    {
        //        totalForce += repulsiveGain * toObstacle.normalized *
        //                     (1.0f / distanceToObstacle - 1.0f / repulsiveRange) /
        //                     (distanceToObstacle * distanceToObstacle);
        //    }
        //}

        // 3. 边界排斥力
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
        if (isCrashed || GlobalVariebles_APF.IsGameOver)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        ChooseNearestTarget();

        if (currentTarget == null)
        {
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.1f);
            return;
        }

        Vector3 apfForce = ArtificialPotentialField();

        // 限制力的大小
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
            FindObjectOfType<GlobalVariebles_APF>().RecordChaserObstacleCollision();
        }

        if (other.gameObject.CompareTag("chaser"))
        {
            hasCollisionEnded = true;
            getCrashed();
            FindObjectOfType<GlobalVariebles_APF>().RecordChaserDroneCollision();
        }

        if (other.gameObject.CompareTag("runner"))
        {
            runner_APF r = other.GetComponent<runner_APF>();
            if (r != null && !r.isCaught)
            {
                r.getCaught();

                if (r == currentTarget)
                {
                    ChooseNearestTarget();
                }
            }
        }
    }
}