using UnityEngine;

public class GameOverUIController : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject gameOverPanel;   // Canvas/GameOverPanel
    public GameObject doorButtonGO;    // Canvas/DoorButton

    Chapter1TwoGameState gs;

    void Awake()
    {
        gs = Chapter1TwoGameState.Instance != null
            ? Chapter1TwoGameState.Instance
            : FindFirstObjectByType<Chapter1TwoGameState>();

        // ✅ 開局一定隱藏
        if (doorButtonGO != null) doorButtonGO.SetActive(false);
    }

    void Update()
    {
        if (gameOverPanel == null || doorButtonGO == null || gs == null) return;

        bool gameOverShowing = gameOverPanel.activeSelf;

        // ✅ 每 3 次(3/6/9...) 唔出 door
        bool isMultipleOf3 = (gs.restartCount > 0) && (gs.restartCount % 3 == 0);

        bool shouldShowDoor = gameOverShowing && !isMultipleOf3;

        if (doorButtonGO.activeSelf != shouldShowDoor)
            doorButtonGO.SetActive(shouldShowDoor);
    }
}