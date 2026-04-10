// 負責生成障礙物，隨機揀Y位置並check有冇overlap，生成間距會隨時間慢慢縮短令難度增加
// This script spawns obstacles at random vertical positions with overlap checking, gradually reducing spawn intervals over time to increase difficulty.

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