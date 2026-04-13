// script 控制 mini game 開始流程：
// 一開始：
// 1. disable 所有 gameplay scripts
// 2. 隱藏 gameplay 物件（showOnStart）
// 3. 顯示 start panel 同鍵盤提示動畫

// 等玩家按鍵：
// 4. 偵測 up 或 down 鍵輸入
// 5. 玩家按鍵後開始遊戲流程

// 開始遊戲：
// 6. 提示 UI fade out 並隱藏
// 7. 隱藏 start panel
// 8. 顯示 gameplay 物件
// 9. enable 所有 gameplay scripts

// This script controls the mini game start sequence.
// At start:
// 1. Disables all gameplay scripts
// 2. Hides gameplay objects (showOnStart)
// 3. Shows the start panel and keyboard hint animation

// Waiting for input:
// 4. Detects up or down arrow key press
// 5. Once pressed, begins the game start routine

// When the game starts:
// 6. Fades out and hides the hint UI
// 7. Hides the start panel
// 8. Shows gameplay objects
// 9. Enables all gameplay scripts

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