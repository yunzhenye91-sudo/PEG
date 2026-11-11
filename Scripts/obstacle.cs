using UnityEngine;

public class obstacle : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public bool enableHorizontalMove = true;

    // true = X轴移动，false = Z轴移动
    public bool moveOnX = true;

    // 这是绝对范围，不是相对范围
    public float minMoveValue = -2.0f;
    public float maxMoveValue = 2.0f;

    // 速度必须填正数
    public float moveSpeed = 1.0f;

    // 初始方向：1 正方向，-1 反方向
    public int startDirection = 1;

    private Vector3 startLocalPos;
    private int moveDirection;
    private bool isMoving = false;

    private void Awake()
    {
        // 在其他脚本 Start() 之前记录场景初始位置
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (!enableHorizontalMove || !isMoving)
        {
            return;
        }

        Vector3 pos = transform.localPosition;

        float delta = moveDirection * Mathf.Abs(moveSpeed) * Time.deltaTime;

        if (moveOnX)
        {
            pos.x += delta;

            if (pos.x >= maxMoveValue)
            {
                pos.x = maxMoveValue;
                moveDirection = -1;
            }
            else if (pos.x <= minMoveValue)
            {
                pos.x = minMoveValue;
                moveDirection = 1;
            }
        }
        else
        {
            pos.z += delta;

            if (pos.z >= maxMoveValue)
            {
                pos.z = maxMoveValue;
                moveDirection = -1;
            }
            else if (pos.z <= minMoveValue)
            {
                pos.z = minMoveValue;
                moveDirection = 1;
            }
        }

        transform.localPosition = pos;
    }

    public void ResetObstacle()
    {
        // 每次 episode 重置回场景最初位置
        transform.localPosition = startLocalPos;

        // 重置移动方向
        moveDirection = (startDirection >= 0) ? 1 : -1;

        // 开始移动
        isMoving = true;

        // 防止初始位置超出范围
        Vector3 pos = transform.localPosition;
        if (moveOnX)
        {
            pos.x = Mathf.Clamp(pos.x, minMoveValue, maxMoveValue);
        }
        else
        {
            pos.z = Mathf.Clamp(pos.z, minMoveValue, maxMoveValue);
        }

        transform.localPosition = pos;
    }
}
