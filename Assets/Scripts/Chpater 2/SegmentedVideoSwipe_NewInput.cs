using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem; // New Input System

public class SegmentedVideoSwipe_NewInput : MonoBehaviour
{
    [Header("Intro UI (Chapter Two)")]
    public CanvasGroup introOverlayGroup;      // IntroOverlay (CanvasGroup)
    public float introHoldTime = 1.5f;
    public float introFadeDuration = 1.5f;

    [Header("Sofa / Video UI")]
    public GameObject sofaImage;              // SofaImage (UI Image)
    public GameObject videoRawImage;          // VideoRawImage (UI RawImage)
    public VideoPlayer videoPlayer;
    public float sofaShowTime = 2f;

    [Header("Swipe Hint UI (optional)")]
    public GameObject swipeHintText;          // 文字提示 (可留空)
    public SwipeHintAnimator fingerAnimator;  // 手指動畫腳本 (建議拖呢個)
    public GameObject fingerHintFallback;     // 或者直接拖 FingerHint GameObject (可留空)

    [Header("Video URLs")]
    public string firstVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32Scrollingthephone.mp4";
    public string nextVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/322StressOverload.mp4";

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

    void Awake()
    {
        // Intro overlay 預設可見
        if (introOverlayGroup != null)
        {
            introOverlayGroup.alpha = 1f;
            introOverlayGroup.blocksRaycasts = true;
            introOverlayGroup.interactable = true;
        }

        // 初始 UI 狀態
        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(false);

        SetTextHintVisible(false);
        HideFinger();
    }

    IEnumerator Start()
    {
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

        // 設定第一條影片 URL
        videoPlayer.Stop();
        videoPlayer.url = firstVideoURL;

        // 註冊播完事件
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        // Prepare + Play
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;

        // 只對「第一條影片」做 stopTimes
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

                    // 第一次停點先顯示提示，之後唔再顯示手指
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
                if (!hasShownHintOnce)
                {
                    hasShownHintOnce = true;
                }

                HideFinger();
                SetTextHintVisible(false);

                stopIndex++;
                videoPlayer.Play();
            }
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // 只處理第一條影片播完
        if (vp.url != firstVideoURL) return;

        // 如果仲未完成所有停點（例如你跳過/未 swipe 完），唔切片
        if (stopIndex < stopTimes.Count) return;

        StartCoroutine(PlayNextVideo_StressOverload());
    }

    IEnumerator PlayNextVideo_StressOverload()
    {
        // 重置狀態（第二條片暫時唔做停點互動）
        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        // 換 URL -> 第二條影片
        videoPlayer.Stop();
        videoPlayer.url = nextVideoURL;

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

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }
}