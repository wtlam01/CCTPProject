using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUIController : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject gameOverPanel;   // Canvas/GameOverPanel
    public GameObject doorButtonGO;    // Canvas/DoorButton (the GO)
    public GameObject startPanel;      // optional

    [Header("Door Script (recommended)")]
    public DoorButtonController1 doorController; // drag DoorButtonController1 here

    [Header("Scenes")]
    public string hubSceneName = "Chapter1two";
    public string miniGameSceneName = "MiniGame";

    [Header("Soft System Lock")]
    public int lockOnRestartCount = 3;     // 3,6,9...
    public bool lockOnlyOneRun = true;     // forced one more run

    bool hasShown = false;

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (doorButtonGO != null) doorButtonGO.SetActive(false);
    }

    // ✅ 由你現有 GameManager / GameOver 事件去 call 呢個
    public void ShowGameOverUI()
    {
        if (hasShown) return;
        hasShown = true;

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        ApplyDoorStateFromGameState();
    }

    void ApplyDoorStateFromGameState()
    {
        bool locked = false;

        if (Chapter1TwoGameState.Instance != null)
            locked = Chapter1TwoGameState.Instance.exitLockedThisGameOver;

        // ✅ 用 doorController 會最穩（佢會負責 SetActive）
        if (doorController != null)
        {
            doorController.SetLocked(locked);
        }
        else if (doorButtonGO != null)
        {
            doorButtonGO.SetActive(!locked);
        }
    }

    // Restart button OnClick -> link to this
    public void RestartGame()
    {
        var gs = Chapter1TwoGameState.Instance;

        if (gs != null)
        {
            // 如果今次 GameOver 係「鎖門狀態」，Restart 就代表「被迫再玩一次」
            // 下一輪 GameOver 要解鎖（lockOnlyOneRun）
            if (lockOnlyOneRun && gs.exitLockedThisGameOver)
            {
                gs.exitLockedThisGameOver = false; // consume the lock
            }
            else
            {
                // 正常累積 restartCount
                gs.restartCount++;

                // ✅ 第 3/6/9... 次觸發：下一個 GameOver 會鎖門（即即刻鎖）
                if (gs.restartCount % lockOnRestartCount == 0)
                {
                    gs.exitLockedThisGameOver = true;

                    // 你想 reset 計數就 reset（你定稿係 reset）
                    gs.restartCount = 0;
                }
            }
        }

        SceneManager.LoadScene(miniGameSceneName);
    }

    // Exit / Door click -> 其實由 DoorButtonController1 負責 LoadScene
    // 但如果你想喺呢度提供一個 Exit 按鈕都得：
    public void ExitToHub()
    {
        var gs = Chapter1TwoGameState.Instance;
        if (gs != null && gs.exitLockedThisGameOver) return;

        SceneManager.LoadScene(hubSceneName);
    }

    public void ResetUI()
    {
        hasShown = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (doorButtonGO != null) doorButtonGO.SetActive(false);
    }
}