// script 控制 Chapter 1 嘅 swipe 手指提示動畫：
// 一開始：
// 1. 手指會出現在指定位置（可由外部設定）
// 2. 手指向上移動（模擬 swipe 上）
// 3. 移動途中逐漸淡出
// 4. 到達頂部後完全消失
// 5. 停一小段時間
// 6. 再回到起始位置並重複 loop

// this hints is to 用喺 Rest video 嘅停頓位置
// 用嚟引導玩家做「向上滑」嘅操作
// 同一般 swipe hint 唔同，呢個可以動態設定位置（SetBaseFrom）
// 所以可以喺唔同時間點出現喺唔同位置

// This script controls the swipe hint animation used in Chapter 1:
// At the start:
// 1. The finger appears at a specified position (set externally)
// 2. It moves upward to simulate a swipe gesture
// 3. It gradually fades out during the movement
// 4. It becomes fully invisible at the top
// 5. Waits briefly
// 6. Then resets to the starting position and loops

// This hint is used at pause points in the Rest video,
// guiding the player to perform an upward swipe.
// Unlike a basic swipe hint, this version allows dynamic repositioning (SetBaseFrom),
// so it can be reused at multiple points in the sequence.

using System.Collections;
using UnityEngine;

public class SwipeHintAnimator_Chapter1 : MonoBehaviour
// chapter 1 version of the swipe hint, moves upward and fades out, loops until hidden
// difference from the original SwipeHintAnimator is this one can set its base position externally
{
    public RectTransform fingerRect;
    public CanvasGroup canvasGroup;
    // finger image and canvas group for fading

    [Header("Anim")]
    public float moveDistance = 120f;
    public float moveDuration = 1.2f;
    public float fadeDuration = 0.8f;
    public float delayBetween = 0.5f;
    // how far it moves up, how long the move takes, when it starts fading, gap between loops

    Vector2 baseAnchoredPos;
    Coroutine loopCo;
    // saves starting position so loop resets correctly each cycle

    void Awake()
    {
        if (fingerRect == null) fingerRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        // auto grab if not assigned
    }

    void OnEnable()
    {
        StartLoopFromCurrentPos();
        // auto start when enabled
    }

    void OnDisable()
    {
        StopLoopAndReset();
        // clean up when disabled
    }

    public void SetBaseFrom(RectTransform targetPosRect)
    {
        if (fingerRect == null || targetPosRect == null) return;
        fingerRect.anchoredPosition = targetPosRect.anchoredPosition;
        baseAnchoredPos = fingerRect.anchoredPosition;
        // lets caller reposition the hint to a specific location before playing
        // used for rest video stops where hint appears at different positions each time
    }

    public void ShowAndPlay()
    {
        gameObject.SetActive(true);
        StartLoopFromCurrentPos();
        // called externally to show and start
    }

    public void StopAndHide()
    {
        StopLoopAndReset();
        gameObject.SetActive(false);
        // called externally to stop and hide
    }

    void StartLoopFromCurrentPos()
    {
        if (fingerRect == null || canvasGroup == null) return;

        baseAnchoredPos = fingerRect.anchoredPosition;
        // save current position as base before starting loop

        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(Loop());
        // stop old loop and start fresh
    }

    void StopLoopAndReset()
    {
        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }

        if (fingerRect != null) fingerRect.anchoredPosition = baseAnchoredPos;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        // reset position and alpha when stopped
    }

    IEnumerator Loop()
    {
        while (true)
        {
            fingerRect.anchoredPosition = baseAnchoredPos;
            canvasGroup.alpha = 1f;
            // reset at start of each cycle

            float t = 0f;
            float fadeStart = Mathf.Max(0.01f, moveDuration - fadeDuration);
            // calculate when fading should begin, near the end of the move

            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / moveDuration);

                fingerRect.anchoredPosition = baseAnchoredPos + Vector2.up * (moveDistance * p);
                // move upward based on progress

                if (t >= fadeStart)
                {
                    float ft = Mathf.Clamp01((t - fadeStart) / fadeDuration);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, ft);
                }
                // start fading out near the top of the movement

                yield return null;
            }

            canvasGroup.alpha = 0f;
            yield return new WaitForSecondsRealtime(delayBetween);
            // fully hide then wait before next loop
        }
    }
}