// Chapter 1專用嘅swipe提示動畫，手指向上移動同時慢慢淡出，loop直到隱藏。
// 同原版SwipeHintAnimator唔同嘅係，呢個可以由外部設定起始位置，用於rest video嘅唔同停頓點。
// This script animates a swipe hint finger that moves upward and fades out in a loop,
// with the ability to reposition itself externally for different swipe stop positions in the rest video.

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