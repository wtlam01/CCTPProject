// script 控制 game over 畫面嘅出口門顯示邏輯：
// 一開始：
// 1. 隱藏出口門按鈕（doorButton）

// 當 game over 畫面出現時：
// 2. 檢查玩家嘅 restart 次數（restartCount）
// 3. 如果係第 3、6、9… 次（每 3 次）：
//    隱藏出口門（迫玩家繼續 retry）
// 4. 否則：
//    顯示出口門按鈕

// This script controls the visibility of the exit door on the game over screen.
// At start:
// 1. The door button is hidden by default

// When the game over panel is active:
// 2. Checks the player's restart count (restartCount)
// 3. If it is every 3rd death (3, 6, 9...):
//    - Hides the exit door to force more retries
// 4. Otherwise:
//    - Shows the exit door button

using UnityEngine;

public class GameOverUIController : MonoBehaviour
// controls whether the door button shows on the game over screen, hides it every 3rd death
{
    [Header("UI Objects")]
    public GameObject gameOverPanel;
    public GameObject doorButtonGO;
    // game over panel and the door button that appears after dying

    Chapter1TwoGameState gs;
    // reference to game state to check how many times player has restarted

    void Awake()
    {
        gs = Chapter1TwoGameState.Instance != null
            ? Chapter1TwoGameState.Instance
            : FindFirstObjectByType<Chapter1TwoGameState>();
        // try singleton first, fallback to findFirstObject if instance not set

        if (doorButtonGO != null) doorButtonGO.SetActive(false);
        // always hide door button at start
    }

    void Update()
    {
        if (gameOverPanel == null || doorButtonGO == null || gs == null) return;

        bool gameOverShowing = gameOverPanel.activeSelf;

        bool isMultipleOf3 = (gs.restartCount > 0) && (gs.restartCount % 3 == 0);
        // every 3rd death (3, 6, 9...) hide the door, forces player to retry more

        bool shouldShowDoor = gameOverShowing && !isMultipleOf3;

        if (doorButtonGO.activeSelf != shouldShowDoor)
            doorButtonGO.SetActive(shouldShowDoor);
        // only update if state actually changed, avoid unnecessary SetActive calls every frame
    }
}

// Reference: Code Monkey (2021)