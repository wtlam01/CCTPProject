using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public float speed = 2f;          // 背景速度（通常比障礙物慢少少）
    public float resetX = -20f;        // 去到呢個 x 就送返去右邊
    public float moveToX = 20f;        // 送返去右邊嘅 x

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x <= resetX)
        {
            var p = transform.position;
            p.x = moveToX;
            transform.position = p;
        }
    }
}