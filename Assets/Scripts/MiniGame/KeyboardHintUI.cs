using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeyboardHintUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform upRect;     // UpHint (Image) RectTransform
    public RectTransform downRect;   // DownHint (Image) RectTransform
    public CanvasGroup hintGroup;    // CanvasGroup on KeyHint root (recommended)

    [Header("Auto Demo Loop (like SpaceHint)")]
    public float pressDownScale = 0.88f;
    public float pressDownTime  = 0.10f;
    public float releaseTime    = 0.14f;
    public float pressPause     = 0.55f;   // pause after one key press anim
    public float loopDelay      = 0.25f;   // delay between up/down

    [Header("Fade out when finished")]
    public float fadeOutTime = 0.2f;

    Coroutine loopCo;
    bool stopped = false;

    void Awake()
    {
        if (upRect != null) upRect.localScale = Vector3.one;
        if (downRect != null) downRect.localScale = Vector3.one;

        if (hintGroup != null)
        {
            hintGroup.alpha = 1f;
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;
        }
    }

    void OnEnable()
    {
        StartLoop();
    }

    void OnDisable()
    {
        StopLoop();
    }

    public void StartLoop()
    {
        stopped = false;
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(DemoLoop());
    }

    public void StopLoop()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = null;

        if (upRect != null) upRect.localScale = Vector3.one;
        if (downRect != null) downRect.localScale = Vector3.one;
    }

    IEnumerator DemoLoop()
    {
        // loop: Up press -> Down press -> repeat
        while (!stopped)
        {
            if (upRect != null) yield return PressAnim(upRect);
            yield return new WaitForSecondsRealtime(loopDelay);

            if (downRect != null) yield return PressAnim(downRect);
            yield return new WaitForSecondsRealtime(loopDelay);
        }
    }

    IEnumerator PressAnim(RectTransform rect)
    {
        Vector3 baseScale = Vector3.one;
        Vector3 downScale = baseScale * pressDownScale;

        float t = 0f;
        while (t < pressDownTime)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(baseScale, downScale, t / pressDownTime);
            yield return null;
        }
        rect.localScale = downScale;

        t = 0f;
        while (t < releaseTime)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(downScale, baseScale, t / releaseTime);
            yield return null;
        }
        rect.localScale = baseScale;

        yield return new WaitForSecondsRealtime(pressPause);
    }

    // Call this when player pressed ↑ or ↓
    public IEnumerator HideAndDisable()
    {
        stopped = true;
        StopLoop();

        if (hintGroup == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float start = hintGroup.alpha;
        float t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            hintGroup.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / fadeOutTime));
            yield return null;
        }

        hintGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}