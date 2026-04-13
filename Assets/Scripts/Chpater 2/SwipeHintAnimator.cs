// script 控制 swipe 提示嘅手指動畫：
// 手指會向上移動，同時逐漸淡出，並持續 loop 直到被隱藏

// 主要 flow：
// 1. 手指由起始位置開始（startAnchoredPos）
// 2. 向上移動（moveDistance）
// 3. 喺接近尾段開始 fade out（fadeDuration）
// 4. 完全消失後停一段時間（delayBetween）
// 5. 重置位置再重複 loop

// 控制方式：
// OnEnable：自動開始動畫
// OnDisable：停止並重置位置 + alpha
// ShowAndPlay()：外部呼叫顯示並播放
// StopAndHide()：外部呼叫停止並隱藏

// 動畫原理：
// 使用 anchoredPosition 控制 UI 移動
// 使用 CanvasGroup alpha 控制淡入淡出
// 使用 coroutine + while loop 實現持續動畫

// - 用於提示玩家做「向上 swipe」操作
// - 常見於 video 停頓點、互動引導場景


// This script controls a swipe hint finger animation,
// where the finger moves upward and fades out in a continuous loop until hidden.

// Main flow:
// 1. The finger starts from its base position (startAnchoredPos)
// 2. Moves upward by a set distance (moveDistance)
// 3. Begins fading out near the end of the movement (fadeDuration)
// 4. Fully disappears, then waits for a short delay (delayBetween)
// 5. Resets and loops again

// Control:
// OnEnable: automatically starts the animation
// ODisable: stops the animation and resets position + alpha
// ShowAndPlay(): externally shows and starts the animation
// StopAndHide(): externally stops and hides the animation

// Animation logic:
// Uses anchoredPosition to move the UI element
// Uses CanvasGroup alpha to control fade
// Uses coroutine loops to continuously animate

// - Guides the player to perform an upward swipe action
// - Typically used at interaction points such as video pauses or UI prompts

using System.Collections;
using UnityEngine;
// basic imports, no UI needed here since controlling RectTransform and CanvasGroup directly

public class SwipeHintAnimator : MonoBehaviour
// animates the finger hint that tells player to swipe, loops until hidden
{
    public RectTransform fingerRect;
    public CanvasGroup canvasGroup;
    // the finger image rect and its canvas group for fading

    public float moveDistance = 120f;
    public float moveDuration = 1.2f;
    public float fadeDuration = 0.8f;
    public float delayBetween = 0.5f;
    // how far it moves up, how long the move takes, how long the fade takes, gap between loops

    Vector2 startAnchoredPos;
    Coroutine loopCo;
    // save starting position so we can reset it each loop

    void Awake()
    {
        if (fingerRect == null) fingerRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (fingerRect != null) startAnchoredPos = fingerRect.anchoredPosition;
        // grab components and save start position
    }

    void OnEnable()
    {
        StartLoop();
        // auto start animation when object is enabled
    }

    void OnDisable()
    {
        StopLoopAndReset();
        // clean up when disabled
    }

    public void ShowAndPlay()
    {
        gameObject.SetActive(true);
        StartLoop();
        // called externally to show and start the animation
    }

    public void StopAndHide()
    {
        StopLoopAndReset();
        gameObject.SetActive(false);
        // called externally to stop and hide
    }

    void StartLoop()
    {
        if (fingerRect == null || canvasGroup == null) return;

        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(Loop());
        // stop old loop if running then start fresh
    }

    void StopLoopAndReset()
    {
        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }

        if (fingerRect != null) fingerRect.anchoredPosition = startAnchoredPos;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        // reset finger back to start position and make it fully visible
    }

    IEnumerator Loop()
    {
        while (true)
        {
            fingerRect.anchoredPosition = startAnchoredPos;
            canvasGroup.alpha = 1f;
            // reset at start of each loop cycle

            float t = 0f;

            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / moveDuration);

                fingerRect.anchoredPosition = startAnchoredPos + Vector2.up * (moveDistance * p);
                // move finger upward based on progress

                float fadeStart = Mathf.Max(0.01f, moveDuration - fadeDuration);
                if (t >= fadeStart)
                {
                    float ft = Mathf.Clamp01((t - fadeStart) / fadeDuration);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, ft);
                }
                // start fading out near the end of the move, so it disappears as it reaches the top

                yield return null;
            }

            canvasGroup.alpha = 0f;
            yield return new WaitForSecondsRealtime(delayBetween);
            // fully hide then wait before restarting the loop
        }
    }
}