using System.Collections;
using UnityEngine;

public class SwipeHintAnimator_Chapter1 : MonoBehaviour
{
    public RectTransform fingerRect;
    public CanvasGroup canvasGroup;

    [Header("Anim")]
    public float moveDistance = 120f;
    public float moveDuration = 1.2f;
    public float fadeDuration = 0.8f;
    public float delayBetween = 0.5f;

    Vector2 baseAnchoredPos;
    Coroutine loopCo;

    void Awake()
    {
        if (fingerRect == null) fingerRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        StartLoopFromCurrentPos();
    }

    void OnDisable()
    {
        StopLoopAndReset();
    }

    public void SetBaseFrom(RectTransform targetPosRect)
    {
        if (fingerRect == null || targetPosRect == null) return;
        fingerRect.anchoredPosition = targetPosRect.anchoredPosition;
        baseAnchoredPos = fingerRect.anchoredPosition;
    }

    public void ShowAndPlay()
    {
        gameObject.SetActive(true);
        StartLoopFromCurrentPos();
    }

    public void StopAndHide()
    {
        StopLoopAndReset();
        gameObject.SetActive(false);
    }

    void StartLoopFromCurrentPos()
    {
        if (fingerRect == null || canvasGroup == null) return;

        baseAnchoredPos = fingerRect.anchoredPosition;

        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(Loop());
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
    }

    IEnumerator Loop()
    {
        while (true)
        {
            fingerRect.anchoredPosition = baseAnchoredPos;
            canvasGroup.alpha = 1f;

            float t = 0f;
            float fadeStart = Mathf.Max(0.01f, moveDuration - fadeDuration);

            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / moveDuration);

                fingerRect.anchoredPosition = baseAnchoredPos + Vector2.up * (moveDistance * p);

                if (t >= fadeStart)
                {
                    float ft = Mathf.Clamp01((t - fadeStart) / fadeDuration);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, ft);
                }

                yield return null;
            }

            canvasGroup.alpha = 0f;
            yield return new WaitForSecondsRealtime(delayBetween);
        }
    }
}