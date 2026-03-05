using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MiniGameChoiceController : MonoBehaviour
{
    [Header("Buttons (optional)")]
    public Button playButton;
    public Button studyButton;

    [Header("Scene Names")]
    public string hubSceneName = "Chapter1two";
    public string miniGameSceneName = "MiniGame";

    void Awake()
    {
        // Optional: auto-wire buttons if you dragged them in Inspector
        if (playButton != null)
            playButton.onClick.AddListener(PlayMiniGame);

        if (studyButton != null)
            studyButton.onClick.AddListener(ChooseStudy);
    }

    // =========================
    // HUB -> MINI GAME
    // =========================
    public void PlayMiniGame()
    {
        var gs = Chapter1TwoGameState.Instance;

        if (gs != null)
        {
            // ✅ Start counting at 1 when entering the mini-game
            gs.restartCount = 1;
        }

        SceneManager.LoadScene(miniGameSceneName);
    }

    // =========================
    // Optional: HUB study option
    // =========================
    public void ChooseStudy()
    {
        // If you have study flow, put it here.
        // For now, do nothing / or load another scene if needed.
        Debug.Log("[MiniGameChoiceController] Study chosen (no action set).");
    }

    // =========================
    // MINI GAME -> HUB (Exit)
    // You can call this from DoorButton too if you want.
    // =========================
    public void ExitToHub()
    {
        var gs = Chapter1TwoGameState.Instance;

        if (gs != null)
        {
            // ✅ Reset so next time player presses Play,
            // it starts again at 1 (and 3/6/9 rule repeats)
            gs.ResetMiniGameRestartCount();
        }

        SceneManager.LoadScene(hubSceneName);
    }
}