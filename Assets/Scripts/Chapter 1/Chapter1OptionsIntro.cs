using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Chapter1OptionsIntro : MonoBehaviour
{
    [Header("Timing")]
    public float delayBeforeOptions = 2f;
    public float dimFadeDuration = 0.35f;

    [Header("Dim Overlay")]
    public CanvasGroup dimOverlayGroup;   // CanvasGroup on DimOverlay
    [Range(0f, 1f)] public float dimTargetAlpha = 0.35f;

    [Header("Options")]
    public CanvasGroup[] optionGroups;    // CanvasGroup on each icon (Option_Chat, Option_Study)
    public float optionsFadeDuration = 0.25f;

    void Awake()
    {
        // start: no dim, no options
        if (dimOverlayGroup != null)
        {
            dimOverlayGroup.alpha = 0f;
            dimOverlayGroup.blocksRaycasts = false;
            dimOverlayGroup.interactable = false;
        }

        if (optionGroups != null)
        {
            foreach (var g in optionGroups)
            {
                if (g == null) continue;
                g.alpha = 0f;
                g.blocksRaycasts = false;
                g.interactable = false;
                g.gameObject.SetActive(true); // keep active, just invisible
            }
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(delayBeforeOptions);

        // dim in
        if (dimOverlayGroup != null)
            yield return Fade(dimOverlayGroup, 0f, dimTargetAlpha, dimFadeDuration);

        // options fade in + enable clicks
        if (optionGroups != null)
        {
            foreach (var g in optionGroups)
            {
                if (g == null) continue;
                g.blocksRaycasts = true;
                g.interactable = true;
            }

            // fade all together
            float t = 0f;
            while (t < optionsFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / optionsFadeDuration);
                foreach (var g in optionGroups)
                    if (g != null) g.alpha = a;
                yield return null;
            }
            foreach (var g in optionGroups)
                if (g != null) g.alpha = 1f;
        }
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        if (duration <= 0.0001f) { cg.alpha = to; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}