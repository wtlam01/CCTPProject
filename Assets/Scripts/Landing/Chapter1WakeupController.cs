// script 控制 Chapter 1 嘅開場 video flow：
// 一開始：
// 1. play第一條 intro video（firstVideoURL）
// 2. play之後切換到 wakeup video（wakeupVideoURL）
// 3. wakeup video 會喺指定時間段內 loop（loopStart → loopEnd）
// 4. 顯示 bubble hotspot 同 finger hint，等player click
// 5. player click bubble 後，停止 loop，繼續播放 wakeup video 剩餘部分
// 6. 然後播放 choices video（choicesVideoURL）
// 7. 最後顯示 Chapter 1 title overlay（endOverlay）
//
// This script controls the Chapter 1 opening video sequence:
// At the start
// 1. Play the first intro video (firstVideoURL)
// 2. Then switch to the wakeup video (wakeupVideoURL)
// 3. Loop the wakeup video within a defined section (loopStart → loopEnd)
// 4. Show the bubble hotspot and finger hint, waiting for player input
// 5. When the player clicks the bubble, stop looping and continue the rest of the wakeup video
// 6. Then play the choices video (choicesVideoURL)
// 7. Finally, show the Chapter 1 title overlay (endOverlay)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Chapter1WakeupController : MonoBehaviour
{
    [Header("END Overlay (Show at the end)")]
    public GameObject endOverlayObject;     // IntroOverlay
    public CanvasGroup endOverlayGroup;     // CanvasGroup on IntroOverlay
    public float endFadeInDuration = 1.0f;
    public float endHoldTime = 2.0f;
    public bool endFadeOut = false;
    public float endFadeOutDuration = 1.0f;

    [Header("Video Output")]
    public VideoPlayer videoPlayer;         // Chapter1VideoPlayer
    public GameObject videoRawImageObject;  // VideoRawImage (RawImage GO)

    [Header("Video URLs")]
    public string firstVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/1.mp4";
    public string wakeupVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/11wakeup.mp4";
    public string choicesVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/112Choices.mp4";

    [Header("Wakeup Loop (seconds)")]
    public double loopStart = 0.0;
    public double loopEnd = 15.0;

    [Header("Bubble Hotspot")]
    public GameObject bubbleHotspotObject;  // BubbleHotspot (Button GO)
    public Button bubbleButton;             // Button component

    [Header("Finger Hint")]
    public GameObject fingerHintObject;                 // FingerHint (Image)
    public ClickFingerHintAnimator fingerHintAnimator;  // script on FingerHint

    bool inLoop = false;
    bool clicked = false;

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
        }

        // 片一開始就要見到畫面：RawImage 保持開
        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        // End overlay 一開始唔顯示
        if (endOverlayObject != null) endOverlayObject.SetActive(false);
        if (endOverlayGroup != null)
        {
            endOverlayGroup.alpha = 0f;
            endOverlayGroup.blocksRaycasts = false;
            endOverlayGroup.interactable = false;
        }

        SetHotspot(false);
        SetFinger(false);

        if (bubbleButton != null)
        {
            bubbleButton.onClick.RemoveListener(OnBubbleClicked);
            bubbleButton.onClick.AddListener(OnBubbleClicked);
        }
    }

    IEnumerator Start()
    {
        // -------------------------
        // 1) Play first video (1.mp4) to end
        // -------------------------
        yield return PlayUrlAndWaitEnd(firstVideoURL);

        // -------------------------
        // 2) Wakeup video: loop 0-15 until clicked
        // -------------------------
        yield return PlayUrlPreparedOnly(wakeupVideoURL);

        inLoop = true;
        clicked = false;

        SetHotspot(true);
        SetFinger(true);

        videoPlayer.time = loopStart;
        videoPlayer.Play();

        // keep looping the wakeup segment until player clicks
        // this creates a "waiting state" where the system pauses progress until interaction
        while (inLoop && !clicked)
        {
            // if video reaches loop end, reset back to loop start
            if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.time >= loopEnd)
            {
                videoPlayer.time = loopStart;
                videoPlayer.Play();
            }
            yield return null;
        }

        // Clicked → continue from loopEnd
        inLoop = false;
        SetFinger(false);
        SetHotspot(false);

        if (videoPlayer != null)
        {
            videoPlayer.time = loopEnd;
            videoPlayer.Play();
        }

        // wait wakeup end
        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        // -------------------------
        // 3) Choices video
        // -------------------------
        yield return PlayUrlAndWaitEnd(choicesVideoURL);

        // -------------------------
        // 4) Show "CHAPTER 1" overlay at the end
        // -------------------------
        yield return ShowEndOverlay();
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

    IEnumerator ShowEndOverlay()
    {
        if (endOverlayObject == null || endOverlayGroup == null) yield break;

        // 顯示 overlay
        endOverlayObject.SetActive(true);
        endOverlayGroup.blocksRaycasts = true;
        endOverlayGroup.interactable = true;

        // Fade in
        yield return Fade(endOverlayGroup, 0f, 1f, endFadeInDuration);

        // Hold
        yield return new WaitForSecondsRealtime(endHoldTime);

        // Optional fade out
        if (endFadeOut)
        {
            yield return Fade(endOverlayGroup, 1f, 0f, endFadeOutDuration);
            endOverlayGroup.blocksRaycasts = false;
            endOverlayGroup.interactable = false;
            endOverlayObject.SetActive(false);
        }
    }

// 控制 UI 淡入淡出嘅 coroutine（用 CanvasGroup alpha）
// This coroutine fades a CanvasGroup from one alpha value to another over time
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
            t += Time.unscaledDeltaTime; // increase time using unscaled time (works even if game is paused)
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)); 
            // interpolate alpha smoothly from "from" to "to"
            yield return null;  // wait until next frame
        }
        cg.alpha = to;
    }
}