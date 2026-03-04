using UnityEngine;

public class SpawnObstacles : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject obstaclePrefab;

    [Header("Sprites (4 types)")]
    public Sprite[] obstacleSprites;

    [Header("Spawn Y Range")]
    public float minY = -4.5f;
    public float maxY = 4.5f;

    [Header("Spawn Timing")]
    public float timeBetweenSpawns = 0.8f;
    public float minInterval = 0.25f;

    [Header("Anti-overlap")]
    public float minDistanceY = 1.2f;   // 同一條 X 上，Y 最少距離
    public int maxTries = 10;           // 最多試幾次搵位
    public LayerMask obstacleLayer;     // 只檢查 obstacle layer（建議用）

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeBetweenSpawns)
        {
            timer = 0f;
            SpawnOne();

            // 例子：慢慢加快（你可以另外調）
            timeBetweenSpawns = Mathf.Max(minInterval, timeBetweenSpawns * 0.995f);
        }
    }

    void SpawnOne()
    {
        float y = 0f;
        bool found = false;

        for (int i = 0; i < maxTries; i++)
        {
            y = Random.Range(minY, maxY);

            // 用 OverlapCircle 檢查附近有冇 obstacle（同一 X 附近）
            Vector2 checkPos = new Vector2(transform.position.x, y);
            float radius = minDistanceY * 0.5f;

            Collider2D hit = Physics2D.OverlapCircle(checkPos, radius, obstacleLayer);
            if (hit == null)
            {
                found = true;
                break;
            }
        }

        if (!found) return; // 搵唔到位就今次唔spawn

        GameObject obj = Instantiate(obstaclePrefab, new Vector3(transform.position.x, y, 0f), Quaternion.identity);

        // 換 sprite（你已有 4 張）
        if (obstacleSprites != null && obstacleSprites.Length > 0)
        {
            var sr = obj.GetComponentInChildren<SpriteRenderer>() ?? obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = obstacleSprites[Random.Range(0, obstacleSprites.Length)];
            }
        }

        // 如果你用 PolygonCollider + 需要 refresh collider（你已有 setup script 就唔理）
        // obj.SendMessage("RefreshCollider", SendMessageOptions.DontRequireReceiver);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}