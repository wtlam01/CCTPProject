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