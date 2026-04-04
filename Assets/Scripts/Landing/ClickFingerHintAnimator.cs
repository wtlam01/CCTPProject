// 控制click提示嘅手指動畫，手指停喺原位fade in/out同埋tap pulse效果
// tap到底嗰一刻會有白色半透明圓形放大然後消失嘅ripple effect，loop直到被disable
// This script animates a stationary finger hint with fade, tap pulse, and a white ripple circle
// that expands and fades out at the moment the finger reaches the bottom of its tap, looping until disabled externally.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
// unity will auto add these components if missing
public class ClickFingerHintAnimator : MonoBehaviour
// animates a finger hint with tap pulse and ripple effect, no movement
{
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
    // small scale down then back up to simulate a tap

    [Header("Ripple")]
    public RectTransform rippleRect;
    // drag the RippleCircle Image RectTransform here
    public CanvasGroup rippleCanvasGroup;
    // drag the RippleCircle CanvasGroup here
    public float rippleStartSize = 60f;
    public float rippleEndSize = 200f;
    public float rippleDuration = 0.4f;
    public float rippleStartAlpha = 0.5f;
    // white circle expands and fades out when finger hits bottom of tap

    [Header("Loop")]
    public float loopGap = 0.15f;
    // short pause before repeating the animation

    Coroutine co;
    Vector3 baseScale;
    Vector2 basePos;
    // save original scale and position to reset each loop

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        baseScale = transform.localScale;
        basePos = GetComponent<RectTransform>().anchoredPosition;

        canvasGroup.alpha = 0f;
        // start invisible

        if (rippleCanvasGroup != null) rippleCanvasGroup.alpha = 0f;
        if (rippleRect != null) rippleRect.sizeDelta = Vector2.one * rippleStartSize;
        // hide ripple at start

        enabled = false;
        // disabled by default, only starts when enabled externally
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
        transform.localScale = baseScale;

        if (rippleCanvasGroup != null) rippleCanvasGroup.alpha = 0f;
        if (rippleRect != null) rippleRect.sizeDelta = Vector2.one * rippleStartSize;
        // clean up and reset when disabled
    }

    IEnumerator Loop()
    {
        while (true)
        {
            transform.localScale = baseScale;

            yield return Fade(canvasGroup, 0f, 1f, fadeInDuration);
            // fade in finger

            yield return new WaitForSecondsRealtime(pulseDelay);
            // brief pause before tap

            if (enablePulse)
                yield return PulseWithRipple();
            // tap with ripple at the moment finger hits bottom

            yield return Fade(canvasGroup, 1f, 0f, fadeOutDuration);
            // fade out finger

            yield return new WaitForSecondsRealtime(loopGap);
            // gap before repeating
        }
    }

    IEnumerator PulseWithRipple()
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
        transform.localScale = to;
        // finger fully pressed down

        StartCoroutine(RippleEffect());
        // circle appears exactly when finger hits bottom of tap

        t = 0f;
        while (t < pulseDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(to, from, t / pulseDuration);
            yield return null;
        }
        transform.localScale = baseScale;
        // finger bounces back up
    }

    IEnumerator RippleEffect()
    {
        if (rippleRect == null || rippleCanvasGroup == null) yield break;

        rippleRect.sizeDelta = Vector2.one * rippleStartSize;
        rippleCanvasGroup.alpha = rippleStartAlpha;
        // reset ripple to start state

        float t = 0f;
        while (t < rippleDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / rippleDuration);

            rippleRect.sizeDelta = Vector2.one * Mathf.Lerp(rippleStartSize, rippleEndSize, p);
            rippleCanvasGroup.alpha = Mathf.Lerp(rippleStartAlpha, 0f, p);
            // expand circle and fade out at same time

            yield return null;
        }

        rippleCanvasGroup.alpha = 0f;
        rippleRect.sizeDelta = Vector2.one * rippleStartSize;
        // reset after done
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        if (duration <= 0.0001f) { cg.alpha = to; yield break; }

        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
        // reusable fade coroutine
    }
}