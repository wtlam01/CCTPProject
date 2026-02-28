using System.Collections;
using UnityEngine;
using TMPro;

public class ChapterIntroController : MonoBehaviour
{
    [Header("Intro Overlay")]
    public CanvasGroup introOverlayGroup;
    public TMP_Text chapterText;
    public string chapterTitle = "CHAPTER 1";
    public float holdTime = 1.5f;
    public float fadeDuration = 1.5f;

    [Header("After Intro (enable these)")]
    public GameObject[] enableAfterIntro;

    [Header("After Intro (optional callback)")]
    public Chapter1VideoController chapter1VideoController; // 拖入去
    public bool playVideoAfterIntro = true;

    void Awake()
    {
        if (chapterText != null) chapterText.text = chapterTitle;

        if (introOverlayGroup != null)
        {
            introOverlayGroup.alpha = 1f;
            introOverlayGroup.blocksRaycasts = true;
            introOverlayGroup.interactable = true;
        }

        if (enableAfterIntro != null)
        {
            foreach (var go in enableAfterIntro)
                if (go != null) go.SetActive(false);
        }
    }

    IEnumerator Start()
    {
        if (introOverlayGroup == null) yield break;

        yield return new WaitForSecondsRealtime(holdTime);
        yield return FadeCanvasGroup(introOverlayGroup, 1f, 0f, fadeDuration);

        introOverlayGroup.blocksRaycasts = false;
        introOverlayGroup.interactable = false;

        if (enableAfterIntro != null)
        {
            foreach (var go in enableAfterIntro)
                if (go != null) go.SetActive(true);
        }

        if (playVideoAfterIntro && chapter1VideoController != null)
        {
            chapter1VideoController.PlayChapter1Video();
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
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