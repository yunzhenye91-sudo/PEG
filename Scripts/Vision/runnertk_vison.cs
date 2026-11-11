using UnityEngine;


public class runnertk_vison : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    private Rigidbody rb;

    private GlobalVariebles_vison environment;


    // ============================================================
    // RUNNER MODE
    // ============================================================

    [Header("Runner Mode")]

    [Tooltip("OFF = static Runner, ON = dynamic Runner")]
    public bool dynamicRunner = false;


    [Tooltip("Static training stage only.")]
    public bool holdStillForChaserTraining = true;


    [Tooltip("Caught Runner falls downward.")]
    public bool fallWhenCaught = true;


    // ============================================================
    // RUNNER STATE
    // ============================================================

    [Header("Runner State")]

    public bool isCaught = false;


    // ============================================================
    // RANDOM SPAWN
    // ============================================================

    [Header("Runner Random Spawn - LOCAL")]

    public Vector3 spawnMin =
        new Vector3(
            -7.75f,
            1.45f,
            8.43f
        );


    public Vector3 spawnMax =
        new Vector3(
            8.05f,
            3.66f,
            9.75f
        );


    // ============================================================
    // SPAWN SAFETY
    // ============================================================

    [Header("Spawn Safety")]

    public LayerMask spawnBlockingLayer;

    public float spawnRadius =
        0.25f;


    public int spawnAttempts =
        200;


    public float minRunnerSpawnSpacing =
        0.7f;


    public float minChaserSpawnSpacing =
        3.0f;


    public float minObstacleSpawnSpacing =
        3.0f;


    // ============================================================
    // DYNAMIC MOVEMENT
    // ============================================================

    [Header("Dynamic Runner Movement")]

    [Tooltip("Runner movement speed in Unity units / second.")]
    public float moveSpeed =
        0.4f;


    [Tooltip("Minimum time before changing movement direction.")]
    public float minDirectionChangeTime =
        8.0f;


    [Tooltip("Maximum time before changing movement direction.")]
    public float maxDirectionChangeTime =
        10.0f;


    [Tooltip(
        "Distance from movement boundary at which Runner turns inward."
    )]
    public float boundaryMargin =
        0.5f;


    [Range(0f, 1f)]
    [Tooltip(
        "0 = direction can change sharply; " +
        "1 = direction changes very smoothly."
    )]
    public float directionSmoothness =
        0.90f;


    // ============================================================
    // DYNAMIC INTERNAL STATE
    // ============================================================

    private Vector3 currentMoveDirection =
        Vector3.forward;


    private Vector3 desiredMoveDirection =
        Vector3.forward;


    private float directionChangeTimer =
        0f;


    private float fixedHeight =
        0f;


    // ============================================================
    // INITIAL STATE
    // ============================================================

    private Quaternion initialLocalRotation;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        EnsureReferences();


        initialLocalRotation =
            transform.localRotation;
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        EnsureReferences();
    }


    // ============================================================
    // REFERENCES
    // ============================================================

    private void EnsureReferences()
    {
        if (
            rb == null
        )
        {
            rb =
                GetComponent<Rigidbody>();
        }


        if (
            environment == null &&
            transform.parent != null
        )
        {
            environment =
                transform.parent
                    .GetComponent<GlobalVariebles_vison>();
        }
    }


    // ============================================================
    // RESET RUNNER
    //
    // Global calls this at every new Episode.
    // ============================================================

    public void ResetRunner()
    {
        EnsureReferences();


        // ========================================================
        // RESET STATE
        // ========================================================

        isCaught =
            false;


        // ========================================================
        // RESTORE COLLIDERS
        // ========================================================

        Collider[] colliders =
            GetComponentsInChildren<Collider>(
                true
            );


        foreach (
            Collider col
            in colliders
        )
        {
            if (
                col != null
            )
            {
                col.enabled =
                    true;
            }
        }


        // ========================================================
        // RESET PHYSICS
        // ========================================================

        if (
            rb != null
        )
        {
            rb.detectCollisions =
                true;


            rb.useGravity =
                false;


            rb.velocity =
                Vector3.zero;


            rb.angularVelocity =
                Vector3.zero;
        }


        // ========================================================
        // RANDOM SPAWN
        // ========================================================

        SpawnRandomly();


        // ========================================================
        // SAVE THIS EPISODE HEIGHT
        // ========================================================

        fixedHeight =
            transform.localPosition.y;


        // ========================================================
        // RANDOM INITIAL DIRECTION
        // ========================================================

        ChooseRandomDirection();


        currentMoveDirection =
            desiredMoveDirection;


        ResetDirectionTimer();


        // ========================================================
        // STATIC RUNNER
        // ========================================================

        if (
            !dynamicRunner
        )
        {
            if (
                rb != null
            )
            {
                rb.useGravity =
                    false;


                rb.velocity =
                    Vector3.zero;


                rb.angularVelocity =
                    Vector3.zero;
            }
        }
    }


    // ============================================================
    // RANDOM SPAWN
    // ============================================================

    private void SpawnRandomly()
    {
        Vector3 selected =
            transform.localPosition;


        bool found =
            false;


        for (
            int attempt = 0;
            attempt <
            Mathf.Max(
                1,
                spawnAttempts
            );
            attempt++
        )
        {
            Vector3 candidate =
                new Vector3(
                    Random.Range(
                        spawnMin.x,
                        spawnMax.x
                    ),

                    Random.Range(
                        spawnMin.y,
                        spawnMax.y
                    ),

                    Random.Range(
                        spawnMin.z,
                        spawnMax.z
                    )
                );


            if (
                IsSpawnPositionSafe(
                    candidate
                )
            )
            {
                selected =
                    candidate;


                found =
                    true;


                break;
            }
        }


        if (
            found
        )
        {
            transform.localPosition =
                selected;
        }
        else
        {
            Debug.LogWarning(
                gameObject.name +
                ": no safe Runner spawn found."
            );
        }


        transform.localRotation =
            initialLocalRotation;


        if (
            rb != null
        )
        {
            rb.velocity =
                Vector3.zero;


            rb.angularVelocity =
                Vector3.zero;
        }
    }


    // ============================================================
    // SPAWN POSITION SAFETY
    // ============================================================

    private bool IsSpawnPositionSafe(
        Vector3 localPosition)
    {
        Vector3 worldPosition =
            transform.parent != null
                ?
                transform.parent.TransformPoint(
                    localPosition
                )
                :
                localPosition;


        // ========================================================
        // WALL / OBSTACLE
        // ========================================================

        if (
            spawnBlockingLayer.value != 0
        )
        {
            Collider[] hits =
                Physics.OverlapSphere(
                    worldPosition,
                    spawnRadius,
                    spawnBlockingLayer,
                    QueryTriggerInteraction.Ignore
                );


            if (
                hits.Length > 0
            )
            {
                return false;
            }
        }


        if (
            environment == null
        )
        {
            return true;
        }


        // ========================================================
        // OTHER RUNNERS
        // ========================================================

        if (
            environment.runners != null
        )
        {
            foreach (
                runnertk_vison otherRunner
                in environment.runners
            )
            {
                if (
                    otherRunner == null ||
                    otherRunner == this
                )
                {
                    continue;
                }


                if (
                    Vector3.Distance(
                        worldPosition,
                        otherRunner
                            .transform
                            .position
                    )
                    <
                    minRunnerSpawnSpacing
                )
                {
                    return false;
                }
            }
        }


        // ========================================================
        // CHASERS
        // ========================================================

        if (
            environment.chasers != null
        )
        {
            foreach (
                chasertk_vison chaser
                in environment.chasers
            )
            {
                if (
                    chaser == null
                )
                {
                    continue;
                }


                if (
                    Vector3.Distance(
                        worldPosition,
                        chaser
                            .transform
                            .position
                    )
                    <
                    minChaserSpawnSpacing
                )
                {
                    return false;
                }
            }
        }


        // ========================================================
        // ACTIVE OBSTACLES
        //
        // Runner spawn must stay at least 3 units away.
        // ========================================================

        if (
            environment.ActiveObstacles != null
        )
        {
            foreach (
                Transform obstacle
                in environment.ActiveObstacles
            )
            {
                if (
                    obstacle == null ||
                    !obstacle.gameObject.activeInHierarchy
                )
                {
                    continue;
                }


                if (
                    Vector3.Distance(
                        worldPosition,
                        obstacle.position
                    )
                    <
                    minObstacleSpawnSpacing
                )
                {
                    return false;
                }
            }
        }


        return true;
    }


    // ============================================================
    // FIXED UPDATE
    // ============================================================

    private void FixedUpdate()
    {
        if (
            rb == null
        )
        {
            return;
        }


        // ========================================================
        // CAUGHT
        // ========================================================

        if (
            isCaught
        )
        {
            if (
                fallWhenCaught
            )
            {
                rb.useGravity =
                    true;
            }


            return;
        }


        // ========================================================
        // STATIC RUNNER
        // ========================================================

        if (
            !dynamicRunner
        )
        {
            if (
                holdStillForChaserTraining
            )
            {
                rb.useGravity =
                    false;


                rb.velocity =
                    Vector3.zero;


                rb.angularVelocity =
                    Vector3.zero;
            }


            return;
        }


        // ========================================================
        // DYNAMIC RUNNER
        // ========================================================

        UpdateDynamicMovement();
    }


    // ============================================================
    // DYNAMIC MOVEMENT
    // ============================================================

    private void UpdateDynamicMovement()
    {
        if (
            isCaught ||
            rb == null
        )
        {
            return;
        }


        rb.useGravity =
            false;


        // ========================================================
        // DIRECTION TIMER
        // ========================================================

        directionChangeTimer -=
            Time.fixedDeltaTime;


        if (
            directionChangeTimer <=
            0f
        )
        {
            ChooseRandomDirection();


            ResetDirectionTimer();
        }


        // ========================================================
        // CHECK BOUNDARY
        // ========================================================

        CheckMovementBoundary();


        // ========================================================
        // SMOOTH DIRECTION CHANGE
        // ========================================================

        float smoothFactor =
            1f -
            Mathf.Pow(
                directionSmoothness,
                Time.fixedDeltaTime *
                50f
            );


        currentMoveDirection =
            Vector3.Lerp(
                currentMoveDirection,
                desiredMoveDirection,
                smoothFactor
            );


        currentMoveDirection.y =
            0f;


        if (
            currentMoveDirection.sqrMagnitude >
            0.0001f
        )
        {
            currentMoveDirection.Normalize();
        }


        // ========================================================
        // VELOCITY
        //
        // Constant horizontal speed.
        // No vertical movement.
        // ========================================================

        Vector3 worldDirection;


        if (
            transform.parent != null
        )
        {
            worldDirection =
                transform.parent
                    .TransformDirection(
                        currentMoveDirection
                    );
        }
        else
        {
            worldDirection =
                currentMoveDirection;
        }


        worldDirection.y =
            0f;


        if (
            worldDirection.sqrMagnitude >
            0.0001f
        )
        {
            worldDirection.Normalize();
        }


        rb.velocity =
            worldDirection *
            moveSpeed;


        // ========================================================
        // LOCK Y HEIGHT
        // ========================================================

        Vector3 localPosition =
            transform.localPosition;


        localPosition.y =
            fixedHeight;


        transform.localPosition =
            localPosition;


        // ========================================================
        // OPTIONAL:
        // FACE MOVEMENT DIRECTION
        // ========================================================

        if (
            worldDirection.sqrMagnitude >
            0.001f
        )
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    worldDirection,
                    Vector3.up
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    5f *
                    Time.fixedDeltaTime
                );
        }
    }


    // ============================================================
    // RANDOM DIRECTION
    // ============================================================

    private void ChooseRandomDirection()
    {
        float angle =
            Random.Range(
                0f,
                360f
            );


        float radians =
            angle *
            Mathf.Deg2Rad;


        desiredMoveDirection =
            new Vector3(
                Mathf.Sin(
                    radians
                ),

                0f,

                Mathf.Cos(
                    radians
                )
            );


        desiredMoveDirection.Normalize();
    }


    // ============================================================
    // RESET DIRECTION TIMER
    // ============================================================

    private void ResetDirectionTimer()
    {
        directionChangeTimer =
            Random.Range(
                Mathf.Max(
                    0.1f,
                    minDirectionChangeTime
                ),

                Mathf.Max(
                    minDirectionChangeTime,
                    maxDirectionChangeTime
                )
            );
    }


    // ============================================================
    // MOVEMENT BOUNDARY
    //
    // Dynamic Runner stays inside the same X/Z range used
    // for its spawn area.
    // ============================================================

    private void CheckMovementBoundary()
    {
        Vector3 localPosition =
            transform.localPosition;


        bool needCorrection =
            false;


        Vector3 correction =
            desiredMoveDirection;


        // ========================================================
        // X MIN
        // ========================================================

        if (
            localPosition.x <=
            spawnMin.x +
            boundaryMargin
        )
        {
            correction.x =
                Mathf.Abs(
                    correction.x
                );


            needCorrection =
                true;
        }


        // ========================================================
        // X MAX
        // ========================================================

        if (
            localPosition.x >=
            spawnMax.x -
            boundaryMargin
        )
        {
            correction.x =
                -Mathf.Abs(
                    correction.x
                );


            needCorrection =
                true;
        }


        // ========================================================
        // Z MIN
        // ========================================================

        if (
            localPosition.z <=
            spawnMin.z +
            boundaryMargin
        )
        {
            correction.z =
                Mathf.Abs(
                    correction.z
                );


            needCorrection =
                true;
        }


        // ========================================================
        // Z MAX
        // ========================================================

        if (
            localPosition.z >=
            spawnMax.z -
            boundaryMargin
        )
        {
            correction.z =
                -Mathf.Abs(
                    correction.z
                );


            needCorrection =
                true;
        }


        if (
            needCorrection
        )
        {
            correction.y =
                0f;


            if (
                correction.sqrMagnitude >
                0.0001f
            )
            {
                correction.Normalize();


                desiredMoveDirection =
                    correction;
            }


            ResetDirectionTimer();
        }
    }


    // ============================================================
    // WALL / OBSTACLE COLLISION
    //
    // Runner hitting wall or obstacle:
    //
    // 1. Runner becomes isCaught
    // 2. Global RunnerGetCaughtNum + 1
    // 3. NO Chaser catch reward
    // 4. If all Runners are gone, end episode
    // ============================================================

    private bool IsWall(
        Collider col)
    {
        if (
            col == null
        )
        {
            return false;
        }


        Transform current =
            col.transform;


        while (
            current != null
        )
        {
            if (
                current.CompareTag(
                    "wall"
                )
            )
            {
                return true;
            }


            current =
                current.parent;
        }


        return false;
    }


    private bool IsObstacle(
        Collider col)
    {
        if (
            col == null ||
            environment == null ||
            environment.ActiveObstacles == null
        )
        {
            return false;
        }


        Transform hitTransform =
            col.transform;


        foreach (
            Transform obstacle
            in environment.ActiveObstacles
        )
        {
            if (
                obstacle == null ||
                !obstacle.gameObject.activeInHierarchy
            )
            {
                continue;
            }


            if (
                hitTransform == obstacle ||
                hitTransform.IsChildOf(
                    obstacle
                ) ||
                obstacle.IsChildOf(
                    hitTransform
                )
            )
            {
                return true;
            }
        }


        return false;
    }


    private void HandleRunnerEnvironmentCollision()
    {
        if (
            isCaught
        )
        {
            return;
        }


        bool allRunnersGone =
            false;


        if (
            environment != null
        )
        {
            allRunnersGone =
                environment
                    .RegisterRunnerEliminated(
                        this
                    );
        }
        else
        {
            getCaught();


            GlobalVariebles_vison
                .RunnerGetCaughtNum++;
        }


        if (
            allRunnersGone &&
            environment != null
        )
        {
            environment
                .EndEnvironmentEpisode(
                    GlobalVariebles_vison
                        .EpisodeEndReason
                        .AllRunnersEliminated
                );
        }
    }


    private void OnTriggerEnter(
        Collider other)
    {
        if (
            other == null ||
            isCaught
        )
        {
            return;
        }


        if (
            IsWall(
                other
            ) ||
            IsObstacle(
                other
            )
        )
        {
            HandleRunnerEnvironmentCollision();


            return;
        }
    }


    private void OnCollisionEnter(
        Collision collision)
    {
        if (
            collision == null ||
            collision.collider == null ||
            isCaught
        )
        {
            return;
        }


        Collider other =
            collision.collider;


        if (
            IsWall(
                other
            ) ||
            IsObstacle(
                other
            )
        )
        {
            HandleRunnerEnvironmentCollision();


            return;
        }
    }


    // ============================================================
    // CAUGHT
    //
    // Called by Global.
    // Global owns RunnerGetCaughtNum.
    // ============================================================

    public void getCaught()
    {
        if (
            isCaught
        )
        {
            return;
        }


        isCaught =
            true;


        // ========================================================
        // STOP
        // ========================================================

        if (
            rb != null
        )
        {
            rb.velocity =
                Vector3.zero;


            rb.angularVelocity =
                Vector3.zero;
        }


        // ========================================================
        // DISABLE COLLIDERS
        //
        // Caught Runner no longer blocks Chaser.
        // ========================================================

        Collider[] colliders =
            GetComponentsInChildren<Collider>(
                true
            );


        foreach (
            Collider col
            in colliders
        )
        {
            if (
                col != null
            )
            {
                col.enabled =
                    false;
            }
        }


        // ========================================================
        // FALL
        // ========================================================

        if (
            rb != null
        )
        {
            rb.detectCollisions =
                false;


            rb.useGravity =
                fallWhenCaught;
        }
    }
}