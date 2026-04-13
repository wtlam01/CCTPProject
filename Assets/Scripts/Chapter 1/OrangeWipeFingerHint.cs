// script 控制 overwork wipe 效果入面嘅finger提示動畫：
// 一開始：
// 1. 手指先 fade in
// 2. 手指會沿住預設嘅 M 字路徑移動（p0 → p4）
// 3. 移動途中會配合縮放變化，模擬按壓／抹動作
// 4. 到達終點後停一小段時間
// 5. 然後 fade out
// 6. 停一小段時間後再重複 loop

// 呢個提示會喺 overwork 嘅橙色 wipe overlay 入面出現
// 用嚟引導玩家做出來回擦拭畫面嘅動作，直到系統將佢停用為止

// This script controls the finger hint animation used in the overwork wipe sequence:
// At the start:
// 1. The finger fades in
// 2. It moves along a predefined M-shaped path (p0 → p4)
// 3. Its scale changes during movement to simulate pressing / wiping
// 4. It briefly holds at the end
// 5. Then fades out
// 6. After a short delay, the loop repeats

// This hint appears during the orange overwork wipe overlay,
// guiding the player to wipe back and forth across the screen
// until the system stops the animation externally.

using System.Collections;
using UnityEngine;

public class OrangeWipeFingerHint : MonoBehaviour
// animates a finger hint that traces an M shape path, used during the orange wipe overwork effect
{
    [Header("Target")]
    public RectTransform fingerRect;
    public CanvasGroup canvasGroup;
    // the finger image and its canvas group for fading

    [Header("M Path Points (direct anchored offsets)")]
    public Vector2 p0 = new Vector2(-300f, 180f);  // 左上
    public Vector2 p1 = new Vector2(-150f, -180f); // 左下
    public Vector2 p2 = new Vector2(0f, 140f);     // 中上
    public Vector2 p3 = new Vector2(160f, -160f);  // 右下
    public Vector2 p4 = new Vector2(320f, 120f);   // 右上
    // five points that form an M shape, finger moves through them in order

    [Header("Timing")]
    public float segmentDuration = 0.22f;
    public float fadeDuration = 0.25f;
    public float holdAtEnd = 0.15f;
    public float loopDelay = 0.35f;
    // how long each segment takes, how long fade is, brief hold at end before looping

    [Header("Scale Pulse")]
    public bool usePulse = true;
    public float startScale = 1f;
    public float pressScale = 0.9f;
    // finger slightly shrinks as it moves down, grows back as it moves up, simulates pressing

    Vector2 basePos;
    Coroutine loopCo;
    // base position used as offset origin for all the M path points

    void Reset()
    {
        fingerRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // auto assign in editor when component added
    }

    void Awake()
    {
        if (fingerRect == null)
            fingerRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (fingerRect != null)
            basePos = fingerRect.anchoredPosition;
        // save starting position as the base for all offsets

        HideInstant();
        // start hidden
    }

    void OnEnable()
    {
        Play();
        // auto play when enabled
    }

    void OnDisable()
    {
        Stop();
        // clean up when disabled
    }

    public void Play()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(LoopRoutine());
        // stop old loop and start fresh
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
        // show object then start animation, called externally
    }

    public void HideAndStop()
    {
        Stop();
        HideInstant();
        gameObject.SetActive(false);
        // stop animation then hide, called externally
    }

    public void HideInstant()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        // instantly make invisible without animation
    }

    IEnumerator LoopRoutine()
    {
        while (true)
        {
            yield return Fade(0f, 1f, fadeDuration);
            // fade in before starting movement

            Vector2 sp0 = basePos + p0;
            Vector2 sp1 = basePos + p1;
            Vector2 sp2 = basePos + p2;
            Vector2 sp3 = basePos + p3;
            Vector2 sp4 = basePos + p4;
            // calculate actual world positions by adding offsets to base pos

            if (fingerRect != null)
            {
                fingerRect.anchoredPosition = sp0;
                fingerRect.localScale = Vector3.one * startScale;
            }
            // snap to start of M path before animating

            yield return MoveAndScale(sp0, sp1, startScale, usePulse ? pressScale : startScale, segmentDuration);
            yield return MoveAndScale(sp1, sp2, usePulse ? pressScale : startScale, startScale, segmentDuration);
            yield return MoveAndScale(sp2, sp3, startScale, usePulse ? pressScale : startScale, segmentDuration);
            yield return MoveAndScale(sp3, sp4, usePulse ? pressScale : startScale, startScale, segmentDuration);
            // move through each segment of the M, alternating scale to simulate press feel

            yield return new WaitForSecondsRealtime(holdAtEnd);
            yield return Fade(1f, 0f, fadeDuration);
            yield return new WaitForSecondsRealtime(loopDelay);
            // hold briefly, fade out, wait before repeating
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
            // using SmoothStep instead of linear lerp, makes movement feel more natural

            if (fingerRect != null)
            {
                fingerRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, eased);

                float s = Mathf.Lerp(fromScale, toScale, eased);
                fingerRect.localScale = Vector3.one * s;
            }
            // move position and scale at same time using eased value

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
        // standard fade coroutine
    }
}

//Unity Technologies (2023) Mathf.SmoothStep. https://docs.unity3d.com/ScriptReference/Mathf.SmoothStep.html
// movement smoother than linear interpolation
// more natural motion