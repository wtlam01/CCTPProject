// script 控制障礙物移動：
// 一開始：
// 1. 設定基礎速度（baseSpeed）

// 遊戲進行中：
// 2. 每一幀向左移動
// 3. 速度會乘以 DifficultyManager 嘅 speed（隨時間加快）

// 當離開畫面時：
// 4. 當 x 座標 < destroyX
// 5. 自動刪除 object（Destroy）

// This script controls obstacle movement.
// At start:
// 1. Uses a base movement speed (baseSpeed)

// During gameplay:
// 2. Moves left every frame
// 3. Speed is multiplied by DifficultyManager.speed (increases over time)

// When off screen:
// 4. If x position < destroyX
// 5. Destroys the object automatically

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

//Reference: Mini game (Dani, 2020)