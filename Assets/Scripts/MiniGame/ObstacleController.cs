using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    [Header("Base Speed")]
    public float baseSpeed = 6f;

    [Header("Destroy Position")]
    public float destroyX = -15f;

    void Update()
    {
        // 讀取 DifficultyManager 的速度倍數
        float multiplier = 1f;

        if (DifficultyManager.Instance != null)
            multiplier = DifficultyManager.Instance.speed;

        // 向左移動（會越來越快）
        transform.position += Vector3.left * baseSpeed * multiplier * Time.deltaTime;

        // 出畫面就刪除
        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}