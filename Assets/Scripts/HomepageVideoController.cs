// script 控制 homepage 嘅 video play flow：
// 一開始（第一次進入 session）：
// 1. 顯示「Click to Begin」overlay
// 2. player click 後開始播放 icon video（播放一次）
// 3. icon 播完後淡入黑幕&切換
// 4. 播放 homepage video（loop）
// 5. 顯示首頁 UI（buttons 等）

// 如果同一個 session 再次返回 homepage：
// 1. 跳過「Click to Begin」overlay
// 2. 直接播放 icon → homepage video flow
//
// This script controls the homepage video sequence:
// On the first visit in a session:
// 1. Show the "Click to Begin" overlay
// 2. Wait for player input, then play the icon video (once)
// 3. Fade to black and switch after the icon finishes
// 4. Play the homepage video (loop)
// 5. Show homepage UI (buttons, etc.)

// If returning to the homepage within the same session:
// 1. Skip the "Click to Begin" overlay
// 2. Start the icon → homepage video sequence directly

using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class HomepageVideoController : MonoBehaviour
// controls the homepage video sequence, shows click to begin overlay first time only per session, then plays icon video then homepage loop
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    // the video player component

    [Header("Click To Begin Overlay")]
    public GameObject clickToBeginOverlay;
    // the panel that says "click to begin", only shows on first visit per session

    [Header("UI (show when homepage starts)")]
    public GameObject[] uiToShowOnHomepage;
    // all UI elements to show after homepage video starts, like buttons

    [Header("Transition Cover (CanvasGroup on a full-screen black Image)")]
    public CanvasGroup blackCoverGroup;
    public float fadeOutToBlack = 0.12f;
    public float fadeInFromBlack = 0.25f;
    // black overlay used to hide transitions between videos

    [Header("URLs")]
    public string iconURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/Icon.mp4";
    public string homepageURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/homepage.mp4";
    // icon plays once at start, homepage loops after

    static bool sessionStarted = false;
    // static so it persists across scene loads but resets on page refresh

    bool hasStarted = false;
    bool isSwitching = false;
    // hasStarted prevents double clicking, isSwitching prevents video switch triggering twice

    void Reset()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        // auto assign in editor when component first added
    }

    void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
        }
        // make sure video doesnt auto play on start

        SetUIVisible(false);
        // hide all UI until homepage video starts

        if (blackCoverGroup != null)
        {
            blackCoverGroup.alpha = 1f;
            blackCoverGroup.blocksRaycasts = false;
            blackCoverGroup.interactable = false;
        }
        // black cover starts fully on to prevent any flash on first frame

        if (sessionStarted)
        {
            hasStarted = true;
            if (clickToBeginOverlay != null)
                clickToBeginOverlay.SetActive(false);
            StartCoroutine(BeginSequence());
            // returning from another scene, skip overlay and play directly
        }
        else
        {
            if (clickToBeginOverlay != null)
                clickToBeginOverlay.SetActive(true);
            // first time this session, show click to begin overlay
        }
    }

    void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        // subscribe to video end event, remove first to avoid duplicates
    }

    void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
        // unsubscribe when disabled to avoid memory leaks
    }

    void Update()
    {
        if (hasStarted) return;
        // stop checking input once started

        bool pressed = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pressed = true;
        // check mouse click

        if (!pressed && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            pressed = true;
        // check space bar as alternative

        if (!pressed && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            pressed = true;
        // check touch for mobile

        if (pressed)
            StartExperience();
        // any of the above triggers start
    }

    void StartExperience()
    {
        if (hasStarted) return;
        hasStarted = true;
        sessionStarted = true;
        // mark session as started so returning to homepage skips overlay

        if (clickToBeginOverlay != null)
            clickToBeginOverlay.SetActive(false);
        // hide the click to begin panel

        StartCoroutine(BeginSequence());
        // start the video sequence
    }

    IEnumerator BeginSequence()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[HomepageVideoController] VideoPlayer not assigned.");
            yield break;
        }

        yield return PlayURL(iconURL, false);
        // prepare and play icon video first

        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, 1f, 0f, fadeInFromBlack);
        // fade out black to reveal icon video after it starts playing
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (isSwitching) return;

        if (vp.url == iconURL)
            StartCoroutine(SwitchToHomepage_NoFlash());
        // when icon ends, switch to homepage loop
    }

    IEnumerator SwitchToHomepage_NoFlash()
    {
        isSwitching = true;

        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, blackCoverGroup.alpha, 1f, fadeOutToBlack);
        // quickly fade to black to hide last frame of icon before switching

        yield return PlayURL(homepageURL, true);
        // load and play homepage video on loop

        SetUIVisible(true);
        // show homepage UI now that homepage video is playing

        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, 1f, 0f, fadeInFromBlack);
        // fade black out to reveal homepage

        isSwitching = false;
    }

    IEnumerator PlayURL(string url, bool loop)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.isLooping = loop;
        videoPlayer.url = url;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;
        // wait until video is fully prepared before playing

        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    void SetUIVisible(bool visible)
    {
        if (uiToShowOnHomepage == null) return;

        foreach (var go in uiToShowOnHomepage)
        {
            if (go != null)
                go.SetActive(visible);
        }
        // loop through all UI objects and show or hide them
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }
        // snap instantly if duration is near zero

        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        cg.alpha = to;
        // reusable fade coroutine, lerps alpha over time
    }
}