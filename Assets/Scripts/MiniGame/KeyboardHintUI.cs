// script 控制鍵盤上下鍵提示動畫：
// 一開始：
// 1. 顯示提示 UI（up / down）
// 2. 自動開始動畫 loop

// 動畫流程：
// 3. 先模擬按下 up 鍵（縮放）
// 4. 停一小段時間
// 5. 再模擬按下 down 鍵（縮放）
// 6. 重複 loop

// 當玩家真正按鍵時：
// 7. 停止動畫 loop
// 8. 提示 UI 淡出（fade out）
// 9. 隱藏整個提示

// This script controls the keyboard hint animation for up and down keys.
// At start:
// 1. Shows the hint UI (up / down)
// 2. Starts a looping demo animation

// Animation flow:
// 3. Simulates pressing the up key (scale down + up)
// 4. Waits briefly
// 5. Simulates pressing the down key
// 6. Repeats in a loop

// When the player actually presses a key:
// 7. Stops the animation loop
// 8. Fades out the hint UI
// 9. Disables the hint

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
// new input system imported but detection handled by caller, this script just does the animation

public class KeyboardHintUI : MonoBehaviour
// animates the up and down key hint UI, loops a press animation until player actually presses a key
{
    [Header("UI")]
    public RectTransform upRect;
    public RectTransform downRect;
    public CanvasGroup hintGroup;
    // up and down key images, canvasgroup for fading the whole hint out

    [Header("Auto Demo Loop (like SpaceHint)")]
    public float pressDownScale = 0.88f;
    public float pressDownTime  = 0.10f;
    public float releaseTime    = 0.14f;
    public float pressPause     = 0.55f;
    public float loopDelay      = 0.25f;
    // controls how the press animation looks and feels, quick press down then release

    [Header("Fade out when finished")]
    public float fadeOutTime = 0.2f;
    // how fast the hint fades out when player finally presses a key

    Coroutine loopCo;
    bool stopped = false;
    // stopped flag so the loop knows when to exit cleanly

    void Awake()
    {
        if (upRect != null) upRect.localScale = Vector3.one;
        if (downRect != null) downRect.localScale = Vector3.one;
        // reset key scales to normal

        if (hintGroup != null)
        {
            hintGroup.alpha = 1f;
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;
        }
        // hint starts visible but doesnt block any clicks
    }

    void OnEnable()
    {
        StartLoop();
        // auto start animation when object enabled
    }

    void OnDisable()
    {
        StopLoop();
        // clean up when disabled
    }

    public void StartLoop()
    {
        stopped = false;
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(DemoLoop());
        // restart loop fresh
    }

    public void StopLoop()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = null;

        if (upRect != null) upRect.localScale = Vector3.one;
        if (downRect != null) downRect.localScale = Vector3.one;
        // stop and reset both key scales
    }

    IEnumerator DemoLoop()
    {
        while (!stopped)
        {
            if (upRect != null) yield return PressAnim(upRect);
            yield return new WaitForSecondsRealtime(loopDelay);

            if (downRect != null) yield return PressAnim(downRect);
            yield return new WaitForSecondsRealtime(loopDelay);
        }
        // alternates between animating up key and down key with a small gap between
    }

    IEnumerator PressAnim(RectTransform rect)
    {
        Vector3 baseScale = Vector3.one;
        Vector3 downScale = baseScale * pressDownScale;

        float t = 0f;
        while (t < pressDownTime)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(baseScale, downScale, t / pressDownTime);
            yield return null;
        }
        rect.localScale = downScale;
        // press down phase, scale shrinks slightly

        t = 0f;
        while (t < releaseTime)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(downScale, baseScale, t / releaseTime);
            yield return null;
        }
        rect.localScale = baseScale;
        // release phase, scale bounces back to normal

        yield return new WaitForSecondsRealtime(pressPause);
        // pause before next key animates
    }

    public IEnumerator HideAndDisable()
    {
        stopped = true;
        StopLoop();
        // stop the loop first

        if (hintGroup == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float start = hintGroup.alpha;
        float t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            hintGroup.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / fadeOutTime));
            yield return null;
        }

        hintGroup.alpha = 0f;
        gameObject.SetActive(false);
        // fade out then hide, called externally when player actually presses up or down
    }
}