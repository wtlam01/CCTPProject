// script 控制分數系統：
// 一開始：
// 1. 將分數設為 0
// 2. 更新 UI 顯示

// 每一幀（Update）：
// 3. 累積時間（accumulator）
// 4. 當累積達到 1 或以上：
//    - 增加整數分數（Score）
//    - 扣除已使用嘅累積值
//    - 更新 UI 顯示

// 當玩家死亡時：
// 5. 停止計分（running = false）

// This script tracks and displays the player score over time.
// At start:
// 1. Reset the score to 0
// 2. Update the UI display

// Every frame (Update):
// 3. Accumulate time using an accumulator
// 4. When the accumulated value reaches 1 or more:
//    - Increase the score in whole numbers
//    - Subtract the used accumulated value
//    - Update the UI display

// When the player dies:
// 5. Stop the score system (running = false)


using UnityEngine;
using TMPro;
// TMPro for the score text display

public class ScoreManager : MonoBehaviour
// tracks and displays score over time, stops when player dies
{
    public TextMeshProUGUI scoreText;
    public float pointsPerSecond = 1f;
    // how fast score goes up, 1 point per second by default

    public int Score { get; private set; }
    bool running = true;
    float acc = 0f;
    // acc accumulates fractional points each frame, only adds to score when it hits a whole number

    void Start()
    {
        Score = 0;
        UpdateUI();
        // reset score and update display at start
    }

    void Update()
    {
        if (!running) return;

        acc += Time.deltaTime * pointsPerSecond;
        if (acc >= 1f)
        {
            int add = Mathf.FloorToInt(acc);
            acc -= add;
            Score += add;
            UpdateUI();
        }
        // using accumulator so score increments cleanly as whole numbers, not floats
    }

    public void StopScore()
    {
        running = false;
        // called by PlayerHit when game over, freezes the score
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Score;
        // update the text display whenever score changes
    }
}

//Reference: Mini game (Dani, 2020)