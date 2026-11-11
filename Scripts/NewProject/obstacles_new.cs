using UnityEngine;

public class obstacles_new : MonoBehaviour
{
    [Header("Safety")]
    public LayerMask obstacleLayer;
    public float checkRadius = 0.1f;

    [Header("Spawn Range")]
    public Vector2 xRange = new Vector2(-6.5f, 7.91f);
    public Vector2 yRange = new Vector2(0.35f, 4.0f);
    public Vector2 zRange = new Vector2(-0.4f, 5.34f);

    [Header("Horizontal Random Move")]
    public bool enableMove = true;
    public float moveRadius = 0.6f;
    public float moveSpeed = 0.5f;
    public float changeTargetTime = 2f;

    private Vector3 originPosition;   // 本障碍物出生点（各自独立）
    private Vector3 targetPosition;   // 当前目标点
    private float timer;
    private bool hasSpawned;

    private bool IsPositionSafe(Vector3 pos)
    {
        return Physics.OverlapSphere(pos, checkRadius, obstacleLayer).Length == 0;
    }

    public void spawn()
    {
        int maxAttempts = 1000;
        int attempts = 0;
        Vector3 pos;

        do
        {
            pos = new Vector3(
                Random.Range(xRange.x, xRange.y),
                Random.Range(yRange.x, yRange.y),
                Random.Range(zRange.x, zRange.y)
            );
            attempts++;
        }
        while (!IsPositionSafe(pos) && attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning($"{name}: Failed to find a safe spawn position.");
            return;
        }

        transform.position = pos;

        originPosition = pos;     // 记住出生点
        hasSpawned = true;
        PickNewTarget();
        timer = 0f;
    }

    private void Update()
    {
        if (!enableMove || !hasSpawned) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        timer += Time.deltaTime;
        if (timer >= changeTargetTime || Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            PickNewTarget();
            timer = 0f;
        }
    }

    private void PickNewTarget()
    {
        int maxAttempts = 200;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * moveRadius;
            Vector3 candidate = new Vector3(
                originPosition.x + offset2D.x,
                originPosition.y,              // ★ 只水平移动：Y 不变
                originPosition.z + offset2D.y
            );

            if (IsPositionSafe(candidate))
            {
                targetPosition = candidate;
                return;
            }
        }

        // 实在找不到安全点就回出生点
        targetPosition = originPosition;
    }
}
