using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Chapter1WakeupController : MonoBehaviour
{
    [Header("Intro Overlay")]
    public GameObject introOverlayObject;
    public CanvasGroup introOverlayGroup;
    public float introHoldTime = 1.5f;
    public float introFadeDuration = 1.5f;

    [Header("Video Output")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject; // keep this ON for no-black transitions

    [Header("Video URLs")]
    public string firstVideoURL =
        "https://w33lam.panel.uwe.ac.uk/CCTPVideo/1.mp4";
    public string wakeupVideoURL =
        "https://w33lam.panel.uwe.ac.uk/CCTPVideo/11wakeup.mp4";
    public string choicesVideoURL =
        "https://w33lam.panel.uwe.ac.uk/CCTPVideo/112Choices.mp4";

    [Header("Wakeup Loop (seconds)")]
    public double loopStart = 0.0;
    public double loopEnd = 15.0;

    [Header("Bubble Hotspot")]
    public GameObject bubbleHotspotObject;
    public Button bubbleButton;

    [Header("Finger Hint")]
    public GameObject fingerHintObject;
    public ClickFingerHintAnimator fingerHintAnimator;

    bool inLoop = false;
    bool clicked = false;

    // Used to confirm first frame is actually arriving (avoid brief black on some platforms)
    bool gotFirstFrame = false;

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.Stop();

            // Frame callback
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady -= OnFrameReady;
            videoPlayer.frameReady += OnFrameReady;
        }

        // ✅ No black between videos: keep RawImage ON
        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        SetHotspot(false);
        SetFinger(false);

        if (bubbleButton != null)
        {
            bubbleButton.onClick.RemoveListener(OnBubbleClicked);
            bubbleButton.onClick.AddListener(OnBubbleClicked);
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.frameReady -= OnFrameReady;
    }

    void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        gotFirstFrame = true;
        // Optional: stop sending to save overhead
        source.sendFrameReadyEvents = false;
    }

    IEnumerator Start()
    {
        // Intro
        if (introOverlayGroup != null)
        {
            introOverlayGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(introHoldTime);
            yield return Fade(introOverlayGroup, 1f, 0f, introFadeDuration);
        }
        if (introOverlayObject != null)
            introOverlayObject.SetActive(false);

        // 1.mp4 (play to end)
        yield return PlayUrlNoBlackAndWaitEnd(firstVideoURL);

        // wakeup (start + loop 0-15 until click)
        yield return PlayUrlNoBlack(wakeupVideoURL);

        inLoop = true;
        clicked = false;

        SetHotspot(true);
        SetFinger(true);

        videoPlayer.time = loopStart;
        videoPlayer.Play();

        while (inLoop && !clicked)
        {
            if (videoPlayer.time >= loopEnd)
            {
                videoPlayer.time = loopStart;
                videoPlayer.Play();
            }
            yield return null;
        }

        // clicked -> continue from 15s
        inLoop = false;
        SetFinger(false);
        SetHotspot(false);

        videoPlayer.time = loopEnd;
        videoPlayer.Play();

        // wait wakeup end
        while (videoPlayer.isPlaying)
            yield return null;

        // choices
        yield return PlayUrlNoBlack(choicesVideoURL);
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

    /// <summary>
    /// No-black transition: do NOT disable RawImage.
    /// Keeps previous frame visible until the new video produces a frame.
    /// </summary>
    IEnumerator PlayUrlNoBlack(string url)
    {
        if (videoPlayer == null) yield break;

        // Reset "first frame arrived" flag
        gotFirstFrame = false;
        videoPlayer.sendFrameReadyEvents = true;

        // Stop old playback, swap url, prepare
        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();

        // Wait until at least one frame is ready (helps prevent a brief black flash)
        float timeout = 2f;
        float t = 0f;
        while (!gotFirstFrame && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator PlayUrlNoBlackAndWaitEnd(string url)
    {
        yield return PlayUrlNoBlack(url);

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;
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