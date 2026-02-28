using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class ClickFingerHintAnimator : MonoBehaviour
{
    [Header("Move (Right -> Left)")]
    public RectTransform fingerRect;
    public Vector2 offsetFrom = new Vector2(60f, 0f);
    public Vector2 offsetTo   = new Vector2(0f, 0f);
    public float moveDuration = 0.6f;
    public float holdTime     = 0.15f;

    [Header("Fade")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration  = 0.15f;
    public float fadeOutDuration = 0.25f;

    [Header("Tap / Pulse")]
    public bool enablePulse = true;
    public float pulseScale = 0.92f;
    public float pulseDuration = 0.16f;
    public float pulseDelay = 0.25f;

    [Header("Loop")]
    public float loopGap = 0.15f;

    Coroutine co;
    Vector3 baseScale;
    Vector2 basePos;

void Awake()
{
    if (fingerRect == null) fingerRect = GetComponent<RectTransform>();
    if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

    baseScale = transform.localScale;
    basePos = fingerRect.anchoredPosition;

    canvasGroup.alpha = 0f;

    // ✅ key：預設唔播，等 Chapter1WakeupController 需要先 enabled
    enabled = false;
}
    void OnEnable()
    {
        co = StartCoroutine(Loop());
    }

    void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        canvasGroup.alpha = 0f;
        fingerRect.anchoredPosition = basePos;
        transform.localScale = baseScale;
    }

    IEnumerator Loop()
    {
        while (true)
        {
            fingerRect.anchoredPosition = basePos + offsetFrom;
            transform.localScale = baseScale;

            yield return Fade(0f, 1f, fadeInDuration);
            yield return Move(basePos + offsetFrom, basePos + offsetTo, moveDuration);

            yield return new WaitForSecondsRealtime(holdTime);

            if (enablePulse)
            {
                yield return new WaitForSecondsRealtime(pulseDelay);
                yield return Pulse();
            }

            yield return Fade(1f, 0f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(loopGap);
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

        t = 0f;
        while (t < pulseDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(to, from, t / pulseDuration);
            yield return null;
        }

        transform.localScale = baseScale;
    }
}