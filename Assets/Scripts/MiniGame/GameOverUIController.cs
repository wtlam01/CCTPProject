using UnityEngine;

public class GameOverUIController : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject gameOverPanel;   // Canvas/GameOverPanel
    public GameObject doorButton;      // Canvas/DoorButton
    public GameObject startPanel;      // Canvas/StartPanel (optional)

    bool hasShown = false;

    void Awake()
    {
        // 起步先收埋 GameOver UI
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (doorButton != null) doorButton.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        if (hasShown) return;
        hasShown = true;

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (doorButton != null) doorButton.SetActive(true);
    }

    public void ResetUI()
    {
        hasShown = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (doorButton != null) doorButton.SetActive(false);
    }
}