using System.Collections;
using UnityEngine;

public class SwipeHintAnimator : MonoBehaviour
{
    public RectTransform fingerRect;
    public CanvasGroup canvasGroup;

    public float moveDistance = 120f;
    public float moveDuration = 1.2f;
    public float fadeDuration = 0.8f;
    public float delayBetween = 0.5f;

    Vector2 startAnchoredPos;
    Coroutine loopCo;

    void Awake()
    {
        if (fingerRect == null) fingerRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (fingerRect != null) startAnchoredPos = fingerRect.anchoredPosition;
    }

    void OnEnable()
    {
        // 如果你係靠 SetActive(true/false) 控制，OnEnable 都可以自動播
        StartLoop();
    }

    void OnDisable()
    {
        StopLoopAndReset();
    }

    // ✅ 給外面呼叫：顯示 + 播動畫
    public void ShowAndPlay()
    {
        gameObject.SetActive(true);
        StartLoop();
    }

    // ✅ 給外面呼叫：停動畫 + 隱藏
    public void StopAndHide()
    {
        StopLoopAndReset();
        gameObject.SetActive(false);
    }

    void StartLoop()
    {
        if (fingerRect == null || canvasGroup == null) return;

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

        if (fingerRect != null) fingerRect.anchoredPosition = startAnchoredPos;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    IEnumerator Loop()
    {
        while (true)
        {
            fingerRect.anchoredPosition = startAnchoredPos;
            canvasGroup.alpha = 1f;

            float t = 0f;

            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / moveDuration);

                fingerRect.anchoredPosition = startAnchoredPos + Vector2.up * (moveDistance * p);

                float fadeStart = Mathf.Max(0.01f, moveDuration - fadeDuration);
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