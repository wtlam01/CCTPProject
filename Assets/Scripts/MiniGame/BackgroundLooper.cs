// script 控制背景無限 scroll 效果：
// 一開始：
// 1. 背景持續向左移動（speed）
// 2. 當移到指定位置（resetX）
// 3. 即刻跳返右邊（moveToX）
// 4. 重複以上流程形成無限 loop

// This script creates an infinite scrolling background effect.
// At runtime:
// 1. The background continuously moves left (speed)
// 2. When it reaches a set position (resetX)
// 3. It instantly teleports back to the right (moveToX)
// 4. The process repeats to create a seamless loop

using UnityEngine;

public class BackgroundLooper : MonoBehaviour
// makes the background scroll left and loop back to the right, creates infinite scrolling effect
{
    public float speed = 2f;
    // scroll speed, usually slower than obstacles to give depth feel

    public float resetX = -20f;
    public float moveToX = 20f;
    // when background reaches resetX on the left, teleport it back to moveToX on the right

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        // move left every frame

        if (transform.position.x <= resetX)
        {
            var p = transform.position;
            p.x = moveToX;
            transform.position = p;
        }
        // once it goes too far left, snap it back to the right so it loops seamlessly
    }
}

//Reference: Mini game (Dani, 2020)