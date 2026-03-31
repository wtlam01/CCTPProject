using UnityEngine;

public class ObstacleController : MonoBehaviour
// controls each obstacle, moves left and destroys itself when off screen
{
    [Header("Base Speed")]
    public float baseSpeed = 6f;
    // 基礎速度，會乘以 DifficultyManager 嘅倍數

    [Header("Destroy Position")]
    public float destroyX = -15f;
    // 去到呢個 x 就刪除，唔使保留出咗畫面嘅 object

    void Update()
    {
        float multiplier = 1f;

        if (DifficultyManager.Instance != null)
            multiplier = DifficultyManager.Instance.speed;
        // grab speed multiplier from difficulty manager, gets faster over time

        transform.position += Vector3.left * baseSpeed * multiplier * Time.deltaTime;
        // move left, speed increases as game goes on

        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
        // 出咗畫面就destroy，keep scene clean
    }
}