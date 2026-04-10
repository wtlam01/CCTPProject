// 處理玩家撞到障礙物嘅邏輯，觸發game over畫面、停止score同disable所有gameplay script
// This script handles player collision with obstacles, triggering the game over panel, freezing the score, and disabling gameplay scripts.

using UnityEngine;

public class PlayerHit : MonoBehaviour
// handles what happens when player hits an obstacle, triggers game over
{
    [Header("UI")]
    public GameObject gameOverPanel;
    public GameObject doorButton;
    // game over panel and door button, both hidden until player dies

    [Header("Score")]
    public ScoreManager scoreManager;
    // reference to stop the score when game over

    [Header("Stop stuff (optional) - drag the COMPONENTS here")]
    public MonoBehaviour[] scriptsToDisable;
    // drag any scripts here that should stop when game over, like spawner or player controller

    bool isGameOver = false;
    // prevent TriggerGameOver from running more than once

    void Awake()
    {
        if (doorButton != null) doorButton.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        // 起步收埋兩個，避免開局就見到
    }

    void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        // 只trigger一次，撞多幾次都唔會重複執行

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (doorButton != null)
            doorButton.SetActive(true);
        // show game over panel and door button at the same time

        if (scoreManager != null)
            scoreManager.StopScore();
        // freeze the score

        if (scriptsToDisable != null)
        {
            foreach (var s in scriptsToDisable)
                if (s != null) s.enabled = false;
        }
        // disable all gameplay scripts so obstacles stop spawning and player stops moving

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        // stop player physics so it doesnt keep sliding after death
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle"))
            TriggerGameOver();
        // trigger collider hit
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
            TriggerGameOver();
        // solid collider hit, both covered just in case
    }
}

//Reference: Mini game (Dani, 2020)