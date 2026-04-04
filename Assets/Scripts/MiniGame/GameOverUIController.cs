//控制game over畫面入面嘅出口門按鈕顯示邏輯，每第3次die就cant exit，迫玩家多retry幾次
// This script controls whether the exit door appears on the game over screen, hiding it every 3rd death to encourage more retries.

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