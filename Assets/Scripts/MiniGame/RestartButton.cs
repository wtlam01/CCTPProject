// script 控制 restart 按鈕邏輯：
// 當玩家按 restart 時：
// 1. 增加 restart 次數（可能會觸發隱藏懲罰：+day / +playCount）
// 2. 檢查當前日數（day）

// 如果已達上限（例如 7 日）：
// 3. 標記玩家從 mini game 返回（通知 hub）
// 4. 載入 hub scene（觸發 exam flow）

// 否則：
// 5. 重新載入當前 mini game scene（正常 restart）

// This script handles the restart button logic.
// When the player presses restart:
// 1. Increment restart count (may trigger hidden penalties: +day / +playCount)
// 2. Check the current day count

// If the day limit is reached (e.g. 7 days):
// 3. Mark that the player is returning from the mini game
// 4. Load the hub scene (to trigger the exam flow)

// Otherwise:
// 5. Reload the current mini game scene (normal restart)


using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtonHook : MonoBehaviour
{
    public bool reloadCurrentScene = true;
    public string hubSceneName = "Chapter1two";
    // hub scene name to return to if day limit reached

    public void Restart()
    {
        if (Chapter1TwoGameState.Instance != null)
        {
            Chapter1TwoGameState.Instance.AddRestart();
            // add restart, might also increment playCount and day if hit 3 retries

            if (Chapter1TwoGameState.Instance.day >= 7)
            {
                Chapter1TwoGameState.Instance.MarkReturnedFromMiniGame();
                SceneManager.LoadScene(hubSceneName);
                // day limit hit after retry penalty, go back to hub so exam triggers
                return;
            }
        }

        if (reloadCurrentScene)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // normal restart, just reload mini game
    }
}


//Reference: Unity Technologies (2023a)