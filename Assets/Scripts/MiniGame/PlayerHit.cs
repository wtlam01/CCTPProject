// script 控制玩家撞到障礙物後嘅 game over flow：
// 一開始：
// 1. 隱藏 game over panel 同 door button

// 當玩家撞到障礙物時：
// 2. 觸發 game over（只會觸發一次）
// 3. 顯示 game over panel 同 door button
// 4. 停止分數計算（score freeze）
// 5. disable 所有 gameplay scripts（例如 player、spawner）
// 6. 停止玩家 Rigidbody2D 嘅移動（速度設為 0）

// This script handles the game over flow when the player hits an obstacle.
// At start:
// 1. Hide the game over panel and door button

// When the player collides with an obstacle:
// 2. Trigger game over (only once)
// 3. Show the game over panel and door button
// 4. Stop the score system
// 5. Disable all gameplay scripts (e.g. player, spawner)
// 6. Stop the player's Rigidbody2D movement (set velocity to 0)
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