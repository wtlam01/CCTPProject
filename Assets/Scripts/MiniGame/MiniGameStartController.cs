using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
// new input system for detecting keyboard press

public class MiniGameStartController : MonoBehaviour
// waits for player to press up or down arrow before starting the game, freezes everything until then
{
    [Header("Hint UI")]
    public KeyboardHintUI hintUI;
    public GameObject startPanel;
    // the hint animation and the whole start panel that shows before game begins

    [Header("Enable scripts when game starts")]
    public MonoBehaviour[] enableOnStart;
    // all the gameplay scripts that should be off until player presses a key

    [Header("Show objects when game starts (optional)")]
    public GameObject[] showOnStart;
    // objects to show when game starts, like score text and backgrounds

    bool started = false;
    // prevent input being detected more than once

    void Start()
    {
        foreach (var mb in enableOnStart)
            if (mb != null) mb.enabled = false;
        // disable all gameplay scripts at start so nothing moves yet

        foreach (var go in showOnStart)
            if (go != null) go.SetActive(false);
        // hide gameplay objects until game begins

        if (startPanel != null) startPanel.SetActive(true);
        if (hintUI != null) hintUI.StartLoop();
        // show start panel and begin the key hint animation
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
        // once up or down is pressed, start the game routine
    }

    IEnumerator BeginGameRoutine()
    {
        if (hintUI != null)
            yield return hintUI.HideAndDisable();
        // fade out the hint first before anything else shows up

        if (startPanel != null)
            startPanel.SetActive(false);
        // hide the whole start panel

        foreach (var go in showOnStart)
            if (go != null) go.SetActive(true);
        // show the gameplay objects

        foreach (var mb in enableOnStart)
            if (mb != null) mb.enabled = true;
        // enable all the gameplay scripts, game officially starts here
    }
}