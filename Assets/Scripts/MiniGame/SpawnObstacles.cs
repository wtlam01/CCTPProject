// script 控制障礙物生成系統：
// 每一幀（Update）：
// 1. 累計時間（timer）
// 2. 當時間達到生成間距（timeBetweenSpawns）：
//    - 重置 timer
//    - 呼叫 SpawnOne() 生成障礙物
//    - 將生成間距逐步縮短（增加難度，但不低於 minInterval）

// 生成障礙物（SpawnOne）：
// 3. 隨機揀一個 Y 位置（minY ～ maxY）
// 4. 檢查附近有冇其他障礙物（避免 overlap）
// 5. 最多嘗試 maxTries 次搵合適位置
// 6. 如果搵到：
//    - instantiate 障礙物
//    - 隨機套用一個 sprite


// This script handles obstacle spawning.
// Every frame (Update):
// 1. Accumulate time using a timer
// 2. When the timer reaches the spawn interval (timeBetweenSpawns):
//    - Reset the timer
//    - Call SpawnOne() to create an obstacle
//    - Gradually reduce the spawn interval (increase difficulty, clamped by minInterval)

// Spawning logic (SpawnOne):
// 3. Randomly choose a Y position (between minY and maxY)
// 4. Check for nearby obstacles to avoid overlap
// 5. Try up to maxTries to find a valid position
// 6. If a valid position is found:
//    - Instantiate the obstacle
//    - Assign a random sprite



using UnityEngine;

public class SpawnObstacles : MonoBehaviour
// spawns obstacles at random y positions, speeds up over time, checks for overlaps before spawning
{
    [Header("Prefab")]
    public GameObject obstaclePrefab;
    // the obstacle prefab to instantiate

    [Header("Sprites (4 types)")]
    public Sprite[] obstacleSprites;
    // randomly picks one of these sprites for each spawned obstacle

    [Header("Spawn Y Range")]
    public float minY = -4.5f;
    public float maxY = 4.5f;
    // obstacles spawn anywhere within this vertical range

    [Header("Spawn Timing")]
    public float timeBetweenSpawns = 0.8f;
    public float minInterval = 0.25f;
    // spawn interval shrinks over time but never goes below minInterval

    [Header("Anti-overlap")]
    public float minDistanceY = 1.2f;
    public int maxTries = 10;
    public LayerMask obstacleLayer;
    // before spawning, checks nearby area to avoid obstacles overlapping each other

    float timer;
    // counts up until next spawn

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeBetweenSpawns)
        {
            timer = 0f;
            SpawnOne();

            timeBetweenSpawns = Mathf.Max(minInterval, timeBetweenSpawns * 0.995f);
            // 每次spawn之後間距縮短少少，慢慢加密obstacles
        }
    }

    void SpawnOne()
    {
        float y = 0f;
        bool found = false;

        for (int i = 0; i < maxTries; i++)
        {
            y = Random.Range(minY, maxY);

            Vector2 checkPos = new Vector2(transform.position.x, y);
            float radius = minDistanceY * 0.5f;

            Collider2D hit = Physics2D.OverlapCircle(checkPos, radius, obstacleLayer);
            if (hit == null)
            {
                found = true;
                break;
            }
        }
        // try up to maxTries random positions, pick first one with no nearby obstacle

        if (!found) return;
        // 搵唔到合適位置就skip今次，唔好硬spawn

        GameObject obj = Instantiate(obstaclePrefab, new Vector3(transform.position.x, y, 0f), Quaternion.identity);
        // spawn obstacle at chosen position

        if (obstacleSprites != null && obstacleSprites.Length > 0)
        {
            var sr = obj.GetComponentInChildren<SpriteRenderer>() ?? obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = obstacleSprites[Random.Range(0, obstacleSprites.Length)];
            }
        }
        // randomly assign one of the 4 sprites, check children first then self
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        // shows spawn point in scene view when selected, useful for positioning
    }
}

//Reference: Mini game (Dani, 2020)