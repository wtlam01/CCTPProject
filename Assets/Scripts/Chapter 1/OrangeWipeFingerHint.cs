using System.Collections;
using UnityEngine;

public class OrangeWipeFingerHint : MonoBehaviour
{
    [Header("Target")]
    public RectTransform fingerRect;
    public CanvasGroup canvasGroup;

    [Header("M Path Points (direct anchored offsets)")]
    public Vector2 p0 = new Vector2(-300f, 180f);  // 左上
    public Vector2 p1 = new Vector2(-150f, -180f); // 左下
    public Vector2 p2 = new Vector2(0f, 140f);     // 中上
    public Vector2 p3 = new Vector2(160f, -160f);  // 右下
    public Vector2 p4 = new Vector2(320f, 120f);   // 右上

    [Header("Timing")]
    public float segmentDuration = 0.22f;
    public float fadeDuration = 0.25f;
    public float holdAtEnd = 0.15f;
    public float loopDelay = 0.35f;

    [Header("Scale Pulse")]
    public bool usePulse = true;
    public float startScale = 1f;
    public float pressScale = 0.9f;

    Vector2 basePos;
    Coroutine loopCo;

    void Reset()
    {
        fingerRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (fingerRect == null)
            fingerRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (fingerRect != null)
            basePos = fingerRect.anchoredPosition;

        HideInstant();
    }

    void OnEnable()
    {
        Play();
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(LoopRoutine());
    }

    public void Stop()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = null;
    }

    public void ShowAndPlay()
    {
        gameObject.SetActive(true);
        HideInstant();
        Play();
    }

    public void HideAndStop()
    {
        Stop();
        HideInstant();
        gameObject.SetActive(false);
    }

    public void HideInstant()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    IEnumerator LoopRoutine()
    {
        while (true)
        {
            yield return Fade(0f, 1f, fadeDuration);

            Vector2 sp0 = basePos + p0;
            Vector2 sp1 = basePos + p1;
            Vector2 sp2 = basePos + p2;
            Vector2 sp3 = basePos + p3;
            Vector2 sp4 = basePos + p4;

            if (fingerRect != null)
            {
                fingerRect.anchoredPosition = sp0;
                fingerRect.localScale = Vector3.one * startScale;
            }

            yield return MoveAndScale(sp0, sp1, startScale, usePulse ? pressScale : startScale, segmentDuration);
            yield return MoveAndScale(sp1, sp2, usePulse ? pressScale : startScale, startScale, segmentDuration);
            yield return MoveAndScale(sp2, sp3, startScale, usePulse ? pressScale : startScale, segmentDuration);
            yield return MoveAndScale(sp3, sp4, usePulse ? pressScale : startScale, startScale, segmentDuration);

            yield return new WaitForSecondsRealtime(holdAtEnd);
            yield return Fade(1f, 0f, fadeDuration);
            yield return new WaitForSecondsRealtime(loopDelay);
        }
    }

    IEnumerator MoveAndScale(Vector2 fromPos, Vector2 toPos, float fromScale, float toScale, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, k);

            if (fingerRect != null)
            {
                fingerRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, eased);

                float s = Mathf.Lerp(fromScale, toScale, eased);
                fingerRect.localScale = Vector3.one * s;
            }

            yield return null;
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        float t = 0f;
        canvasGroup.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}