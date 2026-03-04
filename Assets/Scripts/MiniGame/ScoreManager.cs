using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public float pointsPerSecond = 1f;

    public int Score { get; private set; }
    bool running = true;
    float acc = 0f;

    void Start()
    {
        Score = 0;
        UpdateUI();
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
    }

    public void StopScore()
    {
        running = false;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Score;
    }
}