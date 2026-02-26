using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem; // New Input System

public class SegmentedVideoSwipe_NewInput : MonoBehaviour
{
    [Header("Intro UI (Chapter Two)")]
    public CanvasGroup introOverlayGroup;      // IntroOverlay 的 CanvasGroup（掛喺 IntroOverlay 物件上）
    public float introHoldTime = 1.5f;
    public float introFadeDuration = 1.5f;

    [Header("Sofa / Video UI")]
    public GameObject sofaImage;              // SofaImage (UI Image / GameObject)
    public GameObject videoRawImage;          // VideoRawImage (UI RawImage / GameObject)
    public VideoPlayer videoPlayer;
    public float sofaShowTime = 2f;

    [Header("Swipe Hint UI (optional)")]
    public GameObject swipeHintText;          // 文字提示（可留空）
    public SwipeHintAnimator fingerAnimator;  // 手指提示動畫（建議拖呢個，有就用）
    public GameObject fingerHintFallback;     // 或者直接拖 FingerHint GameObject（可留空）

    [Header("Fade To Black Overlay (between videos)")]
    public CanvasGroup blackFadeGroup;        // BlackFadeOverlay 的 CanvasGroup（alpha 初始 0）
    public float fadeToBlackDuration = 2f;
    public float fadeFromBlackDuration = 0.8f;

    [Header("After Third Video")]
    public GameObject emailImage;             // Email PNG 的 UI Image GameObject（初始 SetActive=false）

    [Header("Video URLs")]
    public string firstVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32Scrollingthephone.mp4";
    public string secondVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/322StressOverload.mp4";
    public string thirdVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/1.mp4";

    [Header("Stop Times (seconds) for FIRST video only")]
    public List<double> stopTimes = new List<double>
    {
        1.8, 3.8, 5.8, 7.8, 9.8, 11.8, 13.4
    };

    [Header("Swipe Settings")]
    public float swipeThreshold = 120f;   // 向上拖動幾多 px 先當 swipe up

    int stopIndex = 0;
    bool waitingForSwipe = false;

    Vector2 startPos;
    bool isPressing = false;

    bool hasShownHintOnce = false; // 只顯示一次手指提示（第一次停點）
    bool switchingVideos = false;  // 防止 loopPointReached 重覆觸發

    void Awake()
    {
        // Intro overlay 預設可見
        if (introOverlayGroup != null)
        {
            introOverlayGroup.alpha = 1f;
            introOverlayGroup.blocksRaycasts = true;
            introOverlayGroup.interactable = true;
        }

        // 黑色淡入淡出 overlay 初始
        if (blackFadeGroup != null)
        {
            blackFadeGroup.alpha = 0f;
            blackFadeGroup.blocksRaycasts = true;  // 你想遮住互動就 true
            blackFadeGroup.interactable = true;
        }

        // 初始 UI 狀態
        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(false);

        SetTextHintVisible(false);
        HideFinger();

        if (emailImage != null) emailImage.SetActive(false);
    }

    IEnumerator Start()
    {
        // 註冊播完事件（任何 video 播完都會進 OnVideoFinished）
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // 1) Intro：停留 -> 淡出
        if (introOverlayGroup != null)
        {
            yield return new WaitForSeconds(introHoldTime);
            yield return FadeCanvasGroup(introOverlayGroup, 1f, 0f, introFadeDuration);
            introOverlayGroup.blocksRaycasts = false;
            introOverlayGroup.interactable = false;
        }

        // 2) Sofa 顯示 2 秒
        if (sofaImage != null) sofaImage.SetActive(true);
        if (videoRawImage != null) videoRawImage.SetActive(false);
        SetTextHintVisible(false);
        HideFinger();

        yield return new WaitForSeconds(sofaShowTime);

        // 3) 切去第一條影片
        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(true);

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned.");
            yield break;
        }

        // 播第一條（有停點互動）
        stopIndex = 0;
        waitingForSwipe = false;
        hasShownHintOnce = false;
        switchingVideos = false;

        yield return PlayVideo(firstVideoURL);
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;
        if (switchingVideos) return;

        bool isFirstVideo = (videoPlayer.url == firstVideoURL);

        if (isFirstVideo)
        {
            // 到停點就停
            if (!waitingForSwipe && stopIndex < stopTimes.Count)
            {
                if (videoPlayer.time >= stopTimes[stopIndex])
                {
                    videoPlayer.Pause();
                    waitingForSwipe = true;

                    // 第一次停點先顯示提示（之後唔再顯示手指）
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
            {
                HandleSwipe_NewInputSystem();
            }
        }
    }

    void HandleSwipe_NewInputSystem()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // mouse down
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isPressing = true;
            startPos = mouse.position.ReadValue();
        }

        // mouse up
        if (isPressing && mouse.leftButton.wasReleasedThisFrame)
        {
            isPressing = false;
            Vector2 endPos = mouse.position.ReadValue();
            float deltaY = endPos.y - startPos.y;

            if (deltaY >= swipeThreshold)
            {
                // swipe up 成功 -> 播下一段
                waitingForSwipe = false;

                // 第一次成功 swipe 後，以後唔再出手指提示
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

        // 第一條播完：只會喺你已經完成全部 stopTimes 的情況下先進入下一條
        if (vp.url == firstVideoURL)
        {
            if (stopIndex < stopTimes.Count)
            {
                // 玩家未 swipe 完就到尾（通常唔會發生，但防呆）
                return;
            }

            StartCoroutine(SwitchToSecondVideo());
            return;
        }

        // 第二條播完：慢慢變黑 -> 播第三條
        if (vp.url == secondVideoURL)
        {
            StartCoroutine(SwitchSecondToThirdWithFade());
            return;
        }

        // 第三條播完：顯示 Email PNG
        if (vp.url == thirdVideoURL)
        {
            StartCoroutine(ShowEmailAfterThird());
            return;
        }
    }

    IEnumerator SwitchToSecondVideo()
    {
        switchingVideos = true;

        // 第二條暫時無停點互動
        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        yield return PlayVideo(secondVideoURL);

        switchingVideos = false;
    }

    IEnumerator SwitchSecondToThirdWithFade()
    {
        switchingVideos = true;

        // 1) 淡去黑色
        if (blackFadeGroup != null)
        {
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);
        }

        // 2) 換第三條影片
        yield return PlayVideo(thirdVideoURL);

        // 3) 可選：由黑淡返出嚟（如果你想第三條一開始係黑，再慢慢見到畫面）
        if (blackFadeGroup != null)
        {
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, fadeFromBlackDuration);
        }

        switchingVideos = false;
    }

    IEnumerator ShowEmailAfterThird()
    {
        switchingVideos = true;

        // 停止影片
        if (videoPlayer != null) videoPlayer.Stop();

        // 讓畫面保持黑（你想保持黑就 fade 到 1）
        if (blackFadeGroup != null)
        {
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, 0.8f);
        }

        // 顯示 Email PNG
        if (emailImage != null)
        {
            emailImage.SetActive(true);
        }

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
        // 優先用 Animator（會自動 loop move+fade）
        if (fingerAnimator != null)
        {
            fingerAnimator.gameObject.SetActive(true);
            return;
        }

        if (fingerHintFallback != null) fingerHintFallback.SetActive(true);
    }

    void HideFinger()
    {
        if (fingerAnimator != null)
        {
            fingerAnimator.gameObject.SetActive(false);
        }

        if (fingerHintFallback != null)
        {
            fingerHintFallback.SetActive(false);
        }
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