using UnityEngine;

public class PlayerHit : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverPanel;
    public GameObject doorButton;   // ✅ 加：Game Over 時顯示

    [Header("Score")]
    public ScoreManager scoreManager;

    [Header("Stop stuff (optional) - drag the COMPONENTS here")]
    public MonoBehaviour[] scriptsToDisable;

    bool isGameOver = false;

    void Awake()
    {
        // ✅ 起步先收埋 Door（避免一開始見到）
        if (doorButton != null) doorButton.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (doorButton != null)          // ✅ 加：show door
            doorButton.SetActive(true);

        if (scoreManager != null)
            scoreManager.StopScore();

        if (scriptsToDisable != null)
        {
            foreach (var s in scriptsToDisable)
                if (s != null) s.enabled = false;
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
            TriggerGameOver();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
            TriggerGameOver();
    }
}