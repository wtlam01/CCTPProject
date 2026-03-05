using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiniGameStartController : MonoBehaviour
{
    [Header("Hint UI")]
    public KeyboardHintUI hintUI;      // drag KeyHint (with KeyboardHintUI)
    public GameObject startPanel;      // optional: StartPanel root (contains hint + text)

    [Header("Enable scripts when game starts")]
    public MonoBehaviour[] enableOnStart; // SpawnObstacles, ScoreManager, DifficultyManager, PlayerController etc

    [Header("Show objects when game starts (optional)")]
    public GameObject[] showOnStart;      // ScoreText, BGs, SpawnPoint etc (only if you set them inactive at start)

    bool started = false;

    void Start()
    {
        // freeze gameplay first
        foreach (var mb in enableOnStart)
            if (mb != null) mb.enabled = false;

        foreach (var go in showOnStart)
            if (go != null) go.SetActive(false);

        if (startPanel != null) startPanel.SetActive(true);
        if (hintUI != null) hintUI.StartLoop();
    }

    void Update()
    {
        if (started) return;
        if (Keyboard.current == null) return;

        bool pressed =
            Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.downArrowKey.wasPressedThisFrame;

        if (!pressed) return;

        started = true;
        StartCoroutine(BeginGameRoutine());
    }

    IEnumerator BeginGameRoutine()
    {
        // hide hint + words
        if (hintUI != null)
            yield return hintUI.HideAndDisable();

        if (startPanel != null)
            startPanel.SetActive(false);

        // start gameplay
        foreach (var go in showOnStart)
            if (go != null) go.SetActive(true);

        foreach (var mb in enableOnStart)
            if (mb != null) mb.enabled = true;
    }
}