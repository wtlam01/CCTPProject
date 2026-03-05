using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MiniGameChoiceController : MonoBehaviour
{
    [Header("Scenes")]
    public string hubSceneName = "Chapter1two";       // 回去 hub
    public string miniGameSceneName = "MiniGame";     // reload 用

    [Header("UI")]
    public Button exitDoorButton;  // 你個 Exit Door Button（拖入 Inspector）

    [Header("Soft System Lock")]
    public int lockOnRestartCount = 3;        // 第幾次 restart 觸發 lock（=3）
    public bool lockOnlyOneRun = true;        // forced one more run

    bool exitLockedThisRun = false;

    void Start()
    {
        ApplyExitLockUI();
    }

    // =========================
    // Restart button
    // =========================
    public void RestartMiniGame()
    {
        if (Chapter1TwoGameState.Instance != null)
        {
            Chapter1TwoGameState.Instance.restartCount += 1;

            // 第 3 次 restart -> lock exit（forced one more run）
            if (Chapter1TwoGameState.Instance.restartCount >= lockOnRestartCount)
            {
                exitLockedThisRun = true;
                ApplyExitLockUI();

                // reset counter immediately (per your spec)
                Chapter1TwoGameState.Instance.ResetMiniGameRestartCount();
            }
        }

        // 重新開始 minigame（唔加 day）
        SceneManager.LoadScene(miniGameSceneName);
    }

    // =========================
    // Exit button
    // =========================
    public void ExitToHub()
    {
        // 如果今次 run 被 lock，就唔俾走（soft lock）
        if (exitLockedThisRun) return;

        // 回 hub（Hub 會負責 EndChoiceAndMaybeExam）
        SceneManager.LoadScene(hubSceneName);
    }

    // =========================
    // Lock UI helper
    // =========================
    void ApplyExitLockUI()
    {
        if (exitDoorButton == null) return;

        // lock: disable/interactable=false + optional hide
        if (exitLockedThisRun)
        {
            exitDoorButton.interactable = false;
            exitDoorButton.gameObject.SetActive(false); // 如你想「隱藏」
        }
        else
        {
            exitDoorButton.gameObject.SetActive(true);
            exitDoorButton.interactable = true;
        }
    }

    // ✅ 當新一局開始，如果上一局 lock 過，呢局要恢復 Exit
    // 因為 lock 只係 forced one more time
    void OnEnable()
    {
        // 新 load scene -> exitLockedThisRun 預設 false
        // 但如果你想「lock 只持續一局」：就保持 false（即恢復）
        // 你 spec：lock 觸發後「之後回復」— 係下一局就回復 ✅
        exitLockedThisRun = false;
    }
}