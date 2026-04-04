// Control click提示嘅手指動畫，手指由右向左滑動，有fade in/out同埋tap pulse效果
// loop直到被disable，用嚟提示玩家click bubble hotspot
// This script animates a finger hint that slides right to left with fade and a subtle tap pulse,
// looping until disabled externally to show the player where to click.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
// unity will auto add these components if missing
public class ClickFingerHintAnimator : MonoBehaviour
// animates a finger hint that moves right to left, fades in and out, with a little pulse tap effect
{
    [Header("Move (Right -> Left)")]
    public RectTransform fingerRect;
    public Vector2 offsetFrom = new Vector2(60f, 0f);
    public Vector2 offsetTo   = new Vector2(0f, 0f);
    public float moveDuration = 0.6f;
    public float holdTime     = 0.15f;
    // finger starts 60 units to the right and slides left to center

    [Header("Fade")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration  = 0.15f;
    public float fadeOutDuration = 0.25f;
    // quick fade in, slightly slower fade out

    [Header("Tap / Pulse")]
    public bool enablePulse = true;
    public float pulseScale = 0.92f;
    public float pulseDuration = 0.16f;
    public float pulseDelay = 0.25f;
    // small scale down then back up to simulate a tap, makes it feel more interactive

    [Header("Loop")]
    public float loopGap = 0.15f;
    // short pause before repeating the animation

    Coroutine co;
    Vector3 baseScale;
    Vector2 basePos;
    // save original scale and position to reset each loop

    void Awake()
    {
        if (fingerRect == null) fingerRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        baseScale = transform.localScale;
        basePos = fingerRect.anchoredPosition;

        canvasGroup.alpha = 0f;
        // start invisible

        enabled = false;
        // disabled by default, only starts when enabled externally by the wakeup controller
    }

    void OnEnable()
    {
        co = StartCoroutine(Loop());
        // start animation loop when enabled
    }

    void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        canvasGroup.alpha = 0f;
        fingerRect.anchoredPosition = basePos;
        transform.localScale = baseScale;
        // clean up and reset when disabled
    }

    IEnumerator Loop()
    {
        while (true)
        {
            fingerRect.anchoredPosition = basePos + offsetFrom;
            transform.localScale = baseScale;
            // reset to starting position before each cycle

            yield return Fade(0f, 1f, fadeInDuration);
            // fade in

            yield return Move(basePos + offsetFrom, basePos + offsetTo, moveDuration);
            // slide from right to left

            yield return new WaitForSecondsRealtime(holdTime);
            // brief pause at destination

            if (enablePulse)
            {
                yield return new WaitForSecondsRealtime(pulseDelay);
                yield return Pulse();
            }
            // do the tap pulse if enabled

            yield return Fade(1f, 0f, fadeOutDuration);
            // fade out

            yield return new WaitForSecondsRealtime(loopGap);
            // gap before repeating
        }
    }

    IEnumerator Move(Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fingerRect.anchoredPosition = Vector2.Lerp(from, to, t / duration);
            yield return null;
        }
        fingerRect.anchoredPosition = to;
        // lerp position from right to left over duration
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0.0001f) { canvasGroup.alpha = to; yield break; }

        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        // reusable fade, snaps instantly if duration is near zero
    }

    IEnumerator Pulse()
    {
        Vector3 from = baseScale;
        Vector3 to = baseScale * pulseScale;

        float t = 0f;
        while (t < pulseDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(from, to, t / pulseDuration);
            yield return null;
        }
        // scale down first half

        t = 0f;
        while (t < pulseDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(to, from, t / pulseDuration);
            yield return null;
        }

        transform.localScale = baseScale;
        // scale back up second half, gives a squeeze tap feel
    }
}