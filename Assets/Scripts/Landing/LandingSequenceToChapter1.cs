// Control Landing scene嘅完整開場流程，依次播放intro video、wakeup loop video
// 等玩家click bubble之後繼續播choices video，最後顯示Chapter 1 title再跳去下一個scene
// This script controls the full landing sequence, playing videos in order with a looping wakeup section
// that waits for the player to click a bubble hotspot before continuing to the choices video and chapter title.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
// scene management to load next scene at the end

public class LandingSequenceToChapter1 : MonoBehaviour
// controls the full landing sequence, plays videos in order, handles the wakeup loop, then loads chapter 1
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;

    [Header("URLs")]
    public string firstVideoURL   = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/1.mp4";
    public string wakeupVideoURL  = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/11wakeup.mp4";
    public string choicesVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/112Choices.mp4";
    // three videos in order, wakeup is the one that loops until player clicks

    [Header("Wakeup Loop (seconds)")]
    public double loopStart = 0.0;
    public double loopEnd   = 15.0;
    // manually loop between these timestamps instead of using unity built in loop

    [Header("Bubble Hotspot")]
    public GameObject bubbleHotspotObject;
    public Button bubbleButton;
    // clickable hotspot that appears during wakeup loop

    [Header("Finger Hint")]
    public GameObject fingerHintObject;
    public ClickFingerHintAnimator fingerHintAnimator;
    // animated finger hint showing player where to click

    [Header("CHAPTER 1 Overlay")]
    public GameObject chapterTitleOverlayObject;
    public CanvasGroup chapterTitleOverlayGroup;
    public float titleFadeInDuration = 0.8f;
    public float titleHoldDuration   = 1.2f;
    public float titleFadeOutDuration = 1.2f;
    // chapter title fades in, holds, then fades out before scene loads

    [Header("Next Scene")]
    public string nextSceneName = "Chapter1";
    // scene name to load after sequence finishes

    bool inLoop = false;
    bool clicked = false;
    bool titleShown = false;
    // state flags to control the sequence flow

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
        }
        // make sure video doesnt auto play

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        SetHotspot(false);
        SetFinger(false);
        // hide hotspot and finger hint at start

        if (bubbleButton != null)
        {
            bubbleButton.onClick.RemoveListener(OnBubbleClicked);
            bubbleButton.onClick.AddListener(OnBubbleClicked);
        }
        // add click listener to bubble, remove first to avoid duplicates

        if (chapterTitleOverlayObject != null) chapterTitleOverlayObject.SetActive(true);
        if (chapterTitleOverlayGroup != null)
        {
            chapterTitleOverlayGroup.alpha = 0f;
            chapterTitleOverlayGroup.blocksRaycasts = false;
            chapterTitleOverlayGroup.interactable = false;
        }
        // title overlay starts invisible, no flash

        if (chapterTitleOverlayObject != null)
        {
            var anim = chapterTitleOverlayObject.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
        }
        // disable animator if there is one, we handling the fade manually
    }

    IEnumerator Start()
    {
        yield return PlayUrlAndWaitEnd(firstVideoURL);
        // play opening video and wait for it to finish

        yield return PlayUrlPreparedOnly(wakeupVideoURL);
        // prepare wakeup video but dont wait for it to end, we controlling the loop manually

        inLoop = true;
        clicked = false;

        SetHotspot(true);
        SetFinger(true);
        // show bubble and finger hint during the loop

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
        // manually loop back to loopStart when we hit loopEnd, exits when player clicks

        inLoop = false;
        SetFinger(false);
        SetHotspot(false);
        // hide hints once clicked

        if (videoPlayer != null)
        {
            videoPlayer.time = loopEnd;
            videoPlayer.Play();
        }

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;
        // play the rest of wakeup video from loopEnd to the actual end

        yield return PlayUrlAndWaitEnd(choicesVideoURL);
        // play choices video and wait for it to finish

        yield return ShowTitleAndTurnOffVideo();
        // show chapter 1 title then hide video

        SceneManager.LoadScene(nextSceneName);
        // load chapter 1 scene
    }

    public void OnBubbleClicked()
    {
        if (!inLoop) return;
        clicked = true;
        // only register click if we are currently in the loop
    }

    void SetHotspot(bool show)
    {
        if (bubbleHotspotObject != null) bubbleHotspotObject.SetActive(show);
        if (bubbleButton != null) bubbleButton.interactable = show;
        // show or hide the clickable bubble hotspot
    }

    void SetFinger(bool show)
    {
        if (fingerHintObject != null) fingerHintObject.SetActive(show);
        if (fingerHintAnimator != null) fingerHintAnimator.enabled = show;
        // show or hide the finger hint animator
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
        // prepare and start playing but dont wait for end, caller decides when to stop
    }

    IEnumerator PlayUrlAndWaitEnd(string url)
    {
        yield return PlayUrlPreparedOnly(url);
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;
        // same as above but waits until video fully finishes
    }

    IEnumerator ShowTitleAndTurnOffVideo()
    {
        if (titleShown) yield break;
        titleShown = true;
        // make sure this only runs once

        if (chapterTitleOverlayGroup == null)
            yield break;

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        // stop and hide video completely before showing title, avoids last frame flash

        chapterTitleOverlayGroup.alpha = 0f;

        yield return Fade(chapterTitleOverlayGroup, 0f, 1f, titleFadeInDuration);
        yield return new WaitForSecondsRealtime(titleHoldDuration);
        yield return Fade(chapterTitleOverlayGroup, 1f, 0f, titleFadeOutDuration);
        // fade in, hold, fade out the chapter title
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
        // snap instantly if duration is basically zero

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        cg.alpha = to;
        // reusable fade coroutine, same pattern used across all scripts
    }
}