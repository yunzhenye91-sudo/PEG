using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class target_PID_5v5 : MonoBehaviour
{
    private Vector3 underdesk;
    private Vector3 behindcouch;
    private Vector3 belowlamp;
    private Vector3 belowcupboard;
    private Vector3 everywhereelse;
    private Vector3 centerpoint;
    private Vector3 spawnposition;
    private Vector3 abovecupboard;
    private int number;
    private float random;
    // 定义障碍物检测相关变量
    public LayerMask obstacleLayer; // 障碍物所在的图层
    public float spawnRadius = 0.1f; // 初始位置生成的安全半径


    private void Start()
    {
        // underdesk = new Vector3(1.421f, -0.5f, 2.808f);
        // behindcouch = new Vector3(-1.04f, 0f, 2.926f);
        // belowcupboard = new Vector3(2.279f, 0.091f, 3.059f);
        // belowlamp = new Vector3(2.822f, 0.5f, 2.85f);
        // centerpoint = new Vector3(1.00f, 0.5f, 3.5f); //single drone last point - (1.01f, 0.563f, 1.2f) // test
        // // centerpoint = new Vector3(1.00f, 0.5f, 2.5f); // train
        // abovecupboard  = new Vector3(2.279f, 0.8f, 3.059f);

        //以上是原版，我改动了环境，所以要修改一下，名字并不意味着位置，只是代号
        underdesk = new Vector3(1.5f, -0.5f, 3f);
        behindcouch = new Vector3(-2f, 1f, 3.2f);
        belowcupboard = new Vector3(5.3f, 1.3f, 2.5f);
        belowlamp = new Vector3(-2f, 2f, -1f);
        //centerpoint = new Vector3(1.00f, 0.5f, 3.5f); //single drone last point - (1.01f, 0.563f, 1.2f) // test
        // centerpoint = new Vector3(1.00f, 0.5f, 2.5f); // train
        abovecupboard = new Vector3(4f, 1f, -2f);

    }

    private bool IsPositionSafe(Vector3 position)
    {
        // 检查给定位置是否与障碍物重叠
        return Physics.OverlapSphere(position, spawnRadius, obstacleLayer).Length == 0;
    }
    public void spawn()
    {
        random = Random.Range(0f, 1f);


        if (random < 1.0f)
        {
            int maxAttempts = 1000; // 最大尝试次数
            int attempts = 0;
            Vector3 spawnposition;

            do
            {
                // 随机生成位置
                spawnposition = new Vector3(
                    Random.Range(-5.0f, 7.0f), // X 轴范围
                    Random.Range(1.2f, 4.5f), // Y 轴范围
                    Random.Range(-6.5f, -5.09f)  // Z 轴范围
                );

                attempts++;
            }
            while (!IsPositionSafe(spawnposition) && attempts < maxAttempts);

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Failed to find a safe position for target spawning.");
            }
            this.transform.localPosition = spawnposition;
        }


    }


}