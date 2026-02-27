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
    public string firstVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32Scrollingthephone.mp4";
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
            blackFadeGroup.alpha = 0f;
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

        yield return new WaitForSeconds(sofaShowTime);

        // 播第一條
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

        if (blackFadeGroup != null) blackFadeGroup.alpha = 0f;

        yield return PlayVideo(firstVideoURL);
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;
        if (switchingVideos) return;

        bool isFirstVideo = (videoPlayer.url == firstVideoURL);
        if (!isFirstVideo) return;

        // 到停點就停
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

        // 停住時等 swipe up
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

        // 第二條播完係 fallback（如果 length 讀唔到，至少播完會切）
        if (vp.url == secondVideoURL)
        {
            if (secondWatcherCo == null)
                StartCoroutine(SwitchSecondToThird_Fallback());
            return;
        }
    }

    IEnumerator SwitchToSecondVideo()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        if (blackFadeGroup != null) blackFadeGroup.alpha = 0f;

        yield return PlayVideo(secondVideoURL);

        // ✅ 開始監察第二條：到尾 2 秒 -> fade -> 換第三條
        if (secondWatcherCo != null) StopCoroutine(secondWatcherCo);
        secondWatcherCo = StartCoroutine(WatchSecondAndFadeToThird());

        switchingVideos = false;
    }

    IEnumerator WatchSecondAndFadeToThird()
    {
        // 等到 length 可用（URL 有時慢）
        double length = videoPlayer.length;
        float timeout = 3f;
        while ((length <= 0.1 || double.IsNaN(length)) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            length = videoPlayer.length;
            yield return null;
        }

        // 如果拎唔到 length，就唔做「尾段 fade」，等 loopPointReached fallback
        if (length <= 0.1 || double.IsNaN(length))
        {
            secondWatcherCo = null;
            yield break;
        }

        double fadeStartTime = System.Math.Max(0.0, length - secondFadeOutLastSeconds);

        // 等到接近尾段
        while (videoPlayer.isPlaying && videoPlayer.time < fadeStartTime)
            yield return null;

        // 開始淡出到黑
        switchingVideos = true;

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);

        // 換第三條
        yield return PlayVideo(thirdVideoURL);

        // 由黑淡出
        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, fadeFromBlackDuration);

        switchingVideos = false;
        secondWatcherCo = null;
    }

    IEnumerator SwitchSecondToThird_Fallback()
    {
        switchingVideos = true;

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);

        yield return PlayVideo(thirdVideoURL);

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, fadeFromBlackDuration);

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

    IEnumerator PlayVideo(string url)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.url = url;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();
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