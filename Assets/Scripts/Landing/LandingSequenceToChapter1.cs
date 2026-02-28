using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class LandingSequenceToChapter1 : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;

    [Header("URLs")]
    public string firstVideoURL   = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/1.mp4";
    public string wakeupVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/11wakeup.mp4";
    public string choicesVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/112Choices.mp4";

    [Header("Wakeup Loop (seconds)")]
    public double loopStart = 0.0;
    public double loopEnd   = 15.0;

    [Header("Bubble Hotspot")]
    public GameObject bubbleHotspotObject;
    public Button bubbleButton;

    [Header("Finger Hint")]
    public GameObject fingerHintObject;
    public ClickFingerHintAnimator fingerHintAnimator;

    [Header("CHAPTER 1 Overlay")]
    public GameObject chapterTitleOverlayObject; // IntroOverlay
    public CanvasGroup chapterTitleOverlayGroup; // CanvasGroup on IntroOverlay
    public float titleFadeInDuration = 0.8f;
    public float titleHoldDuration   = 1.2f;
    public float titleFadeOutDuration = 1.2f;

    [Header("Next Scene")]
    public string nextSceneName = "Chapter1";

    bool inLoop = false;
    bool clicked = false;
    bool titleShown = false;

    void Awake()
    {
        // Video baseline
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
        }

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        SetHotspot(false);
        SetFinger(false);

        if (bubbleButton != null)
        {
            bubbleButton.onClick.RemoveListener(OnBubbleClicked);
            bubbleButton.onClick.AddListener(OnBubbleClicked);
        }

        // Title overlay default hidden (no flash)
        if (chapterTitleOverlayObject != null) chapterTitleOverlayObject.SetActive(true);
        if (chapterTitleOverlayGroup != null)
        {
            chapterTitleOverlayGroup.alpha = 0f;
            chapterTitleOverlayGroup.blocksRaycasts = false;
            chapterTitleOverlayGroup.interactable = false;
        }

        // If IntroOverlay has Animator, disable it
        if (chapterTitleOverlayObject != null)
        {
            var anim = chapterTitleOverlayObject.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
        }
    }

    IEnumerator Start()
    {
        // 1) 1.mp4
        yield return PlayUrlAndWaitEnd(firstVideoURL);

        // 2) wakeup loop until click
        yield return PlayUrlPreparedOnly(wakeupVideoURL);

        inLoop = true;
        clicked = false;

        SetHotspot(true);
        SetFinger(true);

        if (videoPlayer != null)
        {
            videoPlayer.time = loopStart;
            videoPlayer.Play();
        }

        while (inLoop && !clicked)
        {
            if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.time >= loopEnd)
            {
                videoPlayer.time = loopStart;
                videoPlayer.Play();
            }
            yield return null;
        }

        // continue from loopEnd to end
        inLoop = false;
        SetFinger(false);
        SetHotspot(false);

        if (videoPlayer != null)
        {
            videoPlayer.time = loopEnd;
            videoPlayer.Play();
        }

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        // 3) choices
        yield return PlayUrlAndWaitEnd(choicesVideoURL);

        // 4) SHOW CHAPTER 1, and TURN OFF VIDEO OUTPUT immediately
        yield return ShowTitleAndTurnOffVideo();

        // 5) Load next scene
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnBubbleClicked()
    {
        if (!inLoop) return;
        clicked = true;
    }

    void SetHotspot(bool show)
    {
        if (bubbleHotspotObject != null) bubbleHotspotObject.SetActive(show);
        if (bubbleButton != null) bubbleButton.interactable = show;
    }

    void SetFinger(bool show)
    {
        if (fingerHintObject != null) fingerHintObject.SetActive(show);
        if (fingerHintAnimator != null) fingerHintAnimator.enabled = show;
    }

    IEnumerator PlayUrlPreparedOnly(string url)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    IEnumerator PlayUrlAndWaitEnd(string url)
    {
        yield return PlayUrlPreparedOnly(url);
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;
    }

    IEnumerator ShowTitleAndTurnOffVideo()
    {
        if (titleShown) yield break;
        titleShown = true;

        if (chapterTitleOverlayGroup == null)
            yield break;

        // ✅ Kill the last video frame completely
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);

        // Title fade in -> hold -> fade out
        chapterTitleOverlayGroup.alpha = 0f;

        yield return Fade(chapterTitleOverlayGroup, 0f, 1f, titleFadeInDuration);
        yield return new WaitForSecondsRealtime(titleHoldDuration);
        yield return Fade(chapterTitleOverlayGroup, 1f, 0f, titleFadeOutDuration);
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }

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