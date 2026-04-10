// 控制背景無限scroll效果，背景向左移動，去到指定位置就即刻teleport返右邊，make loop感覺
// This script creates an infinite scrolling background by moving left and snapping back to the right when it goes too far

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