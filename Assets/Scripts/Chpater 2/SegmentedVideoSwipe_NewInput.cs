using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class SegmentedVideoSwipe_NewInput : MonoBehaviour
{
    [Header("Intro UI (Chapter Two)")]
    public CanvasGroup introOverlayGroup;
    public float introHoldTime = 1.5f;
    public float introFadeDuration = 1.5f;

    [Header("Sofa / Video UI")]
    public GameObject sofaImage;
    public GameObject videoRawImage;
    public VideoPlayer videoPlayer;
    public float sofaShowTime = 2f;

    [Header("Swipe Hint UI (optional)")]
    public GameObject swipeHintText;
    public SwipeHintAnimator fingerAnimator;
    public GameObject fingerHintFallback;

    [Header("Fade To Black Overlay (between videos)")]
    public CanvasGroup blackFadeGroup;
    public float fadeFromBlackDuration = 0.8f;

    [Header("Second -> Third Fade")]
    [Tooltip("第二條片最後幾秒開始淡出（你要 2 秒）")]
    public float secondFadeOutLastSeconds = 2f;

    [Tooltip("淡出到全黑用幾耐（通常同上面一樣 2 秒）")]
    public float fadeToBlackDuration = 2f;

    [Header("After Third Video")]
    public GameObject emailImage;

    [Header("Video URLs")]
    public string preFirstVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/30IgNotice.mp4";   // ✅ 新增：先播呢條
    public string firstVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32Scrollingthephone.mp4"; // ✅ 呢條仍然係 swipe video
    public string secondVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/322StressOverload.mp4";
    public string thirdVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/311street1.mp4";

    [Header("Stop Times (seconds) for FIRST video only")]
    public List<double> stopTimes = new List<double>
    {
        1.8, 3.8, 5.8, 7.8, 9.8, 11.8, 13.4
    };

    [Header("Swipe Settings")]
    public float swipeThreshold = 120f;

    int stopIndex = 0;
    bool waitingForSwipe = false;

    Vector2 startPos;
    bool isPressing = false;

    bool hasShownHintOnce = false;
    bool switchingVideos = false;

    Coroutine secondWatcherCo;

    void Awake()
    {
        if (introOverlayGroup != null)
        {
            introOverlayGroup.alpha = 1f;
            introOverlayGroup.blocksRaycasts = true;
            introOverlayGroup.interactable = true;
        }

        if (blackFadeGroup != null)
        {
            blackFadeGroup.alpha = 1f;
            blackFadeGroup.blocksRaycasts = true;
            blackFadeGroup.interactable = true;
        }

        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(false);

        SetTextHintVisible(false);
        HideFinger();

        if (emailImage != null) emailImage.SetActive(false);
    }

    IEnumerator Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.waitForFirstFrame = true;
        }

        // Intro：停留 -> 淡出
        if (introOverlayGroup != null)
        {
            yield return new WaitForSeconds(introHoldTime);
            yield return FadeCanvasGroup(introOverlayGroup, 1f, 0f, introFadeDuration);
            introOverlayGroup.blocksRaycasts = false;
            introOverlayGroup.interactable = false;
        }

        // Sofa 顯示
        if (sofaImage != null) sofaImage.SetActive(true);
        if (videoRawImage != null) videoRawImage.SetActive(false);
        SetTextHintVisible(false);
        HideFinger();

        // 黑淡出，露返 sofa
        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, 0.6f);

        yield return new WaitForSeconds(sofaShowTime);

        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(true);

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned.");
            yield break;
        }

        stopIndex = 0;
        waitingForSwipe = false;
        hasShownHintOnce = false;
        switchingVideos = false;

        // ✅ 先播 notice，再播 scrolling phone
        yield return StartCoroutine(PlayPreFirstThenFirst());
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;
        if (switchingVideos) return;

        // ✅ swipe stop 只作用喺 firstVideoURL（即 32Scrollingthephone）
        bool isFirstVideo = (videoPlayer.url == firstVideoURL);
        if (!isFirstVideo) return;

        if (!waitingForSwipe && stopIndex < stopTimes.Count)
        {
            if (videoPlayer.time >= stopTimes[stopIndex])
            {
                videoPlayer.Pause();
                waitingForSwipe = true;

                if (!hasShownHintOnce)
                {
                    ShowFinger();
                    SetTextHintVisible(true);
                }
                else
                {
                    HideFinger();
                    SetTextHintVisible(false);
                }
            }
        }

        if (waitingForSwipe)
            HandleSwipe_NewInputSystem();
    }

    void HandleSwipe_NewInputSystem()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            isPressing = true;
            startPos = mouse.position.ReadValue();
        }

        if (isPressing && mouse.leftButton.wasReleasedThisFrame)
        {
            isPressing = false;
            Vector2 endPos = mouse.position.ReadValue();
            float deltaY = endPos.y - startPos.y;

            if (deltaY >= swipeThreshold)
            {
                waitingForSwipe = false;

                if (!hasShownHintOnce) hasShownHintOnce = true;

                HideFinger();
                SetTextHintVisible(false);

                stopIndex++;
                videoPlayer.Play();
            }
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (switchingVideos) return;

        // ✅ pre-first 播完 -> 播 first
        if (vp.url == preFirstVideoURL)
        {
            StartCoroutine(SwitchPreFirstToFirst());
            return;
        }

        if (vp.url == firstVideoURL)
        {
            if (stopIndex < stopTimes.Count) return;
            StartCoroutine(SwitchToSecondVideo());
            return;
        }

        if (vp.url == thirdVideoURL)
        {
            StartCoroutine(ShowEmailAfterThird());
            return;
        }

        if (vp.url == secondVideoURL)
        {
            if (secondWatcherCo == null)
                StartCoroutine(SwitchSecondToThird_Fallback());
            return;
        }
    }

    IEnumerator PlayPreFirstThenFirst()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        // 先播 notice
        yield return PlayVideoCovered(preFirstVideoURL, fadeFromBlack: true);

        switchingVideos = false;
    }

    IEnumerator SwitchPreFirstToFirst()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        stopIndex = 0;
        hasShownHintOnce = false;

        // 再播 scrolling phone（有 stop/swipe）
        yield return PlayVideoCovered(firstVideoURL, fadeFromBlack: true);

        switchingVideos = false;
    }

    IEnumerator SwitchToSecondVideo()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        yield return PlayVideoCovered(secondVideoURL, fadeFromBlack: true);

        if (secondWatcherCo != null) StopCoroutine(secondWatcherCo);
        secondWatcherCo = StartCoroutine(WatchSecondAndFadeToThird());

        switchingVideos = false;
    }

    IEnumerator WatchSecondAndFadeToThird()
    {
        double length = videoPlayer.length;
        float timeout = 3f;
        while ((length <= 0.1 || double.IsNaN(length)) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            length = videoPlayer.length;
            yield return null;
        }

        if (length <= 0.1 || double.IsNaN(length))
        {
            secondWatcherCo = null;
            yield break;
        }

        double fadeStartTime = System.Math.Max(0.0, length - secondFadeOutLastSeconds);

        while (videoPlayer.isPlaying && videoPlayer.time < fadeStartTime)
            yield return null;

        switchingVideos = true;

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);

        yield return PlayVideoCovered(thirdVideoURL, fadeFromBlack: true);

        switchingVideos = false;
        secondWatcherCo = null;
    }

    IEnumerator SwitchSecondToThird_Fallback()
    {
        switchingVideos = true;

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);

        yield return PlayVideoCovered(thirdVideoURL, fadeFromBlack: true);

        switchingVideos = false;
    }

    IEnumerator ShowEmailAfterThird()
    {
        switchingVideos = true;

        if (videoPlayer != null) videoPlayer.Stop();

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, 0.8f);

        if (emailImage != null)
            emailImage.SetActive(true);

        switchingVideos = false;
    }

    IEnumerator PlayVideoCovered(string url, bool fadeFromBlack)
    {
        if (videoPlayer == null) yield break;

        if (blackFadeGroup != null)
            blackFadeGroup.alpha = 1f;

        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();

        float t = 0f;
        while (videoPlayer.texture == null && t < 2f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (fadeFromBlack && blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, fadeFromBlackDuration);
    }

    void SetTextHintVisible(bool visible)
    {
        if (swipeHintText != null) swipeHintText.SetActive(visible);
    }

    void ShowFinger()
    {
        if (fingerAnimator != null)
        {
            fingerAnimator.gameObject.SetActive(true);
            return;
        }

        if (fingerHintFallback != null) fingerHintFallback.SetActive(true);
    }

    void HideFinger()
    {
        if (fingerAnimator != null) fingerAnimator.gameObject.SetActive(false);
        if (fingerHintFallback != null) fingerHintFallback.SetActive(false);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float t = 0f;
        cg.alpha = from;

        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }
}