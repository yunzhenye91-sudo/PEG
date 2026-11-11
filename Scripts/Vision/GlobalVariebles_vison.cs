using System.Collections.Generic;
using UnityEngine;


public class GlobalVariebles_vison : MonoBehaviour
{
    public enum EpisodeEndReason
    {
        None,
        Timeout,
        AllRunnersCaught,
        AllRunnersEliminated,
        ObstacleCollision,
        WallCollision,
        DroneCollision,
        External
    }


    public enum ObstacleTrainingScenario
    {
        ChaserToBothRunners,
        ChaserAndRunnerBridge,
        FullyRandom
    }



    [Header("Agents")]

    public List<chasertk_vison> chasers =
        new List<chasertk_vison>();

    public List<runnertk_vison> runners =
        new List<runnertk_vison>();


    [Header("Episode")]

    public int maxEpisodeSteps =
        2000;

    private int episodeSteps =
        0;

    private bool episodeActive =
        false;

    private bool isEndingEpisode =
        false;


    public static int RunnerGetCaughtNum =
        0;

    public static bool IsGameOver =
        false;


    [Header("Obstacle Pool")]

    public List<Transform> obstacles =
        new List<Transform>();


    [Header("Random Active Obstacle Count")]

    public bool randomizeActiveObstacleCount =
        true;

    public int minActiveObstacles =
        1;

    public int maxActiveObstacles =
        5;


    [Header("Obstacle Random Spawn - LOCAL XZ")]

    public Vector2 obstacleSpawnMinXZ =
        new Vector2(
            -3.21f,
            -1.53f
        );

    public Vector2 obstacleSpawnMaxXZ =
        new Vector2(
            4.62f,
            6.75f
        );

    public float minObstacleSpacing =
        3.0f;

    public float minAgentClearance =
        3.0f;

    public List<Transform> obstacleProtectedPoints =
        new List<Transform>();

    public float minProtectedPointClearance =
        1.5f;

    public int obstacleSpawnAttempts =
        200;

    public bool randomizeObstacleYaw =
        false;


    [Header("Obstacle Training Scenario Mix")]

    [Range(0f, 1f)]
    public float scenario1Probability =
        0.55f;

    [Range(0f, 1f)]
    public float scenario2Probability =
        0.30f;

    [Range(0f, 1f)]
    public float scenario3Probability =
        0.15f;


    [Header("Directed Obstacle Noise")]

    [Range(0.05f, 0.95f)]
    public float directedPathMinT =
        0.28f;

    [Range(0.05f, 0.95f)]
    public float directedPathMaxT =
        0.62f;

    public float directedLateralNoise =
        0.65f;

    public float directedAgentClearance =
        1.25f;

    public int directedSpawnAttempts =
        250;


    [Header("Initial Training Scenario State")]

    [SerializeField]
    private ObstacleTrainingScenario currentObstacleScenario =
        ObstacleTrainingScenario.ChaserToBothRunners;


    private readonly List<Transform>
        activeObstacles =
            new List<Transform>();


    public IReadOnlyList<Transform> ActiveObstacles
    {
        get
        {
            return activeObstacles;
        }
    }


    public int RunnerCount
    {
        get
        {
            int count =
                0;

            if (
                runners == null
            )
            {
                return count;
            }

            foreach (
                runnertk_vison runner
                in runners
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
    }


    public int ChaserCount
    {
        get
        {
            int count =
                0;

            if (
                chasers == null
            )
            {
                return count;
            }

            foreach (
                chasertk_vison chaser
                in chasers
            )
            {
                if (
                    chaser != null
                )
                {
                    count++;
                }
            }

            return count;
        }
    }


    private void FixedUpdate()
    {
        if (
            !episodeActive ||
            isEndingEpisode
        )
        {
            return;
        }

        episodeSteps++;

        if (
            episodeSteps >=
            maxEpisodeSteps
        )
        {
            EndEnvironmentEpisode(
                EpisodeEndReason.Timeout
            );
        }
    }


    public void BeginEnvironmentEpisode(
        chasertk_vison requester)
    {
        if (
            episodeActive ||
            isEndingEpisode
        )
        {
            return;
        }


        episodeActive =
            true;


        episodeSteps =
            0;


        RunnerGetCaughtNum =
            0;


        IsGameOver =
            false;


        // ========================================================
        // RESET ORDER FOR DIRECTED TRAINING
        //
        // 1) Remove the previous obstacle layout.
        // 2) Reset Runners so their new positions are known.
        // 3) Reset Chasers. Their TakeOff() can now face the Runner area.
        // 4) Build the new structured/noisy obstacle layout around the
        //    actual Chaser/Runner positions.
        // ========================================================

        DeactivateAllObstacles();


        if (
            runners != null
        )
        {
            foreach (
                runnertk_vison runner
                in runners
            )
            {
                if (
                    runner != null
                )
                {
                    runner.ResetRunner();
                }
            }
        }


        if (
            chasers != null
        )
        {
            foreach (
                chasertk_vison chaser
                in chasers
            )
            {
                if (
                    chaser != null
                )
                {
                    chaser
                        .ResetChaserForEpisode();
                }
            }
        }


        RandomizeObstacleLayout();
    }


    public bool RegisterRunnerCaught(
        runnertk_vison runner)
    {
        if (
            !episodeActive ||
            isEndingEpisode ||
            runner == null ||
            runner.isCaught
        )
        {
            return false;
        }

        runner.getCaught();

        RunnerGetCaughtNum++;

        int totalRunnerCount =
            RunnerCount;

        if (
            totalRunnerCount > 0 &&
            RunnerGetCaughtNum >=
            totalRunnerCount
        )
        {
            return true;
        }

        return false;
    }


    public bool RegisterRunnerEliminated(
        runnertk_vison runner)
    {
        if (
            !episodeActive ||
            isEndingEpisode ||
            runner == null ||
            runner.isCaught
        )
        {
            return false;
        }

        runner.getCaught();

        RunnerGetCaughtNum++;

        int totalRunnerCount =
            RunnerCount;

        if (
            totalRunnerCount > 0 &&
            RunnerGetCaughtNum >=
            totalRunnerCount
        )
        {
            return true;
        }

        return false;
    }


    public void EndEnvironmentEpisode(
        EpisodeEndReason reason)
    {
        if (
            !episodeActive ||
            isEndingEpisode
        )
        {
            return;
        }

        isEndingEpisode =
            true;

        episodeActive =
            false;

        IsGameOver =
            true;


        if (
            chasers != null
        )
        {
            foreach (
                chasertk_vison chaser
                in chasers
            )
            {
                if (
                    chaser != null
                )
                {
                    chaser
                        .OnEnvironmentEpisodeEnding(
                            reason,
                            episodeSteps
                        );
                }
            }
        }


        isEndingEpisode =
            false;


        if (
            chasers != null
        )
        {
            foreach (
                chasertk_vison chaser
                in chasers
            )
            {
                if (
                    chaser != null
                )
                {
                    chaser.EndEpisode();
                }
            }
        }
    }


    private void RandomizeObstacleLayout()
    {
        activeObstacles.Clear();


        if (
            obstacles == null ||
            obstacles.Count == 0
        )
        {
            return;
        }


        List<Transform> validObstacles =
            new List<Transform>();


        foreach (
            Transform obstacle
            in obstacles
        )
        {
            if (
                obstacle != null
            )
            {
                validObstacles.Add(
                    obstacle
                );
            }
        }


        if (
            validObstacles.Count == 0
        )
        {
            return;
        }


        currentObstacleScenario =
            SampleObstacleScenario();


        int requiredDirectedCount =
            0;


        if (
            currentObstacleScenario ==
            ObstacleTrainingScenario.ChaserToBothRunners
        )
        {
            requiredDirectedCount =
                2;
        }
        else if (
            currentObstacleScenario ==
            ObstacleTrainingScenario.ChaserAndRunnerBridge
        )
        {
            requiredDirectedCount =
                3;
        }


        int activeCount =
            validObstacles.Count;


        if (
            randomizeActiveObstacleCount
        )
        {
            int minCount =
                Mathf.Clamp(
                    minActiveObstacles,
                    0,
                    validObstacles.Count
                );


            int maxCount =
                Mathf.Clamp(
                    maxActiveObstacles,
                    minCount,
                    validObstacles.Count
                );


            activeCount =
                Random.Range(
                    minCount,
                    maxCount + 1
                );
        }


        if (
            currentObstacleScenario !=
            ObstacleTrainingScenario.FullyRandom
        )
        {
            activeCount =
                Mathf.Max(
                    activeCount,
                    Mathf.Min(
                        requiredDirectedCount,
                        validObstacles.Count
                    )
                );
        }


        Shuffle(
            validObstacles
        );


        for (
            int i = 0;
            i < validObstacles.Count;
            i++
        )
        {
            validObstacles[i]
                .gameObject
                .SetActive(
                    i < activeCount
                );
        }


        List<Transform> selected =
            new List<Transform>();


        for (
            int i = 0;
            i < activeCount;
            i++
        )
        {
            selected.Add(
                validObstacles[i]
            );
        }


        int placedDirected =
            0;


        if (
            currentObstacleScenario !=
            ObstacleTrainingScenario.FullyRandom
        )
        {
            placedDirected =
                PlaceDirectedScenarioObstacles(
                    selected,
                    currentObstacleScenario
                );
        }


        for (
            int i = placedDirected;
            i < selected.Count;
            i++
        )
        {
            Transform obstacle =
                selected[i];


            if (
                obstacle == null
            )
            {
                continue;
            }


            bool placed =
                TrySpawnObstacle(
                    obstacle
                );


            if (!placed)
            {
                // Last-resort fallback keeps the object active but moves it
                // through the same random spawn routine with more attempts.
                placed =
                    TrySpawnObstacle(
                        obstacle,
                        Mathf.Max(
                            obstacleSpawnAttempts,
                            directedSpawnAttempts
                        )
                    );
            }


            if (placed)
            {
                activeObstacles.Add(
                    obstacle
                );
            }
            else
            {
                obstacle.gameObject.SetActive(
                    false
                );
            }
        }
    }


    private bool TrySpawnObstacle(
        Transform obstacle,
        int attemptOverride = -1)
    {
        if (
            obstacle == null
        )
        {
            return false;
        }


        float originalY =
            obstacle.localPosition.y;


        int attemptCount =
            attemptOverride > 0
                ? attemptOverride
                : obstacleSpawnAttempts;


        for (
            int attempt = 0;
            attempt <
            Mathf.Max(
                1,
                attemptCount
            );
            attempt++
        )
        {
            Vector3 candidate =
                new Vector3(
                    Random.Range(
                        obstacleSpawnMinXZ.x,
                        obstacleSpawnMaxXZ.x
                    ),

                    originalY,

                    Random.Range(
                        obstacleSpawnMinXZ.y,
                        obstacleSpawnMaxXZ.y
                    )
                );


            if (
                !IsObstaclePositionSafe(
                    candidate,
                    minAgentClearance
                )
            )
            {
                continue;
            }


            obstacle.localPosition =
                candidate;


            ApplyObstacleYaw(
                obstacle
            );


            return true;
        }


        return false;
    }


    private bool IsObstaclePositionSafe(
        Vector3 localCandidate,
        float agentClearance)
    {
        Vector3 worldCandidate =
            transform.TransformPoint(
                localCandidate
            );


        foreach (
            Transform placed
            in activeObstacles
        )
        {
            if (
                placed == null
            )
            {
                continue;
            }


            if (
                XZDistance(
                    worldCandidate,
                    placed.position
                )
                <
                minObstacleSpacing
            )
            {
                return false;
            }
        }


        float safeAgentClearance =
            Mathf.Max(
                0f,
                agentClearance
            );


        if (
            chasers != null
        )
        {
            foreach (
                chasertk_vison chaser
                in chasers
            )
            {
                if (
                    chaser == null
                )
                {
                    continue;
                }


                if (
                    XZDistance(
                        worldCandidate,
                        chaser.transform.position
                    )
                    <
                    safeAgentClearance
                )
                {
                    return false;
                }
            }
        }


        if (
            runners != null
        )
        {
            foreach (
                runnertk_vison runner
                in runners
            )
            {
                if (
                    runner == null
                )
                {
                    continue;
                }


                if (
                    XZDistance(
                        worldCandidate,
                        runner.transform.position
                    )
                    <
                    safeAgentClearance
                )
                {
                    return false;
                }
            }
        }


        if (
            obstacleProtectedPoints != null
        )
        {
            foreach (
                Transform point
                in obstacleProtectedPoints
            )
            {
                if (
                    point == null
                )
                {
                    continue;
                }


                if (
                    XZDistance(
                        worldCandidate,
                        point.position
                    )
                    <
                    minProtectedPointClearance
                )
                {
                    return false;
                }
            }
        }


        return true;
    }


    private void DeactivateAllObstacles()
    {
        activeObstacles.Clear();


        if (
            obstacles == null
        )
        {
            return;
        }


        foreach (
            Transform obstacle
            in obstacles
        )
        {
            if (
                obstacle != null
            )
            {
                obstacle.gameObject.SetActive(
                    false
                );
            }
        }
    }


    private ObstacleTrainingScenario SampleObstacleScenario()
    {
        float p1 =
            Mathf.Max(
                0f,
                scenario1Probability
            );


        float p2 =
            Mathf.Max(
                0f,
                scenario2Probability
            );


        float p3 =
            Mathf.Max(
                0f,
                scenario3Probability
            );


        float sum =
            p1
            +
            p2
            +
            p3;


        if (
            sum <= 0.0001f
        )
        {
            return
                ObstacleTrainingScenario.FullyRandom;
        }


        float draw =
            Random.value
            *
            sum;


        if (
            draw < p1
        )
        {
            return
                ObstacleTrainingScenario.ChaserToBothRunners;
        }


        draw -=
            p1;


        if (
            draw < p2
        )
        {
            return
                ObstacleTrainingScenario.ChaserAndRunnerBridge;
        }


        return
            ObstacleTrainingScenario.FullyRandom;
    }


    private int PlaceDirectedScenarioObstacles(
        List<Transform> selected,
        ObstacleTrainingScenario scenario)
    {
        if (
            selected == null ||
            selected.Count == 0
        )
        {
            return 0;
        }


        chasertk_vison primaryChaser =
            GetPrimaryChaser();


        List<runnertk_vison> activeRunners =
            GetActiveRunners();


        if (
            primaryChaser == null ||
            activeRunners.Count == 0
        )
        {
            return 0;
        }


        int placedCount =
            0;


        // Scenario 1:
        // one noisy blocking obstacle on the Chaser -> Runner1 path,
        // and another on the Chaser -> Runner2 path when available.
        int runnerPathCount =
            Mathf.Min(
                2,
                activeRunners.Count
            );


        for (
            int i = 0;
            i < runnerPathCount &&
            placedCount < selected.Count;
            i++
        )
        {
            Transform obstacle =
                selected[placedCount];


            bool placed =
                TrySpawnDirectedObstacle(
                    obstacle,
                    primaryChaser.transform.position,
                    activeRunners[i].transform.position
                );


            if (placed)
            {
                activeObstacles.Add(
                    obstacle
                );


                placedCount++;
            }
            else
            {
                break;
            }
        }


        // Scenario 2 adds a third obstacle between the two Runners.
        if (
            scenario ==
            ObstacleTrainingScenario.ChaserAndRunnerBridge &&
            activeRunners.Count >= 2 &&
            placedCount < selected.Count
        )
        {
            Transform obstacle =
                selected[placedCount];


            bool placed =
                TrySpawnDirectedObstacle(
                    obstacle,
                    activeRunners[0].transform.position,
                    activeRunners[1].transform.position
                );


            if (placed)
            {
                activeObstacles.Add(
                    obstacle
                );


                placedCount++;
            }
        }


        return
            placedCount;
    }


    private bool TrySpawnDirectedObstacle(
        Transform obstacle,
        Vector3 worldStart,
        Vector3 worldEnd)
    {
        if (
            obstacle == null
        )
        {
            return false;
        }


        Vector3 flatDirection =
            worldEnd
            -
            worldStart;


        flatDirection.y =
            0f;


        float pathLength =
            flatDirection.magnitude;


        if (
            pathLength <
            0.5f
        )
        {
            return false;
        }


        flatDirection /=
            pathLength;


        Vector3 lateral =
            new Vector3(
                -flatDirection.z,
                0f,
                flatDirection.x
            );


        float originalLocalY =
            obstacle.localPosition.y;


        float minT =
            Mathf.Clamp01(
                Mathf.Min(
                    directedPathMinT,
                    directedPathMaxT
                )
            );


        float maxT =
            Mathf.Clamp01(
                Mathf.Max(
                    directedPathMinT,
                    directedPathMaxT
                )
            );


        for (
            int attempt = 0;
            attempt <
            Mathf.Max(
                1,
                directedSpawnAttempts
            );
            attempt++
        )
        {
            float t =
                Random.Range(
                    minT,
                    maxT
                );


            float lateralOffset =
                Random.Range(
                    -Mathf.Abs(
                        directedLateralNoise
                    ),
                    Mathf.Abs(
                        directedLateralNoise
                    )
                );


            Vector3 worldCandidate =
                Vector3.Lerp(
                    worldStart,
                    worldEnd,
                    t
                )
                +
                lateral
                *
                lateralOffset;


            Vector3 localCandidate =
                transform.InverseTransformPoint(
                    worldCandidate
                );


            localCandidate.y =
                originalLocalY;


            if (
                localCandidate.x <
                obstacleSpawnMinXZ.x ||
                localCandidate.x >
                obstacleSpawnMaxXZ.x ||
                localCandidate.z <
                obstacleSpawnMinXZ.y ||
                localCandidate.z >
                obstacleSpawnMaxXZ.y
            )
            {
                continue;
            }


            if (
                !IsObstaclePositionSafe(
                    localCandidate,
                    directedAgentClearance
                )
            )
            {
                continue;
            }


            obstacle.localPosition =
                localCandidate;


            ApplyObstacleYaw(
                obstacle
            );


            return true;
        }


        return false;
    }


    private void ApplyObstacleYaw(
        Transform obstacle)
    {
        if (
            obstacle == null ||
            !randomizeObstacleYaw
        )
        {
            return;
        }


        Vector3 euler =
            obstacle.localEulerAngles;


        euler.y =
            Random.Range(
                0f,
                360f
            );


        obstacle.localEulerAngles =
            euler;
    }


    private chasertk_vison GetPrimaryChaser()
    {
        if (
            chasers == null
        )
        {
            return null;
        }


        foreach (
            chasertk_vison chaser
            in chasers
        )
        {
            if (
                chaser != null
            )
            {
                return chaser;
            }
        }


        return null;
    }


    private List<runnertk_vison> GetActiveRunners()
    {
        List<runnertk_vison> result =
            new List<runnertk_vison>();


        if (
            runners == null
        )
        {
            return result;
        }


        foreach (
            runnertk_vison runner
            in runners
        )
        {
            if (
                runner == null ||
                runner.isCaught
            )
            {
                continue;
            }


            result.Add(
                runner
            );
        }


        return result;
    }


    private float XZDistance(
        Vector3 a,
        Vector3 b)
    {
        Vector2 aa =
            new Vector2(
                a.x,
                a.z
            );

        Vector2 bb =
            new Vector2(
                b.x,
                b.z
            );

        return
            Vector2.Distance(
                aa,
                bb
            );
    }


    private void Shuffle<T>(
        List<T> list)
    {
        if (
            list == null
        )
        {
            return;
        }

        for (
            int i =
                list.Count - 1;
            i > 0;
            i--
        )
        {
            int j =
                Random.Range(
                    0,
                    i + 1
                );

            T temp =
                list[i];

            list[i] =
                list[j];

            list[j] =
                temp;
        }
    }
}