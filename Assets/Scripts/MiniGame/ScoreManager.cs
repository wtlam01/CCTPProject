// 追蹤同顯示玩家嘅分數，隨時間增加，玩家死亡後停止計分
// This script tracks and displays the player score over time using an accumulator for clean whole number increments, stopping when the player dies.

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