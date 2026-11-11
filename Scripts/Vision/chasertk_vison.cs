using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class chasertk_vison : Agent
{
    // ============================================================
    // PHASE
    // ============================================================

    public enum PursuitPhase
    {
        Explore,
        Pursuit,
        Avoidance
    }

    [Header("Current Phase")]
    [SerializeField]
    private PursuitPhase currentPhase = PursuitPhase.Explore;


    // ============================================================
    // REFERENCES
    // ============================================================

    private Rigidbody rb;
    private GlobalVariebles_vison environment;


    // ============================================================
    // RUNNERS
    // ============================================================

    [Header("Runners")]

    public List<runnertk_vison> runners =
        new List<runnertk_vison>();

    [SerializeField]
    private runnertk_vison currentTarget;


    // ============================================================
    // OBSTACLE INTERFACE
    // ============================================================

    [Header("Obstacle Interface")]

    [HideInInspector]
    public Transform[] obstacleCenters;


    // ============================================================
    // CAMERA
    // ============================================================

    [Header("Forward Monocular Camera")]

    public Camera localCamera;

    public float visualRange = 50f;

    public LayerMask visibilityBlockers;


    // ============================================================
    // FALLBACK FOV
    // ============================================================

    [Header("Fallback FOV")]

    public float horizontalFov = 82.6f;

    public float verticalFov = 53f;


    // ============================================================
    // OBSERVATION
    //
    // SELF = 4
    //
    // Each Runner = 9
    //
    // visible
    // bearingX
    // bearingY
    // apparentSize
    // deltaBearingX
    // deltaBearingY
    // deltaApparentSize
    // active
    // isCurrentTarget
    //
    // 5 Runner slots:
    //
    // 5 * 9 = 45
    //
    // Obstacle:
    //
    // 5 * 5 = 25
    //
    // TOTAL:
    //
    // 4 + 45 + 25 = 74
    // ============================================================

    [Header("Observation Slots")]

    public int maxObservedRunners = 5;

    public int maxObservedObstacles = 5;


    // ============================================================
    // MOTION
    //
    // Action 0 = Forward
    // Action 1 = Vertical
    // Action 2 = Yaw
    // ============================================================

    [Header("Motion")]

    public float speed = 0.6f;

    public float maxSpeed = 0.6f;

    public float yawSpeed = 1.0f;


    [SerializeField]
    private float rawForward;

    [SerializeField]
    private float rawVertical;

    [SerializeField]
    private float rawYaw;


    // ============================================================
    // SPAWN
    // ============================================================

    [Header("Chaser Spawn - LOCAL")]

    public Vector3 spawnMin =
        new Vector3(
            -5.99f,
            2.11f,
            -4.4f
        );

    public Vector3 spawnMax =
        new Vector3(
            8.05f,
            4.32f,
            9.29f
        );


    [Header("Spawn Safety")]

    public LayerMask spawnBlockingLayer;

    public float spawnRadius = 0.1f;

    public int spawnAttempts = 200;

    public float minRunnerSpawnDistance = 3.0f;

    public float minObstacleSpawnDistance = 3.0f;


    // ============================================================
    // TERMINAL REWARD
    // ============================================================

    [Header("Terminal Reward")]

    public float catchReward = 1000f;

    public float allRunnerCaughtReward = 500f;

    public float wallCrashPenalty = -1500f;


    // ============================================================
    // DISCOVERY
    // ============================================================

    [Header("Discovery")]

    public float runnerDiscoveryReward = 300f;


    // ============================================================
    // EXPLORE
    // ============================================================

    [Header("Explore Behaviour")]

    [Range(0f, 1f)]
    public float exploreYawWeight = 0.25f;

    [Range(0f, 1f)]
    public float exploreForwardWeight = 0.75f;

    [Range(0f, 1f)]
    public float preferredExploreYaw = 0.6f;

    [Range(0f, 1f)]
    public float preferredExploreForward = 0.6f;

    public float exploreSearchRewardScale = 0.08f;

    public float exploreVerticalPenalty = 0.05f;

    [Range(0f, 1f)]
    public float excessiveYawThreshold = 0.85f;

    public float excessiveYawPenalty = 0.2f;


    public float exploreStepPenalty = -0.12f;


    [Header("Explore Entry Forward Shaping")]

    [Tooltip(
        "For the first few Explore decisions, add only a mild forward preference. This shaping is skipped automatically if the phase immediately changes to Pursuit or Avoidance."
    )]
    public int exploreEntryForwardSteps = 5;

    [Range(0f, 1f)]
    public float exploreEntryForwardTarget = 0.35f;

    [Range(0f, 1f)]
    public float exploreEntryVeryLowForwardThreshold = 0.15f;

    [Tooltip(
        "Small reward for moving forward during the short Explore-entry window."
    )]
    public float exploreEntryForwardRewardScale = 0.015f;

    [Tooltip(
        "Small penalty only for almost-stationary behaviour during the Explore-entry window."
    )]
    public float exploreEntryLowForwardPenalty = -0.015f;


    [Header("Initial Heading")]

    [Tooltip(
        "At spawn, point approximately toward the center of the active Runners so training focuses on obstacle avoidance rather than wasting samples facing a wall."
    )]
    public bool faceRunnerCenterOnSpawn = true;

    [Range(0f, 45f)]
    public float initialHeadingNoiseDegrees = 15f;


    // ============================================================
    // PURSUIT
    // ============================================================

    [Header("Pursuit")]

    public float targetVisibleReward = 0.03f;

    public float targetLostPenalty = -0.4f;

    public float pursuitStepPenalty = -0.06f;


    // ============================================================
    // ALIGN THEN ADVANCE
    // ============================================================

    [Header("Align Then Advance")]

    [Range(0f, 1f)]
    public float forwardGateStart = 0.45f;

    [Range(0f, 1f)]
    public float forwardGateFull = 0.85f;

    [Range(0f, 1f)]
    public float offCenterThreshold = 0.55f;

    [Range(0f, 1f)]
    public float fastForwardThreshold = 0.45f;


    public float centerForwardRewardScale = 0.25f;

    public float offCenterForwardPenalty = -0.35f;

    public float centeringRewardScale = 0.05f;

    public float preferredForwardSpeed = 0.50f;


    // ============================================================
    // TEMPORAL VISUAL TRACKING
    // ============================================================

    [Header("Temporal Visual Tracking")]

    public float centerProgressRewardScale = 6.0f;

    public float apparentSizeProgressRewardScale = 3.0f;

    public float maxVisualDelta = 0.15f;


    // ============================================================
    // OBSTACLE AVOIDANCE
    //
    // Dedicated phase with highest priority. Explore/Pursuit reward
    // is paused while Avoidance is active.
    //
    // No reward for obstacle becoming smaller.
    //
    // Reward:
    // light visual edge-shift + synchronized forward shaping,
    // followed by light recenter shaping and a true-pass clear reward.
    //
    // Reward hierarchy is tuned so Pursuit/Capture > Avoidance > Explore.
    // ============================================================

    [Header("Obstacle Avoidance Reward")]

    [Tooltip(
        "Scale for obstacle moving from image center toward image edge."
    )]
    public float obstacleEscapeProgressRewardScale = 2.0f;


    [Tooltip(
        "Very small reward for continuing forward while obstacle is moving outward."
    )]
    public float obstacleForwardRewardScale = 0.05f;


    [Tooltip(
        "Small one-time reward when a locked obstacle leaves the camera view."
    )]
    public float obstacleClearReward = 200f;


    [Tooltip(
        "Clamp for one-step obstacle image-space progress."
    )]
    public float maxObstacleVisualDelta = 0.15f;


    [Tooltip(
        "Absolute clamp for the signed obstacle shaping reward per decision."
    )]
    public float maxObstacleStepReward = 0.10f;


    [Tooltip(
        "Penalty for physically colliding with an obstacle."
    )]
    public float obstacleCrashPenalty = -1000f;


    [Range(0f, 1f)]
    [Tooltip(
        "Obstacle avoidance reward is active only when the obstacle occupies at least this fraction of the camera image width."
    )]
    public float obstacleAvoidWidthThreshold = 0.30f;


    [Range(0f, 1f)]
    [Tooltip(
        "Once Avoidance has started, an enrolled obstacle is considered cleared when its visible screen width falls to this fraction or below."
    )]
    public float obstacleAvoidExitWidthThreshold = 0.05f;


    [Range(0f, 1f)]
    [Tooltip(
        "Positive obstacle escape reward is allowed only when actual forward speed reaches at least this normalized fraction."
    )]
    public float obstacleMinForwardForPositiveReward = 0.40f;


    [Tooltip(
        "Per-decision time penalty used only during Avoidance. Stronger than Pursuit so the agent is discouraged from staying on one obstacle."
    )]
    public float avoidanceStepPenalty = -0.08f;


    [Tooltip(
        "Additional penalty when the Chaser stays below the minimum forward score during Avoidance."
    )]

    public float obstacleLowForwardPenalty = -0.05f;


    [Header("Obstacle Recenter / Success Teacher")]

    [Tooltip(
        "Small shaping scale for reducing yaw-heading error after the obstacle has reached the visual exit band. This guides recentering without prescribing a fixed turn angle."
    )]
    public float obstacleRecenterRewardScale = 0.08f;

    [Tooltip(
        "Small forward bonus during recentering. It is deliberately much smaller than the final clear reward."
    )]
    public float obstacleRecenterForwardRewardScale = 0.02f;

    [Range(1f, 45f)]
    [Tooltip(
        "A successful bypass should finish with the Chaser facing approximately the same horizontal direction as when Avoidance started."
    )]
    public float obstacleRecenterHeadingToleranceDegrees = 20f;

    [Tooltip(
        "Training-only geometric margin beyond the obstacle plane required before the large clear reward is allowed. This value is never added to the observation."
    )]
    public float obstaclePassMargin = 0.25f;

    [Tooltip(
        "Small penalty if the Chaser is in recentering but stops moving forward."
    )]
    public float obstacleRecenterLowForwardPenalty = -0.03f;


    // ============================================================
    // EPISODE STATE
    // ============================================================

    private bool episodeFinished = false;

    public bool isCrashed = false;


    // ============================================================
    // STATS
    // ============================================================

    [Header("TensorBoard")]

    public bool enableCustomStats = true;


    private int episodeCaptureCount = 0;

    private int episodeDiscoveryCount = 0;

    private int pursuitDecisionCount = 0;

    private int pursuitVisibleDecisionCount = 0;

    private float accumulatedCenterScore = 0f;

    private float accumulatedCenterProgress = 0f;

    private float accumulatedSizeProgress = 0f;


    // ============================================================
    // VISUAL INFO
    // ============================================================

    public struct VisualInfo
    {
        public bool visible;

        public float bearingX;

        public float bearingY;

        public float apparentSize;
    }


    // ============================================================
    // TEMPORAL VISUAL MEMORY
    // ============================================================

    private class RunnerVisualMemory
    {
        public bool hasPrevious = false;

        public bool previousVisible = false;

        public float previousBearingX = 0f;

        public float previousBearingY = 0f;

        public float previousApparentSize = 0f;
    }


    private Dictionary<runnertk_vison, RunnerVisualMemory>
        runnerVisualMemory =
            new Dictionary<runnertk_vison, RunnerVisualMemory>();


    // ============================================================
    // CURRENT TARGET TEMPORAL STATE
    // ============================================================

    private float currentDeltaBearingX = 0f;

    private float currentDeltaBearingY = 0f;

    private float currentDeltaApparentSize = 0f;

    private float currentCenterProgress = 0f;

    private bool currentTargetHasTemporalData = false;


    // ============================================================
    // OBSTACLE AVOIDANCE TEMPORAL STATE
    //
    // Avoidance is a dedicated phase.
    // Each obstacle that crosses the 30% entry threshold gets its
    // own temporal memory, so multiple close obstacles cannot mix
    // their step-to-step center-error history.
    // ============================================================

    private class ObstacleAvoidanceMemory
    {
        public bool hasPreviousCenterError = false;

        public float previousCenterError = 0f;

        // True after at least one valid 30% -> edge visual escape step
        // happened while the Chaser was actually moving forward.
        public bool hadForwardEscapeProgress = false;


        // --------------------------------------------------------
        // TRAINING-ONLY SUCCESS / RECENTER MEMORY
        //
        // These values are NEVER added to CollectObservations().
        // They are privileged reward-teacher state only.
        // --------------------------------------------------------

        public Vector3 entryForwardDirection =
            Vector3.forward;

        public Vector3 obstaclePositionAtEntry =
            Vector3.zero;

        public Vector3 approachDirection =
            Vector3.forward;

        public bool recenterStarted =
            false;

        public bool hasPreviousHeadingError =
            false;

        public float previousHeadingError =
            0f;
    }


    private int exploreDecisionsSinceEntry = 0;


    private Dictionary<Transform, ObstacleAvoidanceMemory>
        obstacleAvoidanceMemory =
            new Dictionary<Transform, ObstacleAvoidanceMemory>();


    private HashSet<Transform> activeAvoidanceObstacles =
        new HashSet<Transform>();


    // Once an obstacle has been successfully cleared in this episode,
    // it may still trigger Avoidance again for safety, but it can no
    // longer produce positive obstacle step reward or another clear reward.
    private HashSet<Transform> clearedObstaclesThisEpisode =
        new HashSet<Transform>();


    private HashSet<runnertk_vison> discoveredRunnersThisEpisode =
        new HashSet<runnertk_vison>();


    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void Awake()
    {
        EnsureReferences();
    }


    protected virtual void Start()
    {
        EnsureReferences();
    }


    private void EnsureReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
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


        if (environment != null)
        {
            runners =
                environment.runners;
        }


        if (rb != null)
        {
            rb.useGravity = false;
        }
    }


    // ============================================================
    // FIXED UPDATE
    // ============================================================

    private void FixedUpdate()
    {
        if (
            episodeFinished ||
            isCrashed
        )
        {
            return;
        }


        // ========================================================
        // AVOIDANCE HAS HIGHEST PRIORITY
        //
        // This lets the phase change as soon as a close obstacle is
        // visible, even between DecisionRequester decisions.
        // ========================================================

        UpdateAvoidancePhaseTransitions();


        if (
            currentPhase !=
            PursuitPhase.Explore
        )
        {
            return;
        }


        if (TryLockVisibleRunner())
        {
            StopMotion();
        }
    }


    // ============================================================
    // EPISODE BEGIN
    // ============================================================

    public override void OnEpisodeBegin()
    {
        EnsureReferences();


        if (environment != null)
        {
            environment
                .BeginEnvironmentEpisode(
                    this
                );
        }
        else
        {
            ResetChaserForEpisode();
        }
    }


    // ============================================================
    // RESET
    // ============================================================

    public void ResetChaserForEpisode()
    {
        EnsureReferences();


        currentPhase =
            PursuitPhase.Explore;


        currentTarget =
            null;


        episodeFinished =
            false;


        isCrashed =
            false;


        rawForward = 0f;
        rawVertical = 0f;
        rawYaw = 0f;

        exploreDecisionsSinceEntry = 0;


        runnerVisualMemory.Clear();


        ResetCurrentTemporalState();


        ResetObstacleAvoidanceState();

        clearedObstaclesThisEpisode.Clear();

        discoveredRunnersThisEpisode.Clear();


        episodeCaptureCount = 0;

        episodeDiscoveryCount = 0;

        pursuitDecisionCount = 0;

        pursuitVisibleDecisionCount = 0;

        accumulatedCenterScore = 0f;

        accumulatedCenterProgress = 0f;

        accumulatedSizeProgress = 0f;


        Collider col =
            GetComponent<Collider>();


        if (col != null)
        {
            col.enabled = true;
        }


        if (rb != null)
        {
            rb.detectCollisions = true;

            rb.useGravity = false;

            rb.velocity = Vector3.zero;

            rb.angularVelocity = Vector3.zero;
        }


        TakeOff();
    }


    // ============================================================
    // RESET CURRENT TEMPORAL STATE
    // ============================================================

    private void ResetCurrentTemporalState()
    {
        currentDeltaBearingX = 0f;

        currentDeltaBearingY = 0f;

        currentDeltaApparentSize = 0f;

        currentCenterProgress = 0f;

        currentTargetHasTemporalData = false;
    }


    // ============================================================
    // STOP MOTION
    // ============================================================

    private void StopMotion()
    {
        rawForward = 0f;
        rawVertical = 0f;
        rawYaw = 0f;


        if (rb != null)
        {
            rb.velocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }
    }


    // ============================================================
    // TAKEOFF
    // ============================================================

    public virtual void TakeOff()
    {
        Vector3 selected =
            transform.localPosition;


        bool found = false;


        for (
            int i = 0;
            i < Mathf.Max(
                1,
                spawnAttempts
            );
            i++
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


        if (found)
        {
            transform.localPosition =
                selected;
        }


        bool usedRunnerHeading =
            false;


        if (
            faceRunnerCenterOnSpawn &&
            environment != null &&
            environment.runners != null
        )
        {
            Vector3 runnerCenter =
                Vector3.zero;


            int validRunnerCount =
                0;


            foreach (
                runnertk_vison runner
                in environment.runners
            )
            {
                if (
                    runner == null ||
                    runner.isCaught
                )
                {
                    continue;
                }


                runnerCenter +=
                    runner.transform.position;


                validRunnerCount++;
            }


            if (
                validRunnerCount > 0
            )
            {
                runnerCenter /=
                    validRunnerCount;


                Vector3 flatDirection =
                    runnerCenter
                    -
                    transform.position;


                flatDirection.y =
                    0f;


                if (
                    flatDirection.sqrMagnitude >
                    0.0001f
                )
                {
                    Quaternion lookRotation =
                        Quaternion.LookRotation(
                            flatDirection.normalized,
                            Vector3.up
                        );


                    float yawNoise =
                        Random.Range(
                            -initialHeadingNoiseDegrees,
                            initialHeadingNoiseDegrees
                        );


                    transform.rotation =
                        lookRotation
                        *
                        Quaternion.Euler(
                            0f,
                            yawNoise,
                            0f
                        );


                    usedRunnerHeading =
                        true;
                }
            }
        }


        if (!usedRunnerHeading)
        {
            float randomYaw =
                Random.Range(
                    0f,
                    360f
                );


            transform.localRotation =
                Quaternion.Euler(
                    0f,
                    randomYaw,
                    0f
                );
        }


        StopMotion();
    }


    // ============================================================
    // SPAWN SAFETY
    // ============================================================

    private bool IsSpawnPositionSafe(
        Vector3 localPosition)
    {
        Vector3 worldPosition =
            transform.parent != null
                ? transform.parent
                    .TransformPoint(
                        localPosition
                    )
                : localPosition;


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


            if (hits.Length > 0)
            {
                return false;
            }
        }


        // ========================================================
        // RUNNER DISTANCE >= 3
        // ========================================================

        if (
            environment != null &&
            environment.runners != null
        )
        {
            foreach (
                runnertk_vison runner
                in environment.runners
            )
            {
                if (runner == null)
                {
                    continue;
                }


                if (
                    Vector3.Distance(
                        worldPosition,
                        runner.transform.position
                    )
                    <
                    minRunnerSpawnDistance
                )
                {
                    return false;
                }
            }
        }


        // ========================================================
        // ACTIVE OBSTACLE DISTANCE >= 3
        // ========================================================

        if (
            environment != null &&
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
                    minObstacleSpawnDistance
                )
                {
                    return false;
                }
            }
        }


        return true;
    }


    // ============================================================
    // COLLECT OBSERVATIONS
    //
    // TOTAL = 74
    // ============================================================

    public override void CollectObservations(
        VectorSensor sensor)
    {
        Vector3 localVelocity =
            transform
                .InverseTransformDirection(
                    rb != null
                        ? rb.velocity
                        : Vector3.zero
                );


        float safeSpeed =
            Mathf.Max(
                maxSpeed,
                0.001f
            );


        // ========================================================
        // SELF 1 - FORWARD SPEED
        // ========================================================

        sensor.AddObservation(
            Mathf.Clamp(
                localVelocity.z
                /
                safeSpeed,
                -1f,
                1f
            )
        );


        // ========================================================
        // SELF 2 - VERTICAL SPEED
        // ========================================================

        sensor.AddObservation(
            Mathf.Clamp(
                localVelocity.y
                /
                safeSpeed,
                -1f,
                1f
            )
        );


        // ========================================================
        // SELF 3 - YAW RATE
        // ========================================================

        sensor.AddObservation(
            rb != null
                ? Mathf.Clamp(
                    rb.angularVelocity.y
                    /
                    Mathf.Max(
                        yawSpeed,
                        0.001f
                    ),
                    -1f,
                    1f
                )
                : 0f
        );


        // ========================================================
        // SELF 4 - HAS TARGET
        // ========================================================

        sensor.AddObservation(
            HasValidTarget()
                ? 1f
                : 0f
        );


        ResetCurrentTemporalState();


        // ========================================================
        // RUNNERS
        // ========================================================

        for (
            int i = 0;
            i < maxObservedRunners;
            i++
        )
        {
            runnertk_vison runner =
                GetRunner(i);


            AddRunnerObservation(
                sensor,
                runner
            );
        }


        // ========================================================
        // OBSTACLES
        //
        // visible
        // bearingX
        // bearingY
        // screenWidthRatio
        // active
        //
        // screenWidthRatio is the same visual quantity used by the
        // 20% / 5% Avoidance thresholds.
        // ========================================================

        for (
            int i = 0;
            i < maxObservedObstacles;
            i++
        )
        {
            Transform obstacle =
                GetObstacle(i);


            AddObstacleObservation(
                sensor,
                obstacle
            );
        }
    }


    // ============================================================
    // RUNNER OBSERVATION
    //
    // 9 dimensions
    // ============================================================

    private void AddRunnerObservation(
        VectorSensor sensor,
        runnertk_vison runner)
    {
        if (
            runner == null ||
            runner.isCaught
        )
        {
            AddEmptyRunnerObservation(
                sensor
            );


            return;
        }


        VisualInfo info =
            GetVisualInfo(
                runner.transform
            );


        if (
            !runnerVisualMemory
                .TryGetValue(
                    runner,
                    out RunnerVisualMemory memory
                )
        )
        {
            memory =
                new RunnerVisualMemory();


            runnerVisualMemory.Add(
                runner,
                memory
            );
        }


        float deltaBearingX = 0f;

        float deltaBearingY = 0f;

        float deltaSize = 0f;

        float centerProgress = 0f;

        bool temporalValid = false;


        if (
            info.visible &&
            memory.hasPrevious &&
            memory.previousVisible
        )
        {
            deltaBearingX =
                info.bearingX
                -
                memory.previousBearingX;


            deltaBearingY =
                info.bearingY
                -
                memory.previousBearingY;


            deltaSize =
                info.apparentSize
                -
                memory.previousApparentSize;


            deltaBearingX =
                Mathf.Clamp(
                    deltaBearingX,
                    -maxVisualDelta,
                    maxVisualDelta
                );


            deltaBearingY =
                Mathf.Clamp(
                    deltaBearingY,
                    -maxVisualDelta,
                    maxVisualDelta
                );


            deltaSize =
                Mathf.Clamp(
                    deltaSize,
                    -maxVisualDelta,
                    maxVisualDelta
                );


            float previousCenterError =
                Mathf.Sqrt(
                    memory.previousBearingX
                    *
                    memory.previousBearingX
                    +
                    memory.previousBearingY
                    *
                    memory.previousBearingY
                );


            float currentCenterError =
                Mathf.Sqrt(
                    info.bearingX
                    *
                    info.bearingX
                    +
                    info.bearingY
                    *
                    info.bearingY
                );


            centerProgress =
                previousCenterError
                -
                currentCenterError;


            centerProgress =
                Mathf.Clamp(
                    centerProgress,
                    -maxVisualDelta,
                    maxVisualDelta
                );


            temporalValid =
                true;
        }


        if (
            runner ==
            currentTarget
        )
        {
            currentDeltaBearingX =
                deltaBearingX;


            currentDeltaBearingY =
                deltaBearingY;


            currentDeltaApparentSize =
                deltaSize;


            currentCenterProgress =
                centerProgress;


            currentTargetHasTemporalData =
                temporalValid;
        }


        sensor.AddObservation(
            info.visible
                ? 1f
                : 0f
        );


        sensor.AddObservation(
            info.visible
                ? info.bearingX
                : 0f
        );


        sensor.AddObservation(
            info.visible
                ? info.bearingY
                : 0f
        );


        sensor.AddObservation(
            info.visible
                ? info.apparentSize
                : 0f
        );


        sensor.AddObservation(
            temporalValid
                ? deltaBearingX
                : 0f
        );


        sensor.AddObservation(
            temporalValid
                ? deltaBearingY
                : 0f
        );


        sensor.AddObservation(
            temporalValid
                ? deltaSize
                : 0f
        );


        sensor.AddObservation(
            1f
        );


        sensor.AddObservation(
            runner == currentTarget
                ? 1f
                : 0f
        );


        memory.previousVisible =
            info.visible;


        if (info.visible)
        {
            memory.previousBearingX =
                info.bearingX;


            memory.previousBearingY =
                info.bearingY;


            memory.previousApparentSize =
                info.apparentSize;
        }


        memory.hasPrevious =
            true;
    }
    // ============================================================
    // EMPTY RUNNER = 9
    // ============================================================

    private void AddEmptyRunnerObservation(
        VectorSensor sensor)
    {
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);

        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);

        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
    }


    // ============================================================
    // OBSTACLE OBSERVATION
    //
    // 5 dimensions:
    //
    // visible
    // bearingX
    // bearingY
    // screenWidthRatio
    // active
    //
    // screenWidthRatio is observation only.
    // Avoidance reward does NOT directly reward size decrease.
    // ============================================================

    private void AddObstacleObservation(
        VectorSensor sensor,
        Transform obstacle)
    {
        if (
            obstacle == null ||
            !obstacle.gameObject.activeInHierarchy
        )
        {
            AddEmptyObstacleObservation(
                sensor
            );

            return;
        }


        VisualInfo info =
            GetVisualInfo(
                obstacle
            );


        sensor.AddObservation(
            info.visible
                ? 1f
                : 0f
        );


        sensor.AddObservation(
            info.visible
                ? info.bearingX
                : 0f
        );


        sensor.AddObservation(
            info.visible
                ? info.bearingY
                : 0f
        );


        float screenWidthRatio =
            info.visible
                ? GetObstacleScreenWidthRatio(
                    obstacle
                )
                : 0f;


        sensor.AddObservation(
            screenWidthRatio
        );


        sensor.AddObservation(
            1f
        );
    }


    // ============================================================
    // EMPTY OBSTACLE = 5
    // ============================================================

    private void AddEmptyObstacleObservation(
        VectorSensor sensor)
    {
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
    }


    // ============================================================
    // ACTION
    // ============================================================

    public override void OnActionReceived(
        ActionBuffers actionBuffers)
    {
        if (
            episodeFinished ||
            isCrashed
        )
        {
            return;
        }


        if (
            actionBuffers
                .ContinuousActions
                .Length < 3
        )
        {
            return;
        }


        // ========================================================
        // PHASE PRIORITY
        //
        // Avoidance > Pursuit > Explore
        //
        // If a close obstacle appears while chasing a Runner, the
        // Pursuit phase is interrupted BEFORE this decision receives
        // any Pursuit reward.
        // ========================================================

        UpdateAvoidancePhaseTransitions();


        // ========================================================
        // TARGET LOCK SAFETY CHECK
        //
        // After Avoidance returns to Explore, a visible Runner can be
        // reacquired immediately. Discovery reward is still paid only
        // once per Runner per episode.
        // ========================================================

        if (
            currentPhase ==
            PursuitPhase.Explore
        )
        {
            bool justLocked =
                TryLockVisibleRunner();


            if (justLocked)
            {
                StopMotion();

                return;
            }
        }


        // ========================================================
        // POLICY ACTION
        // ========================================================

        float policyForward =
            Mathf.Clamp01(
                actionBuffers
                    .ContinuousActions[0]
            );


        float policyVertical =
            Mathf.Clamp(
                actionBuffers
                    .ContinuousActions[1],
                -1f,
                1f
            );


        float policyYaw =
            Mathf.Clamp(
                actionBuffers
                    .ContinuousActions[2],
                -1f,
                1f
            );


        // ========================================================
        // EXPLORE
        // ========================================================

        if (
            currentPhase ==
            PursuitPhase.Explore
        )
        {
            rawForward =
                policyForward;


            rawVertical =
                policyVertical
                *
                0.35f;


            rawYaw =
                policyYaw;
        }

        // ========================================================
        // PURSUIT / AVOIDANCE
        //
        // Both use the full 3-D low-level action range. Their rewards
        // are separated by phase below.
        // ========================================================

        else
        {
            rawForward =
                policyForward;


            rawVertical =
                policyVertical;


            rawYaw =
                policyYaw;
        }


        // ========================================================
        // APPLY MOTION
        // ========================================================

        ApplyFlightAction();


        // ========================================================
        // REWARD
        // ========================================================

        ApplyTrainingReward();
    }


    // ============================================================
    // FLIGHT ACTION
    // ============================================================

    private void ApplyFlightAction()
    {
        if (rb == null)
        {
            return;
        }


        Vector3 localCommand =
            new Vector3(
                0f,
                rawVertical,
                rawForward
            );


        if (
            localCommand.magnitude >
            1f
        )
        {
            localCommand.Normalize();
        }


        Vector3 worldVelocity =
            transform.TransformDirection(
                localCommand
            )
            *
            speed;


        if (
            worldVelocity.magnitude >
            maxSpeed
        )
        {
            worldVelocity =
                worldVelocity.normalized
                *
                maxSpeed;
        }


        rb.velocity =
            worldVelocity;


        Vector3 angularVelocity =
            rb.angularVelocity;


        angularVelocity.x = 0f;

        angularVelocity.z = 0f;

        angularVelocity.y =
            rawYaw
            *
            yawSpeed;


        rb.angularVelocity =
            angularVelocity;
    }


    // ============================================================
    // TRAINING REWARD
    // ============================================================

    private void ApplyTrainingReward()
    {
        // ========================================================
        // EXPLORE
        //
        // Explore reward only. No obstacle reward is mixed in.
        // ========================================================

        if (
            currentPhase ==
            PursuitPhase.Explore
        )
        {
            AddReward(
                exploreStepPenalty
            );


            ApplyExploreReward();

            return;
        }


        // ========================================================
        // PURSUIT
        //
        // Pursuit reward only. If a >=30% single obstacle appears, the phase
        // was already changed to Avoidance before reaching here.
        // ========================================================

        if (
            currentPhase ==
            PursuitPhase.Pursuit
        )
        {
            AddReward(
                pursuitStepPenalty
            );


            ApplyPursuitReward();

            return;
        }


        // ========================================================
        // AVOIDANCE
        //
        // No Explore shaping and no Pursuit shaping are active here.
        // Only obstacles that crossed the entry threshold are used.
        // ========================================================

        if (
            currentPhase ==
            PursuitPhase.Avoidance
        )
        {
            // Avoidance has its own stronger time pressure so the
            // policy is discouraged from lingering around one obstacle.
            AddReward(
                avoidanceStepPenalty
            );


            ApplyObstacleAvoidanceReward();

            return;
        }
    }


    // ============================================================
    // EXPLORE REWARD
    // ============================================================

    private void ApplyExploreReward()
    {
        float yawMagnitude =
            Mathf.Abs(
                rawYaw
            );


        float yawDifference =
            Mathf.Abs(
                yawMagnitude
                -
                preferredExploreYaw
            );


        float yawScore =
            1f
            -
            Mathf.Clamp01(
                yawDifference
                /
                Mathf.Max(
                    preferredExploreYaw,
                    0.001f
                )
            );


        float forwardDifference =
            Mathf.Abs(
                rawForward
                -
                preferredExploreForward
            );


        float forwardScore =
            1f
            -
            Mathf.Clamp01(
                forwardDifference
                /
                Mathf.Max(
                    preferredExploreForward,
                    0.001f
                )
            );


        float weightSum =
            Mathf.Max(
                exploreYawWeight
                +
                exploreForwardWeight,
                0.001f
            );


        float yawWeight =
            exploreYawWeight
            /
            weightSum;


        float forwardWeight =
            exploreForwardWeight
            /
            weightSum;


        float searchScore =
            yawScore
            *
            yawWeight
            +
            forwardScore
            *
            forwardWeight;


        AddReward(
            searchScore
            *
            exploreSearchRewardScale
        );


        // ========================================================
        // MILD FORWARD PREFERENCE AT THE START OF EXPLORE
        //
        // This is intentionally weak. UpdateAvoidancePhaseTransitions()
        // and TryLockVisibleRunner() run BEFORE this reward, so if the
        // agent immediately enters Avoidance or Pursuit this shaping is
        // never applied.
        // ========================================================

        if (
            exploreDecisionsSinceEntry <
            Mathf.Max(
                0,
                exploreEntryForwardSteps
            )
        )
        {
            float entryForwardScore =
                Mathf.Clamp01(
                    rawForward
                    /
                    Mathf.Max(
                        exploreEntryForwardTarget,
                        0.001f
                    )
                );


            AddReward(
                entryForwardScore
                *
                exploreEntryForwardRewardScale
            );


            if (
                rawForward <
                exploreEntryVeryLowForwardThreshold
            )
            {
                AddReward(
                    exploreEntryLowForwardPenalty
                );
            }
        }


        exploreDecisionsSinceEntry++;


        if (
            yawMagnitude >
            excessiveYawThreshold
        )
        {
            float excessiveAmount =
                Mathf.InverseLerp(
                    excessiveYawThreshold,
                    1f,
                    yawMagnitude
                );


            AddReward(
                -excessiveAmount
                *
                excessiveYawPenalty
            );
        }


        AddReward(
            -Mathf.Abs(
                rawVertical
            )
            *
            exploreVerticalPenalty
        );
    }


    // ============================================================
    // PURSUIT REWARD
    // ============================================================

    private void ApplyPursuitReward()
    {
        if (!HasValidTarget())
        {
            ReturnToExplorePhase();

            return;
        }


        pursuitDecisionCount++;


        VisualInfo info =
            GetVisualInfo(
                currentTarget.transform
            );


        // ========================================================
        // LOST
        // ========================================================

        if (!info.visible)
        {
            AddReward(
                targetLostPenalty
            );

            return;
        }


        pursuitVisibleDecisionCount++;


        // ========================================================
        // SMALL VISIBLE REWARD
        // ========================================================

        AddReward(
            targetVisibleReward
        );


        // ========================================================
        // TEMPORAL TRACKING
        // ========================================================

        if (currentTargetHasTemporalData)
        {
            float centerProgressReward =
                currentCenterProgress
                *
                centerProgressRewardScale;


            AddReward(
                centerProgressReward
            );


            accumulatedCenterProgress +=
                currentCenterProgress;


            float sizeProgressReward =
                currentDeltaApparentSize
                *
                apparentSizeProgressRewardScale;


            AddReward(
                sizeProgressReward
            );


            accumulatedSizeProgress +=
                currentDeltaApparentSize;
        }


        // ========================================================
        // CURRENT CENTER SCORE
        // ========================================================

        float horizontalCenter =
            1f
            -
            Mathf.Clamp01(
                Mathf.Abs(
                    info.bearingX
                )
            );


        float verticalCenter =
            1f
            -
            Mathf.Clamp01(
                Mathf.Abs(
                    info.bearingY
                )
            );


        float centerScore =
            horizontalCenter
            *
            0.8f
            +
            verticalCenter
            *
            0.2f;


        accumulatedCenterScore +=
            centerScore;


        AddReward(
            centerScore
            *
            centeringRewardScale
        );


        // ========================================================
        // ACTUAL FORWARD SPEED
        // ========================================================

        Vector3 localVelocity =
            transform
                .InverseTransformDirection(
                    rb != null
                        ? rb.velocity
                        : Vector3.zero
                );


        float forwardSpeed =
            Mathf.Max(
                0f,
                localVelocity.z
            );


        float forwardScore =
            Mathf.Clamp01(
                forwardSpeed
                /
                Mathf.Max(
                    preferredForwardSpeed,
                    0.01f
                )
            );


        // ========================================================
        // CENTER GATE
        // ========================================================

        float centerGate =
            Mathf.InverseLerp(
                forwardGateStart,
                forwardGateFull,
                centerScore
            );


        AddReward(
            centerGate
            *
            forwardScore
            *
            centerForwardRewardScale
        );


        // ========================================================
        // OFF CENTER + FAST FORWARD
        // ========================================================

        if (
            centerScore <
            offCenterThreshold
            &&
            forwardScore >
            fastForwardThreshold
        )
        {
            AddReward(
                offCenterForwardPenalty
            );
        }
    }


    // ============================================================
    // OBSTACLE AVOIDANCE PHASE
    //
    // ENTRY:
    // Any single visible obstacle with screen-width ratio >= 30% enters the
    // Avoidance phase immediately, even if Pursuit is active.
    //
    // ACTIVE SET:
    // Every obstacle that crosses the entry threshold is enrolled.
    // Each enrolled obstacle keeps its own temporal memory.
    //
    // EXIT:
    // An enrolled obstacle is cleared when its visible width falls to
    // <= 5% or it is no longer visible. Avoidance ends only when all
    // enrolled obstacles are cleared. The agent then returns to Explore.
    // ============================================================

    private void UpdateAvoidancePhaseTransitions()
    {
        if (
            episodeFinished ||
            isCrashed
        )
        {
            return;
        }


        // ========================================================
        // NOT CURRENTLY AVOIDING
        // ========================================================

        if (
            currentPhase !=
            PursuitPhase.Avoidance
        )
        {
            List<Transform> entryThreats =
                FindObstacleEntryThreats();


            if (
                entryThreats.Count > 0
            )
            {
                EnterAvoidancePhase(
                    entryThreats
                );
            }


            return;
        }


        // New obstacles may join while Avoidance is active.
        AddNewObstacleEntryThreats();


        List<Transform> toRemove =
            new List<Transform>();


        foreach (
            Transform obstacle
            in activeAvoidanceObstacles
        )
        {
            if (
                obstacle == null ||
                !obstacle.gameObject.activeInHierarchy
            )
            {
                toRemove.Add(
                    obstacle
                );

                continue;
            }


            if (
                !obstacleAvoidanceMemory.TryGetValue(
                    obstacle,
                    out ObstacleAvoidanceMemory memory
                )
            )
            {
                continue;
            }


            float screenWidthRatio =
                GetObstacleScreenWidthRatio(
                    obstacle
                );


            bool stillVisible =
                IsObstacleVisibleForAvoidance(
                    obstacle,
                    screenWidthRatio
                );


            // ----------------------------------------------------
            // 5% IS NOT SUCCESS ANY MORE.
            //
            // It only starts the recenter part of Avoidance.
            // A pure camera-yaw escape therefore cannot immediately
            // collect the large clear reward.
            // ----------------------------------------------------

            if (
                !memory.recenterStarted &&
                memory.hadForwardEscapeProgress &&
                (
                    !stillVisible ||
                    screenWidthRatio <=
                    obstacleAvoidExitWidthThreshold
                )
            )
            {
                memory.recenterStarted =
                    true;


                memory.previousHeadingError =
                    GetAvoidanceHeadingError(
                        memory
                    );


                memory.hasPreviousHeadingError =
                    true;
            }


            if (
                IsAvoidanceObstacleTrulyCleared(
                    obstacle,
                    memory
                )
            )
            {
                if (
                    !clearedObstaclesThisEpisode.Contains(
                        obstacle
                    )
                )
                {
                    AddReward(
                        obstacleClearReward
                    );


                    clearedObstaclesThisEpisode.Add(
                        obstacle
                    );
                }


                toRemove.Add(
                    obstacle
                );
            }
        }


        foreach (
            Transform obstacle
            in toRemove
        )
        {
            activeAvoidanceObstacles.Remove(
                obstacle
            );


            if (
                obstacle != null
            )
            {
                obstacleAvoidanceMemory.Remove(
                    obstacle
                );
            }
        }


        if (
            activeAvoidanceObstacles.Count == 0
        )
        {
            ExitAvoidanceToExplore();
        }
    }


    // ============================================================
    // FIND ENTRY THREATS
    // ============================================================

    private List<Transform> FindObstacleEntryThreats()
    {
        List<Transform> threats =
            new List<Transform>();


        int obstacleCount =
            GetAvailableObstacleCount();


        int count =
            Mathf.Min(
                maxObservedObstacles,
                obstacleCount
            );


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            Transform obstacle =
                GetObstacle(i);


            if (
                obstacle == null ||
                !obstacle.gameObject.activeInHierarchy
            )
            {
                continue;
            }


            VisualInfo info =
                GetVisualInfo(
                    obstacle
                );


            if (!info.visible)
            {
                continue;
            }


            float screenWidthRatio =
                GetObstacleScreenWidthRatio(
                    obstacle
                );


            if (
                screenWidthRatio <
                obstacleAvoidWidthThreshold
            )
            {
                continue;
            }


            if (
                !IsObstacleVisibleForAvoidance(
                    obstacle,
                    screenWidthRatio
                )
            )
            {
                continue;
            }


            threats.Add(
                obstacle
            );
        }


        return threats;
    }


    // ============================================================
    // ENTER AVOIDANCE
    // ============================================================

    private void EnterAvoidancePhase(
        List<Transform> entryThreats)
    {
        if (
            entryThreats == null ||
            entryThreats.Count == 0
        )
        {
            return;
        }


        // Pursuit is intentionally interrupted. After avoidance, the
        // system returns to Explore and may immediately reacquire a Runner.
        currentTarget =
            null;


        currentPhase =
            PursuitPhase.Avoidance;


        ResetCurrentTemporalState();


        activeAvoidanceObstacles.Clear();

        obstacleAvoidanceMemory.Clear();


        foreach (
            Transform obstacle
            in entryThreats
        )
        {
            EnrollAvoidanceObstacle(
                obstacle
            );
        }


        StopMotion();
    }


    // ============================================================
    // ADD NEW ENTRY THREATS DURING AVOIDANCE
    // ============================================================

    private void AddNewObstacleEntryThreats()
    {
        List<Transform> entryThreats =
            FindObstacleEntryThreats();


        foreach (
            Transform obstacle
            in entryThreats
        )
        {
            if (
                activeAvoidanceObstacles.Contains(
                    obstacle
                )
            )
            {
                continue;
            }


            EnrollAvoidanceObstacle(
                obstacle
            );
        }
    }


    // ============================================================
    // ENROLL ONE OBSTACLE
    // ============================================================

    private void EnrollAvoidanceObstacle(
        Transform obstacle)
    {
        if (
            obstacle == null
        )
        {
            return;
        }


        activeAvoidanceObstacles.Add(
            obstacle
        );


        if (
            !obstacleAvoidanceMemory.ContainsKey(
                obstacle
            )
        )
        {
            ObstacleAvoidanceMemory memory =
                new ObstacleAvoidanceMemory();


            Vector3 entryForward =
                transform.forward;


            entryForward.y =
                0f;


            if (
                entryForward.sqrMagnitude <
                0.0001f
            )
            {
                entryForward =
                    Vector3.forward;
            }


            entryForward.Normalize();


            Vector3 approach =
                obstacle.position
                -
                transform.position;


            approach.y =
                0f;


            if (
                approach.sqrMagnitude <
                0.0001f
            )
            {
                approach =
                    entryForward;
            }


            approach.Normalize();


            memory.entryForwardDirection =
                entryForward;


            memory.obstaclePositionAtEntry =
                obstacle.position;


            memory.approachDirection =
                approach;


            obstacleAvoidanceMemory.Add(
                obstacle,
                memory
            );
        }
    }


    // ============================================================
    // EXIT AVOIDANCE -> EXPLORE
    // ============================================================

    private void ExitAvoidanceToExplore()
    {
        ResetObstacleAvoidanceState();


        currentTarget =
            null;


        currentPhase =
            PursuitPhase.Explore;


        exploreDecisionsSinceEntry =
            0;


        ResetCurrentTemporalState();


        StopMotion();
    }


    // ============================================================
    // OBSTACLE AVOIDANCE REWARD
    //
    // Only active during Avoidance.
    //
    // IMPORTANT:
    // An obstacle must first cross the 30% entry threshold to be
    // enrolled. Once enrolled, its reward continues while it moves
    // from 30% down toward the 5% exit threshold. This avoids a
    // 5%-20% dead zone with no learning signal.
    //
    // If multiple enrolled obstacles are valid in the same decision,
    // their step rewards are averaged so reward magnitude does not grow
    // simply because more obstacles are visible.
    // ============================================================

    private void ApplyObstacleAvoidanceReward()
    {
        if (
            currentPhase !=
            PursuitPhase.Avoidance
        )
        {
            return;
        }


        AddNewObstacleEntryThreats();


        Vector3 phaseLocalVelocity =
            transform
                .InverseTransformDirection(
                    rb != null
                        ? rb.velocity
                        : Vector3.zero
                );


        float phaseForwardSpeed =
            Mathf.Max(
                0f,
                phaseLocalVelocity.z
            );


        float phaseForwardScore =
            Mathf.Clamp01(
                phaseForwardSpeed
                /
                Mathf.Max(
                    preferredForwardSpeed,
                    0.01f
                )
            );


        if (
            phaseForwardScore <
            obstacleMinForwardForPositiveReward
        )
        {
            AddReward(
                obstacleLowForwardPenalty
            );
        }


        float totalStepReward =
            0f;


        int validStepRewardCount =
            0;


        foreach (
            Transform obstacle
            in activeAvoidanceObstacles
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
                !obstacleAvoidanceMemory.TryGetValue(
                    obstacle,
                    out ObstacleAvoidanceMemory memory
                )
            )
            {
                continue;
            }


            float screenWidthRatio =
                GetObstacleScreenWidthRatio(
                    obstacle
                );


            bool stillVisible =
                IsObstacleVisibleForAvoidance(
                    obstacle,
                    screenWidthRatio
                );


            // ====================================================
            // BYPASS SHAPING: 30% -> 5%
            //
            // Keep the old useful signal: move the obstacle from the
            // image centre toward an edge WHILE actually moving forward.
            // This is only light guidance. The final success reward still
            // requires physically passing the obstacle and recovering the
            // original general heading.
            // ====================================================

            if (!memory.recenterStarted)
            {
                if (
                    memory.hadForwardEscapeProgress &&
                    (
                        !stillVisible ||
                        screenWidthRatio <=
                        obstacleAvoidExitWidthThreshold
                    )
                )
                {
                    memory.recenterStarted =
                        true;


                    memory.previousHeadingError =
                        GetAvoidanceHeadingError(
                            memory
                        );


                    memory.hasPreviousHeadingError =
                        true;


                    continue;
                }


                if (!stillVisible)
                {
                    memory.hasPreviousCenterError =
                        false;

                    continue;
                }


                VisualInfo info =
                    GetVisualInfo(
                        obstacle
                    );


                if (!info.visible)
                {
                    memory.hasPreviousCenterError =
                        false;

                    continue;
                }


                float currentCenterError =
                    GetVisualCenterError(
                        info
                    );


                if (
                    !memory.hasPreviousCenterError
                )
                {
                    memory.previousCenterError =
                        currentCenterError;


                    memory.hasPreviousCenterError =
                        true;

                    continue;
                }


                float escapeProgress =
                    currentCenterError
                    -
                    memory.previousCenterError;


                escapeProgress =
                    Mathf.Clamp(
                        escapeProgress,
                        -maxObstacleVisualDelta,
                        maxObstacleVisualDelta
                    );


                float obstacleStepReward =
                    0f;


                if (
                    escapeProgress > 0f
                )
                {
                    bool positiveRewardAvailable =
                        !clearedObstaclesThisEpisode.Contains(
                            obstacle
                        );


                    if (
                        positiveRewardAvailable &&
                        phaseForwardScore >=
                        obstacleMinForwardForPositiveReward
                    )
                    {
                        obstacleStepReward =
                            escapeProgress
                            *
                            obstacleEscapeProgressRewardScale
                            *
                            phaseForwardScore;


                        obstacleStepReward +=
                            phaseForwardScore
                            *
                            obstacleForwardRewardScale;


                        memory.hadForwardEscapeProgress =
                            true;
                    }
                }
                else if (
                    escapeProgress < 0f
                )
                {
                    obstacleStepReward =
                        escapeProgress
                        *
                        obstacleEscapeProgressRewardScale;
                }


                obstacleStepReward =
                    Mathf.Clamp(
                        obstacleStepReward,
                        -maxObstacleStepReward,
                        maxObstacleStepReward
                    );


                totalStepReward +=
                    obstacleStepReward;


                validStepRewardCount++;


                memory.previousCenterError =
                    currentCenterError;


                continue;
            }


            // ====================================================
            // RECENTER SHAPING
            //
            // Do NOT prescribe a fixed return angle or fixed duration.
            // Reward only the direction of improvement:
            // 1) heading error relative to Avoidance-entry direction shrinks;
            // 2) forward motion continues.
            // ====================================================

            float currentHeadingError =
                GetAvoidanceHeadingError(
                    memory
                );


            if (
                !memory.hasPreviousHeadingError
            )
            {
                memory.previousHeadingError =
                    currentHeadingError;


                memory.hasPreviousHeadingError =
                    true;

                continue;
            }


            float headingImprovement =
                memory.previousHeadingError
                -
                currentHeadingError;


            float normalizedHeadingImprovement =
                Mathf.Clamp(
                    headingImprovement
                    /
                    15f,
                    -1f,
                    1f
                );


            float recenterReward =
                normalizedHeadingImprovement
                *
                obstacleRecenterRewardScale
                *
                phaseForwardScore;


            if (
                phaseForwardScore >=
                obstacleMinForwardForPositiveReward
            )
            {
                recenterReward +=
                    phaseForwardScore
                    *
                    obstacleRecenterForwardRewardScale;
            }
            else
            {
                recenterReward +=
                    obstacleRecenterLowForwardPenalty;
            }


            recenterReward =
                Mathf.Clamp(
                    recenterReward,
                    -maxObstacleStepReward,
                    maxObstacleStepReward
                );


            totalStepReward +=
                recenterReward;


            validStepRewardCount++;


            memory.previousHeadingError =
                currentHeadingError;
        }


        if (
            validStepRewardCount <= 0
        )
        {
            return;
        }


        float meanObstacleStepReward =
            totalStepReward
            /
            validStepRewardCount;


        meanObstacleStepReward =
            Mathf.Clamp(
                meanObstacleStepReward,
                -maxObstacleStepReward,
                maxObstacleStepReward
            );


        AddReward(
            meanObstacleStepReward
        );
    }


    // ============================================================
    // OBSTACLE VISIBILITY FOR AVOIDANCE
    //
    // Entry needs a real visible screen footprint. Using the projected
    // renderer width also lets a large obstacle remain relevant when its
    // center approaches an image edge.
    // ============================================================

    private bool IsObstacleVisibleForAvoidance(
        Transform obstacle,
        float screenWidthRatio)
    {
        if (
            obstacle == null ||
            localCamera == null ||
            screenWidthRatio <= 0f
        )
        {
            return false;
        }


        Renderer renderer =
            obstacle
                .GetComponentInChildren<Renderer>();


        if (
            renderer == null
        )
        {
            return false;
        }


        Vector3 viewportCenter =
            localCamera
                .WorldToViewportPoint(
                    renderer.bounds.center
                );


        return
            viewportCenter.z > 0f;
    }


    // ============================================================
    // OBSTACLE SCREEN WIDTH RATIO
    //
    // Returns the fraction of the camera image width occupied by
    // the obstacle renderer bounds. Example: 0.20 = one fifth of
    // the image width. This value is used only to decide whether
    // obstacle avoidance reward should be active.
    // ============================================================

    private float GetObstacleScreenWidthRatio(
        Transform obstacle)
    {
        if (
            obstacle == null ||
            localCamera == null
        )
        {
            return 0f;
        }


        Renderer renderer =
            obstacle
                .GetComponentInChildren<Renderer>();


        if (renderer == null)
        {
            return 0f;
        }


        Bounds bounds =
            renderer.bounds;


        Vector3 center =
            bounds.center;


        Vector3 extents =
            bounds.extents;


        float minViewportX =
            float.PositiveInfinity;


        float maxViewportX =
            float.NegativeInfinity;


        float minViewportY =
            float.PositiveInfinity;


        float maxViewportY =
            float.NegativeInfinity;


        bool hasPointInFront =
            false;


        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner =
                        center
                        +
                        new Vector3(
                            extents.x * x,
                            extents.y * y,
                            extents.z * z
                        );


                    Vector3 viewport =
                        localCamera
                            .WorldToViewportPoint(
                                corner
                            );


                    if (viewport.z <= 0f)
                    {
                        continue;
                    }


                    hasPointInFront =
                        true;


                    minViewportX =
                        Mathf.Min(
                            minViewportX,
                            viewport.x
                        );


                    maxViewportX =
                        Mathf.Max(
                            maxViewportX,
                            viewport.x
                        );


                    minViewportY =
                        Mathf.Min(
                            minViewportY,
                            viewport.y
                        );


                    maxViewportY =
                        Mathf.Max(
                            maxViewportY,
                            viewport.y
                        );
                }
            }
        }


        if (!hasPointInFront)
        {
            return 0f;
        }


        // If the obstacle has no vertical overlap with the image, it
        // should not keep the Avoidance phase alive merely because its
        // projected X interval overlaps the camera width.
        if (
            maxViewportY <= 0f ||
            minViewportY >= 1f
        )
        {
            return 0f;
        }


        float visibleMinX =
            Mathf.Clamp01(
                minViewportX
            );


        float visibleMaxX =
            Mathf.Clamp01(
                maxViewportX
            );


        return
            Mathf.Clamp01(
                visibleMaxX
                -
                visibleMinX
            );
    }


    // ============================================================
    // VISUAL CENTER ERROR
    // ============================================================

    private float GetVisualCenterError(
        VisualInfo info)
    {
        return
            Mathf.Sqrt(
                info.bearingX
                *
                info.bearingX
                +
                info.bearingY
                *
                info.bearingY
            );
    }


    // ============================================================
    // AVOIDANCE TRAINING-ONLY GEOMETRY HELPERS
    // ============================================================

    private float GetAvoidanceHeadingError(
        ObstacleAvoidanceMemory memory)
    {
        if (memory == null)
        {
            return 180f;
        }


        Vector3 currentForward =
            transform.forward;


        currentForward.y =
            0f;


        if (
            currentForward.sqrMagnitude <
            0.0001f
        )
        {
            return 180f;
        }


        currentForward.Normalize();


        return
            Vector3.Angle(
                currentForward,
                memory.entryForwardDirection
            );
    }


    private float GetAvoidancePassProgress(
        ObstacleAvoidanceMemory memory)
    {
        if (memory == null)
        {
            return float.NegativeInfinity;
        }


        Vector3 fromObstacle =
            transform.position
            -
            memory.obstaclePositionAtEntry;


        fromObstacle.y =
            0f;


        return
            Vector3.Dot(
                fromObstacle,
                memory.approachDirection
            );
    }


    private bool IsAvoidanceObstacleTrulyCleared(
        Transform obstacle,
        ObstacleAvoidanceMemory memory)
    {
        if (
            obstacle == null ||
            memory == null ||
            !memory.recenterStarted ||
            !memory.hadForwardEscapeProgress
        )
        {
            return false;
        }


        float passProgress =
            GetAvoidancePassProgress(
                memory
            );


        if (
            passProgress <
            obstaclePassMargin
        )
        {
            return false;
        }


        float headingError =
            GetAvoidanceHeadingError(
                memory
            );


        return
            headingError <=
            obstacleRecenterHeadingToleranceDegrees;
    }


    // ============================================================
    // RESET OBSTACLE AVOIDANCE STATE
    // ============================================================

    private void ResetObstacleAvoidanceState()
    {
        activeAvoidanceObstacles.Clear();

        obstacleAvoidanceMemory.Clear();
    }


    // ============================================================
    // TARGET LOCK
    // ============================================================

    private bool TryLockVisibleRunner()
    {
        if (
            currentPhase !=
            PursuitPhase.Explore
        )
        {
            return false;
        }


        if (HasValidTarget())
        {
            return false;
        }


        if (
            environment == null ||
            environment.runners == null
        )
        {
            return false;
        }


        runnertk_vison bestRunner =
            null;


        float bestScore =
            float.NegativeInfinity;


        foreach (
            runnertk_vison runner
            in environment.runners
        )
        {
            if (
                runner == null ||
                runner.isCaught
            )
            {
                continue;
            }


            VisualInfo info =
                GetVisualInfo(
                    runner.transform
                );


            if (!info.visible)
            {
                continue;
            }


            float centerPenalty =
                Mathf.Abs(
                    info.bearingX
                )
                +
                0.5f
                *
                Mathf.Abs(
                    info.bearingY
                );


            float score =
                info.apparentSize
                -
                0.15f
                *
                centerPenalty;


            if (
                bestRunner == null ||
                score > bestScore
            )
            {
                bestRunner =
                    runner;


                bestScore =
                    score;
            }
        }


        if (bestRunner == null)
        {
            return false;
        }


        EnterPursuitPhase(
            bestRunner
        );


        return true;
    }
    // ============================================================
    // ENTER PURSUIT
    // ============================================================

    private void EnterPursuitPhase(
        runnertk_vison runner)
    {
        if (
            runner == null ||
            runner.isCaught
        )
        {
            return;
        }


        currentTarget =
            runner;


        currentPhase =
            PursuitPhase.Pursuit;


        if (
            runnerVisualMemory
                .TryGetValue(
                    runner,
                    out RunnerVisualMemory memory
                )
        )
        {
            memory.hasPrevious =
                false;


            memory.previousVisible =
                false;
        }


        ResetCurrentTemporalState();


        if (
            !discoveredRunnersThisEpisode.Contains(
                runner
            )
        )
        {
            discoveredRunnersThisEpisode.Add(
                runner
            );


            episodeDiscoveryCount++;


            AddReward(
                runnerDiscoveryReward
            );
        }
    }


    // ============================================================
    // RETURN EXPLORE
    // ============================================================

    private void ReturnToExplorePhase()
    {
        currentTarget =
            null;


        currentPhase =
            PursuitPhase.Explore;


        exploreDecisionsSinceEntry =
            0;


        ResetCurrentTemporalState();


        StopMotion();
    }


    // ============================================================
    // HAS TARGET
    // ============================================================

    public bool HasValidTarget()
    {
        return
            currentTarget != null
            &&
            !currentTarget.isCaught;
    }


    // ============================================================
    // CAMERA VISUAL INFO
    // ============================================================

    private VisualInfo GetVisualInfo(
        Transform target)
    {
        VisualInfo info =
            new VisualInfo();


        if (
            target == null
        )
        {
            return info;
        }


        Vector3 visualPoint =
            target.position;


        Renderer renderer =
            target
                .GetComponentInChildren<Renderer>();


        if (
            renderer != null
        )
        {
            visualPoint =
                renderer.bounds.center;
        }


        // ========================================================
        // CAMERA
        // ========================================================

        if (
            localCamera != null
        )
        {
            Vector3 cameraPosition =
                localCamera
                    .transform
                    .position;


            float distance =
                Vector3.Distance(
                    cameraPosition,
                    visualPoint
                );


            if (
                distance <= 0.001f ||
                distance > visualRange
            )
            {
                return info;
            }


            Vector3 viewport =
                localCamera
                    .WorldToViewportPoint(
                        visualPoint
                    );


            if (
                viewport.z <= 0f
            )
            {
                return info;
            }


            if (
                viewport.x < 0f ||
                viewport.x > 1f ||
                viewport.y < 0f ||
                viewport.y > 1f
            )
            {
                return info;
            }


            if (
                IsOccluded(
                    cameraPosition,
                    visualPoint,
                    target
                )
            )
            {
                return info;
            }


            info.visible =
                true;


            info.bearingX =
                Mathf.Clamp(
                    (
                        viewport.x
                        -
                        0.5f
                    )
                    *
                    2f,
                    -1f,
                    1f
                );


            info.bearingY =
                Mathf.Clamp(
                    (
                        viewport.y
                        -
                        0.5f
                    )
                    *
                    2f,
                    -1f,
                    1f
                );


            info.apparentSize =
                EstimateApparentSize(
                    target,
                    distance
                );


            return info;
        }


        return
            GetFallbackVisualInfo(
                target,
                visualPoint
            );
    }


    // ============================================================
    // FALLBACK VISUAL
    // ============================================================

    private VisualInfo GetFallbackVisualInfo(
        Transform target,
        Vector3 visualPoint)
    {
        VisualInfo info =
            new VisualInfo();


        Transform cameraTransform =
            transform;


        Vector3 toTarget =
            visualPoint
            -
            cameraTransform.position;


        float distance =
            toTarget.magnitude;


        if (
            distance <= 0.001f ||
            distance > visualRange
        )
        {
            return info;
        }


        Vector3 localDirection =
            cameraTransform
                .InverseTransformDirection(
                    toTarget.normalized
                );


        if (
            localDirection.z <= 0f
        )
        {
            return info;
        }


        float horizontalAngle =
            Mathf.Atan2(
                localDirection.x,
                localDirection.z
            )
            *
            Mathf.Rad2Deg;


        float horizontalPlane =
            Mathf.Sqrt(
                localDirection.x
                *
                localDirection.x
                +
                localDirection.z
                *
                localDirection.z
            );


        float verticalAngle =
            Mathf.Atan2(
                localDirection.y,
                horizontalPlane
            )
            *
            Mathf.Rad2Deg;


        if (
            Mathf.Abs(
                horizontalAngle
            )
            >
            horizontalFov
            *
            0.5f
        )
        {
            return info;
        }


        if (
            Mathf.Abs(
                verticalAngle
            )
            >
            verticalFov
            *
            0.5f
        )
        {
            return info;
        }


        if (
            IsOccluded(
                cameraTransform.position,
                visualPoint,
                target
            )
        )
        {
            return info;
        }


        info.visible =
            true;


        info.bearingX =
            Mathf.Clamp(
                horizontalAngle
                /
                Mathf.Max(
                    horizontalFov
                    *
                    0.5f,
                    0.001f
                ),
                -1f,
                1f
            );


        info.bearingY =
            Mathf.Clamp(
                verticalAngle
                /
                Mathf.Max(
                    verticalFov
                    *
                    0.5f,
                    0.001f
                ),
                -1f,
                1f
            );


        info.apparentSize =
            EstimateApparentSize(
                target,
                distance
            );


        return info;
    }


    // ============================================================
    // OCCLUSION
    // ============================================================

    private bool IsOccluded(
        Vector3 cameraPosition,
        Vector3 visualPoint,
        Transform target)
    {
        if (
            target == null ||
            visibilityBlockers.value == 0
        )
        {
            return false;
        }


        Vector3 direction =
            visualPoint
            -
            cameraPosition;


        float distance =
            direction.magnitude;


        if (
            distance <= 0.001f
        )
        {
            return false;
        }


        if (
            Physics.Raycast(
                cameraPosition,

                direction.normalized,

                out RaycastHit hit,

                distance,

                visibilityBlockers,

                QueryTriggerInteraction.Ignore
            )
        )
        {
            Transform hitTransform =
                hit.transform;


            if (
                hitTransform == target ||
                hitTransform.IsChildOf(
                    target
                ) ||
                target.IsChildOf(
                    hitTransform
                )
            )
            {
                return false;
            }


            return true;
        }


        return false;
    }


    // ============================================================
    // APPARENT SIZE
    // ============================================================

    private float EstimateApparentSize(
        Transform target,
        float distance)
    {
        float objectScale =
            0.5f;


        Renderer renderer =
            target
                .GetComponentInChildren<Renderer>();


        if (
            renderer != null
        )
        {
            objectScale =
                Mathf.Max(
                    0.1f,
                    renderer
                        .bounds
                        .extents
                        .magnitude
                );
        }


        return
            Mathf.Clamp01(
                objectScale
                /
                Mathf.Max(
                    distance,
                    0.05f
                )
            );
    }


    // ============================================================
    // WALL
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


    // ============================================================
    // WALL COLLISION
    // ============================================================

    private void HandleWallCollision()
    {
        if (
            episodeFinished
        )
        {
            return;
        }


        AddReward(
            wallCrashPenalty
        );


        episodeFinished =
            true;


        isCrashed =
            true;


        StopMotion();


        if (
            environment != null
        )
        {
            environment
                .EndEnvironmentEpisode(
                    GlobalVariebles_vison
                        .EpisodeEndReason
                        .WallCollision
                );
        }
        else
        {
            EndEpisode();
        }
    }


    // ============================================================
    // OBSTACLE COLLIDER
    // ============================================================

    private bool IsObstacle(
        Collider col)
    {
        if (
            col == null
        )
        {
            return false;
        }


        Transform hitTransform =
            col.transform;


        int obstacleCount =
            GetAvailableObstacleCount();


        for (
            int i = 0;
            i < obstacleCount;
            i++
        )
        {
            Transform obstacle =
                GetObstacle(i);


            if (
                obstacle == null
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


    // ============================================================
    // OBSTACLE COLLISION
    // ============================================================

    private void HandleObstacleCollision()
    {
        if (
            episodeFinished
        )
        {
            return;
        }


        AddReward(
            obstacleCrashPenalty
        );


        episodeFinished =
            true;


        isCrashed =
            true;


        StopMotion();


        if (
            environment != null
        )
        {
            environment
                .EndEnvironmentEpisode(
                    GlobalVariebles_vison
                        .EpisodeEndReason
                        .ObstacleCollision
                );
        }
        else
        {
            EndEpisode();
        }
    }


    // ============================================================
    // RUNNER COLLIDER
    // ============================================================

    private bool TryCatchRunnerFromCollider(
        Collider other)
    {
        if (
            other == null ||
            episodeFinished
        )
        {
            return false;
        }


        runnertk_vison runner =
            other
                .GetComponentInParent<runnertk_vison>();


        if (
            runner == null ||
            runner.isCaught
        )
        {
            return false;
        }


        if (
            currentPhase !=
            PursuitPhase.Pursuit
            ||
            runner != currentTarget
        )
        {
            return false;
        }


        CatchRunner(
            runner
        );


        return true;
    }


    // ============================================================
    // TRIGGER
    // ============================================================

    private void OnTriggerEnter(
        Collider other)
    {
        if (
            other == null ||
            episodeFinished
        )
        {
            return;
        }


        if (
            IsWall(
                other
            )
        )
        {
            HandleWallCollision();


            return;
        }


        if (
            IsObstacle(
                other
            )
        )
        {
            HandleObstacleCollision();


            return;
        }


        TryCatchRunnerFromCollider(
            other
        );
    }


    // ============================================================
    // NORMAL COLLISION
    // ============================================================

    private void OnCollisionEnter(
        Collision collision)
    {
        if (
            collision == null ||
            collision.collider == null ||
            episodeFinished
        )
        {
            return;
        }


        Collider other =
            collision.collider;


        if (
            IsWall(
                other
            )
        )
        {
            HandleWallCollision();


            return;
        }


        if (
            IsObstacle(
                other
            )
        )
        {
            HandleObstacleCollision();


            return;
        }


        TryCatchRunnerFromCollider(
            other
        );
    }


    // ============================================================
    // CATCH
    // ============================================================

    private void CatchRunner(
        runnertk_vison runner)
    {
        if (
            runner == null ||
            runner.isCaught ||
            episodeFinished
        )
        {
            return;
        }


        if (
            currentPhase !=
            PursuitPhase.Pursuit
            ||
            runner !=
            currentTarget
        )
        {
            return;
        }


        bool allCaught =
            false;


        if (
            environment != null
        )
        {
            allCaught =
                environment
                    .RegisterRunnerCaught(
                        runner
                    );
        }
        else
        {
            runner.getCaught();


            GlobalVariebles_vison
                .RunnerGetCaughtNum++;


            allCaught =
                GlobalVariebles_vison
                    .RunnerGetCaughtNum
                >=
                GetRunnerCount();
        }


        // ========================================================
        // NORMAL CHASER CAPTURE REWARD
        //
        // Runner crashing by itself does NOT come through here.
        // ========================================================

        episodeCaptureCount++;


        AddReward(
            catchReward
        );


        currentTarget =
            null;


        ResetCurrentTemporalState();


        StopMotion();


        // ========================================================
        // ALL RUNNERS CAUGHT BY NORMAL COUNTING
        // ========================================================

        if (
            allCaught
        )
        {
            AddReward(
                allRunnerCaughtReward
            );


            episodeFinished =
                true;


            if (
                environment != null
            )
            {
                environment
                    .EndEnvironmentEpisode(
                        GlobalVariebles_vison
                            .EpisodeEndReason
                            .AllRunnersCaught
                    );
            }
            else
            {
                EndEpisode();
            }


            return;
        }


        // ========================================================
        // MORE RUNNERS REMAIN
        // ========================================================

        ReturnToExplorePhase();
    }
    // ============================================================
    // TENSORBOARD
    // ============================================================

    public void OnEnvironmentEpisodeEnding(
        GlobalVariebles_vison.EpisodeEndReason reason,
        int episodeSteps)
    {
        if (!enableCustomStats)
        {
            return;
        }


        var stats =
            Academy
                .Instance
                .StatsRecorder;


        int runnerCount =
            GetRunnerCount();


        float captureRate =
            runnerCount > 0
                ? Mathf.Clamp01(
                    (float)
                    episodeCaptureCount
                    /
                    runnerCount
                )
                : 0f;


        stats.Add(
            "Custom/CaptureRate",
            captureRate
        );


        stats.Add(
            "Custom/CaptureCount",
            episodeCaptureCount
        );


        stats.Add(
            "Custom/DiscoveryCount",
            episodeDiscoveryCount
        );


        stats.Add(
            "Custom/EpisodeLength",
            episodeSteps
        );


        stats.Add(
            "Custom/WallCollisionRate",

            reason ==
                GlobalVariebles_vison
                    .EpisodeEndReason
                    .WallCollision
                ? 1f
                : 0f
        );


        stats.Add(
            "Custom/TimeoutRate",

            reason ==
                GlobalVariebles_vison
                    .EpisodeEndReason
                    .Timeout
                ? 1f
                : 0f
        );


        stats.Add(
            "Custom/SuccessRate",

            reason ==
                GlobalVariebles_vison
                    .EpisodeEndReason
                    .AllRunnersCaught
                ? 1f
                : 0f
        );


        float visibleRate =
            pursuitDecisionCount > 0
                ? Mathf.Clamp01(
                    (float)
                    pursuitVisibleDecisionCount
                    /
                    pursuitDecisionCount
                )
                : 0f;


        stats.Add(
            "Custom/TargetVisibleRate",
            visibleRate
        );


        float meanCentering =
            pursuitVisibleDecisionCount > 0
                ? accumulatedCenterScore
                  /
                  pursuitVisibleDecisionCount
                : 0f;


        stats.Add(
            "Custom/MeanTargetCentering",
            meanCentering
        );


        float meanCenterProgress =
            pursuitVisibleDecisionCount > 0
                ? accumulatedCenterProgress
                  /
                  pursuitVisibleDecisionCount
                : 0f;


        stats.Add(
            "Custom/MeanCenterProgress",
            meanCenterProgress
        );


        float meanSizeProgress =
            pursuitVisibleDecisionCount > 0
                ? accumulatedSizeProgress
                  /
                  pursuitVisibleDecisionCount
                : 0f;


        stats.Add(
            "Custom/MeanSizeProgress",
            meanSizeProgress
        );
    }


    // ============================================================
    // HELPERS
    // ============================================================

    private int GetAvailableObstacleCount()
    {
        if (
            environment != null &&
            environment.ActiveObstacles != null
        )
        {
            return
                environment.ActiveObstacles.Count;
        }


        if (
            obstacleCenters != null
        )
        {
            return
                obstacleCenters.Length;
        }


        return 0;
    }


    private Transform GetObstacle(
        int index)
    {
        if (
            index < 0
        )
        {
            return null;
        }


        // ========================================================
        // NORMAL PATH
        //
        // Global owns obstacle randomization.
        // Every Chaser reads the active obstacle list.
        // ========================================================

        if (
            environment != null &&
            environment.ActiveObstacles != null
        )
        {
            if (
                index >=
                environment.ActiveObstacles.Count
            )
            {
                return null;
            }


            return
                environment.ActiveObstacles[index];
        }


        // ========================================================
        // FALLBACK
        // ========================================================

        if (
            obstacleCenters == null ||
            index >= obstacleCenters.Length
        )
        {
            return null;
        }


        return
            obstacleCenters[index];
    }


    private runnertk_vison GetRunner(
        int index)
    {
        if (
            environment == null ||
            environment.runners == null ||
            index < 0 ||
            index >=
            environment.runners.Count
        )
        {
            return null;
        }


        return
            environment.runners[index];
    }


    private int GetRunnerCount()
    {
        if (
            environment == null ||
            environment.runners == null
        )
        {
            return 0;
        }


        int count =
            0;


        foreach (
            runnertk_vison runner
            in environment.runners
        )
        {
            if (
                runner != null
            )
            {
                count++;
            }
        }


        return count;
    }


    // ============================================================
    // LEADER COMPATIBILITY
    // ============================================================

    public void SetLeaderHold(
        bool hold)
    {
    }


    public void ReceiveRunnerHint(
        runnertk_vison runner,
        float coarseBearing,
        float confidence)
    {
    }


    public void ReceiveObstacleHint(
        Transform obstacle,
        float coarseBearing,
        float nearWarning,
        float confidence)
    {
    }


    // ============================================================
    // HEURISTIC
    // ============================================================

    public override void Heuristic(
        in ActionBuffers actionsOut)
    {
        var actions =
            actionsOut
                .ContinuousActions;


        if (
            actions.Length < 3
        )
        {
            return;
        }


        actions[0] =
            0f;


        actions[1] =
            0f;


        actions[2] =
            0f;
    }
}