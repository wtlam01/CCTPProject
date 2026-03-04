using UnityEngine;

public class PlayerHit : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverPanel;

    [Header("Score")]
    public ScoreManager scoreManager;   // ← 加呢行

    [Header("Stop stuff (optional) - drag the COMPONENTS here")]
    public MonoBehaviour[] scriptsToDisable;

    bool isGameOver = false;

    void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (scoreManager != null)        // ← 加呢行
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