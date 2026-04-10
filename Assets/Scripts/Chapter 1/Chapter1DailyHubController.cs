// Chapter 1嘅主要hub控制器，負責處理玩家嘅三個選擇（study, rest, chat）
// 播放對應嘅video，追蹤選擇次數，並且喺7次選擇之後觸發考試同判斷pass定fail (Exam video)
// 讀書3次連續會觸發overwork事件，玩家需要完成wipe動畫先可以繼續。
// This script is the main hub controller for Chapter 1, handling three player choices (study, rest, chat),
// playing the corresponding videos, tracking choice counts, and triggering the exam after 7 total choices.
// Studying 3 times in a row triggers an overwork event with a wipe-to-clear overlay mechanic.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
// lot of imports bc this script does a lot of different things

public class Chapter1DailyHubController : MonoBehaviour
// this is the main hub controller for chapter 1, handles all the choices, videos, scoring and scene transitions
{
    [Header("Scene Names")]
    public string chapter1SceneName = "Chapter1";
    public string chapter1twoSceneName = "Chapter1two";
    public string chapter2SceneName = "Chapter2";
    // scene names to load depending on what happens

    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;
    public RawImage videoRawImage;
    // the video player and the raw image that displays it on screen

    [Header("BG (show when video hidden)")]
    public GameObject bgImageObject;
    // background image shown when no video is playing, swap between this and video

    [Header("Hub UI (CanvasGroups on each option)")]
    public CanvasGroup chatOptionGroup;
    public CanvasGroup studyOptionGroup;
    public CanvasGroup coffeeOptionGroup;
    public Button chatButton;
    public Button studyButton;
    public Button coffeeButton;
    // three choice buttons, each with their own canvas group for showing and hiding

    [Header("System Overlay (optional)")]
    public CanvasGroup blackoutGroup;
    public float blackoutFadeIn = 0.35f;
    public float blackoutHold = 0.7f;
    public float blackoutFadeOut = 0.35f;
    // black overlay used for dramatic transitions, fades in holds then fades out

    [Header("URLs")]
    public string studyVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/2Studying.mp4";
    public string restVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/23Resting.mp4";
    public string overworkURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/21Fire.mp4";
    public string examURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/24Exam.mp4";
    public string successURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/25academicsuccess.mp4";
    public string failureURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/26Failure.mp4";
    public string chatVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/231Chatwithfriend.mp4";
    // all the video urls, different videos play depending on what player chooses

    [Header("Study: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;
    public float maxPressesPerSecond = 8f;
    public float maxPlaybackSpeed = 5f;
    public float speedSmoothing = 10f;
    public float stopAfterNoPressSeconds = 0.25f;
    public float minPlaybackSpeed = 0f;
    public float endPadding = 0.05f;
    // study mechanic settings, player mashes space to control video playback speed

    [Header("Space Hint (press demo)")]
    public RectTransform spaceHintRect;
    public CanvasGroup spaceHintGroup;
    public float hintShowDelay = 0.25f;
    // the space bar hint UI that shows player what to do

    [Header("Space Hint Animation")]
    public float pressDownScale = 0.88f;
    public float pressDownTime = 0.10f;
    public float releaseTime = 0.14f;
    public float pressPause = 0.70f;
    public float loopDelay = 0.50f;
    // animation values for the space key press demo, same pattern as KeyboardHintUI

    [Header("Hint Rule")]
    [Tooltip("只係第一次播放 Study 先需要 Hint")]
    public bool studyHintOnlyFirstTime = true;
    [Tooltip("第一次 Study 時：玩家按幾多次 Space 先收埋 Hint")]
    public int hintHideAfterPresses = 3;
    // hint only shows first time player studies, hides after they press space a few times

    [Header("Rest: swipe stops (first time only)")]
    public double restStop1 = 6.0;
    public double restStop2 = 10.0;
    // rest video pauses at these timestamps waiting for player to swipe, first time only

    [Header("Rest: second time plays only this segment")]
    public double restRepeatStart = 0.0;
    public double restRepeatEnd = 4.0;
    // second time player rests, only plays a short segment instead of full video

    [Header("Swipe Hint (Rest stops)")]
    public RectTransform swipePos6s;
    public RectTransform swipePos10s;
    public SwipeHintAnimator_Chapter1 swipeAnim;
    public float swipeMinDistance = 120f;
    public float swipeMaxTime = 0.6f;
    // swipe hint positions and settings for the rest video stops

    [Header("Hidden System")]
    public int day = 1;
    [Tooltip("Count how many times player chose Study")]
    public int studyCount = 0;
    [Tooltip("Streak for overwork trigger")]
    public int studyStreak = 0;
    // hidden progress tracking, player doesnt see these directly

    [Header("Choice System")]
    public int totalChoices = 0;
    public int maxChoices = 7;
    // game ends after 7 total choices, triggers exam

    [Header("Day pacing")]
    public int dayPerChoice = 2;
    // each choice advances day counter by 2

    [Header("Balanced Success Rule")]
    public int successStudyMin = 4;
    public int successStudyMax = 5;
    // kept for reference but pass condition is now checked by exact combination below

    [Header("Overwork (trigger by streak)")]
    public int overworkTriggerStreak = 3;
    // studying 3 times in a row triggers overwork event

    [Header("Overwork: wipe-to-clean overlay")]
    public WipeToClearOverlay wipeOverlay;
    public float orangeTriggerLastSeconds = 2.0f;
    [Range(0.1f, 0.99f)] public float nearlyCleanThreshold = 0.85f;
    // overwork uses a wipe effect, triggers near end of fire video

    // ---------------- Runtime State ----------------
    bool isPlaying = false;
    bool coffeeUnlocked = false;
    int restTimesChosen = 0;
    // isPlaying prevents multiple choices at once, coffee unlocks after first study

    bool chatLockedAfterStudy = false;
    bool studyHintAlreadyShown = false;
    // chat gets locked once player studies, hint only shows once

    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;
    Coroutine spaceHintCo;
    // queue stores recent press timestamps to calculate presses per second

    bool waitingSwipe = false;
    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swipeTriggered = false;
    // swipe state variables for rest video stops

    bool overworkPending = false;
    // flag to apply overwork penalty after wipe animation finishes

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.playbackSpeed = 1f;
            videoPlayer.Stop();
        }
        // make sure video doesnt auto play and starts clean

        if (videoRawImage != null) videoRawImage.color = Color.black;
        // set raw image to black so no flash before video loads

        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
        // start with bg visible, video hidden

        if (studyButton != null)
        {
            studyButton.onClick.RemoveAllListeners();
            studyButton.onClick.AddListener(OnStudyClicked);
        }

        if (coffeeButton != null)
        {
            coffeeButton.onClick.RemoveAllListeners();
            coffeeButton.onClick.AddListener(OnRestClicked);
        }

        if (chatButton != null)
        {
            chatButton.onClick.RemoveAllListeners();
            chatButton.onClick.AddListener(OnChatClicked);
        }
        // add button listeners, remove first to avoid duplicates

        SetSpaceHintVisible(false);
        StopSpaceHintLoop();
        // hide space hint at start

        if (swipeAnim != null) swipeAnim.StopAndHide();
        // hide swipe hint at start

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
        }
        // blackout starts fully transparent

        if (wipeOverlay != null)
        {
            wipeOverlay.EndWipeHide();
            wipeOverlay.OnFinished -= OnOrangeWipeFinished;
            wipeOverlay.OnFinished += OnOrangeWipeFinished;
        }
        // subscribe to wipe finished event, remove first to avoid duplicates

        ApplyHubState(showHub: true);
        // show hub choices on awake
    }

    void Update()
    {
        if (!waitingSwipe) return;
        // only check swipe input when we actually waiting for one

        if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            swipeTriggered = true;
            return;
        }
        // allow keyboard up arrow as alternative to mouse swipe

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            swipeStartPos = mouse.position.ReadValue();
            swipeStartTime = Time.unscaledTime;
        }
        // record where mouse press started

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            Vector2 endPos = mouse.position.ReadValue();
            float dt = Time.unscaledTime - swipeStartTime;
            float dy = endPos.y - swipeStartPos.y;

            if (dt <= swipeMaxTime && dy >= swipeMinDistance)
                swipeTriggered = true;
        }
        // check if release was fast enough and far enough upward to count as swipe
    }

    // -------------------- UI Actions --------------------
    void OnChatClicked()
    {
        if (isPlaying) return;
        StartCoroutine(ChatThenGoScene());
        // only allow click if nothing currently playing
    }

    void OnStudyClicked()
    {
        if (isPlaying) return;

        chatLockedAfterStudy = true;
        StartCoroutine(StudyDayRoutine());
        // lock chat after first study choice
    }

    void OnRestClicked()
    {
        if (isPlaying) return;
        if (!coffeeUnlocked) return;

        StartCoroutine(RestDayRoutine());
        // rest only available after player has studied at least once
    }

    // -------------------- Chat Flow --------------------
    IEnumerator ChatThenGoScene()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);
        // hide hub while video plays

        yield return PlayUrlFull(chatVideoURL);
        // play chat video and wait for it to finish

        isPlaying = false;
        SceneManager.LoadScene(chapter1twoSceneName);
        // go to mini game scene after chat
    }

    // -------------------- Day Routines --------------------
    IEnumerator StudyDayRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        day += dayPerChoice;
        studyCount += 1;
        studyStreak += 1;
        totalChoices += 1;
        // increment all the relevant counters

        yield return PlayStudyWithSpace(studyVideoURL);
        // play study video with space mash mechanic

        coffeeUnlocked = true;
        // unlock rest option after studying

        yield return SystemCheckRoutine();
        if (!isPlaying) yield break;
        // check if overwork or exam should trigger, exit if something took over

        ApplyHubState(showHub: true);
        isPlaying = false;
    }

    IEnumerator RestDayRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        day += dayPerChoice;
        studyStreak = 0;
        totalChoices += 1;
        // resting resets the study streak

        restTimesChosen++;

        if (restTimesChosen == 1)
            yield return PlayRestWithSwipeStops(restVideoURL, restStop1, restStop2);
        else
            yield return PlayUrlSegment(restVideoURL, restRepeatStart, restRepeatEnd);
        // first rest plays full video with swipe stops, after that just short clip

        yield return SystemCheckRoutine();
        if (!isPlaying) yield break;

        ApplyHubState(showHub: true);
        isPlaying = false;
    }

    // -------------------- System Check --------------------
    IEnumerator SystemCheckRoutine()
    {
        if (totalChoices >= maxChoices)
        {
            yield return PlayUrlFull(examURL);
            // play exam video after 7 choices

            bool pass = (studyCount == 4 && restTimesChosen == 3) ||
                        (studyCount == 5 && restTimesChosen == 2);
            // only these two exact combos pass: 4 study + 3 coffee, or 5 study + 2 coffee

            if (pass) yield return PlayUrlFull(successURL);
            else yield return PlayUrlFull(failureURL);
            // play result video based on pass or fail

            ApplyHubState(showHub: false);
            isPlaying = false;

            SceneManager.LoadScene(chapter2SceneName);
            yield break;
        }

        if (studyStreak >= overworkTriggerStreak)
        {
            studyStreak = 0;
            overworkPending = true;
            // reset streak and flag overwork

            yield return BlackoutRoutine(true);
            yield return PlayOverworkFireThenWipe(overworkURL);
            // blackout then play fire video with wipe effect
            yield break;
        }

        yield break;
    }

    // -------------------- Overwork flow --------------------
    IEnumerator PlayOverworkFireThenWipe(string url)
    {
        if (wipeOverlay != null) wipeOverlay.EndWipeHide();
        // make sure wipe overlay is hidden before starting

        yield return PrepareVideoNoBgFlash(url);
        // load video without showing bg flash

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        yield return WaitUntilVideoActuallyPlays(2f);
        // wait until video actually starts playing

        double len = videoPlayer.length;
        if (len <= 0.01) len = 8.0;
        // fallback length if video length not loaded yet

        double showAt = Mathf.Max(0f, (float)len - orangeTriggerLastSeconds);

        while (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.time < showAt)
            yield return null;
        // wait until near the end of fire video before triggering wipe

        ForceHideAllOptions();

        if (videoPlayer != null) videoPlayer.Pause();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
        // pause video and swap back to bg before wipe starts

        if (wipeOverlay != null)
        {
            wipeOverlay.clearToFinish = nearlyCleanThreshold;
            wipeOverlay.BeginWipe();
        }
        // start the wipe to clean effect

        while (wipeOverlay != null && wipeOverlay.gameObject.activeInHierarchy)
            yield return null;
        // wait until wipe overlay hides itself when done
    }

    void OnOrangeWipeFinished()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
        // clean up video after wipe finishes

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
        }
        // make sure blackout is cleared

        if (overworkPending)
        {
            overworkPending = false;
            day += dayPerChoice;
        }
        // overwork penalty, lose extra days as hidden consequence

        ApplyHubState(showHub: true);
        isPlaying = false;
        // return to hub after overwork sequence done
    }

    // -------------------- Blackout --------------------
    IEnumerator BlackoutRoutine(bool fadeIn)
    {
        if (blackoutGroup == null) yield break;

        if (fadeIn)
        {
            blackoutGroup.blocksRaycasts = true;
            blackoutGroup.interactable = true;
            yield return FadeCanvasGroup(blackoutGroup, 1f, blackoutFadeIn);
            yield return new WaitForSecondsRealtime(blackoutHold);
            // fade in and hold
        }
        else
        {
            yield return FadeCanvasGroup(blackoutGroup, 0f, blackoutFadeOut);
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
            // fade out and disable raycasts
        }
    }

    // -------------------- Video Helpers --------------------
    IEnumerator PrepareVideoNoBgFlash(string url)
    {
        if (videoPlayer == null) yield break;

        if (bgImageObject != null) bgImageObject.SetActive(false);
        if (videoRawImageObject != null) videoRawImageObject.SetActive(true);
        // swap to video display before loading

        if (videoRawImage != null) videoRawImage.color = Color.black;
        // keep it black until video is ready, avoids flash

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.playbackSpeed = 1f;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        // wait until fully prepared before returning
    }

    IEnumerator WaitUntilVideoActuallyPlays(float timeoutSeconds)
    {
        if (videoPlayer == null) yield break;

        double t0 = videoPlayer.time;
        float t = timeoutSeconds;

        while (t > 0f)
        {
            if (videoPlayer.time > t0 + 0.01) yield break;
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
        // wait until video time actually moves, confirms its playing. timeout after set seconds
    }

    IEnumerator PlayUrlFull(string url)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.time = 0;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);

        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;
        // wait for video to fully finish

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
        // clean up and swap back to bg after video ends
    }

    IEnumerator PlayUrlSegment(string url, double start, double end)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        if (end <= start) yield break;

        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.time = start;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);

        while (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.time < end)
            yield return null;
        // only play between start and end timestamps

        videoPlayer.Pause();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    // -------------------- Study --------------------
    IEnumerator PlayStudyWithSpace(string url)
    {
        if (string.IsNullOrEmpty(url) || videoPlayer == null) yield break;

        yield return PrepareVideoNoBgFlash(url);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;

        int pressCount = 0;

        bool shouldShowHintThisPlay = true;
        if (studyHintOnlyFirstTime && studyHintAlreadyShown)
            shouldShowHintThisPlay = false;
        // dont show hint if already shown before

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);
        // start video at speed 0, player controls speed by pressing space

        if (shouldShowHintThisPlay)
        {
            yield return new WaitForSecondsRealtime(hintShowDelay);
            StartSpaceHintLoop();
        }
        else
        {
            StopSpaceHintLoop();
            SetSpaceHintVisible(false);
        }
        // show or hide space hint depending on if already shown

        double duration = videoPlayer.length;
        if (duration <= 0.01) duration = 12.0;
        // fallback duration

        float currentSpeed = 0f;

        while (videoPlayer.time < duration - endPadding)
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (pressed)
            {
                pressTimes.Enqueue(Time.unscaledTime);
                lastPressAt = Time.unscaledTime;
                // record each space press with its timestamp

                if (shouldShowHintThisPlay)
                {
                    pressCount++;
                    if (pressCount >= hintHideAfterPresses)
                    {
                        StopSpaceHintLoop();
                        SetSpaceHintVisible(false);
                    }
                }
                // hide hint after player presses enough times
            }

            while (pressTimes.Count > 0 && Time.unscaledTime - pressTimes.Peek() > sampleWindowSeconds)
                pressTimes.Dequeue();
            // remove press times older than sample window

            float aps = (sampleWindowSeconds > 0.0001f) ? (pressTimes.Count / sampleWindowSeconds) : 0f;
            float t = Mathf.Clamp01(aps / maxPressesPerSecond);
            float targetSpeed = Mathf.Lerp(minPlaybackSpeed, maxPlaybackSpeed, t);
            // calculate target speed based on how fast player is pressing

            if (Time.unscaledTime - lastPressAt > stopAfterNoPressSeconds)
                targetSpeed = 0f;
            // if player stops pressing, video slows to 0

            float lerpFactor = 1f - Mathf.Exp(-speedSmoothing * Time.unscaledDeltaTime);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpFactor);
            // smoothly interpolate speed so it doesnt jump instantly

            videoPlayer.playbackSpeed = currentSpeed;
            yield return null;
        }

        videoPlayer.playbackSpeed = 1f;
        StopSpaceHintLoop();
        SetSpaceHintVisible(false);
        // reset speed and hide hint when video finishes

        if (studyHintOnlyFirstTime)
            studyHintAlreadyShown = true;
        // mark hint as shown so it doesnt appear next time

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    // -------------------- Rest --------------------
    IEnumerator PlayRestWithSwipeStops(string url, double stop1, double stop2)
    {
        if (string.IsNullOrEmpty(url) || videoPlayer == null) yield break;

        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);

        yield return PlayUntilTime(stop1);
        yield return WaitForSwipeAtPos(swipePos6s);
        // pause at first stop, wait for swipe at position 6s

        videoPlayer.Play();
        yield return PlayUntilTime(stop2);
        yield return WaitForSwipeAtPos(swipePos10s);
        // continue to second stop, wait for swipe again

        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;
        // play rest of video to end

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    IEnumerator PlayUntilTime(double stopTime)
    {
        if (videoPlayer == null) yield break;
        while (videoPlayer.time < stopTime) yield return null;
        videoPlayer.Pause();
        // waits each frame until video reaches stop time then pauses
    }

    IEnumerator WaitForSwipeAtPos(RectTransform pos)
    {
        swipeTriggered = false;
        waitingSwipe = true;

        if (videoPlayer != null) videoPlayer.Pause();

        if (swipeAnim != null)
        {
            if (pos != null) swipeAnim.SetBaseFrom(pos);
            swipeAnim.ShowAndPlay();
        }
        // show swipe hint at the given position

        while (!swipeTriggered) yield return null;
        // wait until Update detects a valid swipe

        waitingSwipe = false;

        if (swipeAnim != null) swipeAnim.StopAndHide();
        yield return null;
    }

    // -------------------- Hub UI --------------------
    void ApplyHubState(bool showHub)
    {
        if (!showHub)
        {
            ForceHideAllOptions();
            return;
        }

        if (chatOptionGroup != null) chatOptionGroup.gameObject.SetActive(true);
        if (studyOptionGroup != null) studyOptionGroup.gameObject.SetActive(true);
        if (coffeeOptionGroup != null) coffeeOptionGroup.gameObject.SetActive(true);

        bool showChat = !chatLockedAfterStudy;
        SetOptionVisible(chatOptionGroup, chatButton, showChat);
        // chat only visible if player hasnt studied yet

        SetOptionVisible(studyOptionGroup, studyButton, true);
        SetOptionVisible(coffeeOptionGroup, coffeeButton, coffeeUnlocked);
        // study always visible, coffee only after first study
    }

    void ForceHideAllOptions()
    {
        SetOptionVisible(chatOptionGroup, chatButton, false);
        SetOptionVisible(studyOptionGroup, studyButton, false);
        SetOptionVisible(coffeeOptionGroup, coffeeButton, false);
        // hide all three choices at once
    }

    void SetOptionVisible(CanvasGroup g, Button b, bool visible)
    {
        if (g == null) return;

        g.alpha = visible ? 1f : 0f;
        g.interactable = visible;
        g.blocksRaycasts = visible;

        if (b != null) b.interactable = visible;
        // use canvas group to show or hide, also disable button interactability
    }

    // -------------------- Space Hint --------------------
    void SetSpaceHintVisible(bool show)
    {
        if (spaceHintRect != null) spaceHintRect.gameObject.SetActive(show);

        if (spaceHintGroup != null)
        {
            spaceHintGroup.alpha = show ? 1f : 0f;
            spaceHintGroup.blocksRaycasts = false;
            spaceHintGroup.interactable = false;
        }
        // toggle visibility without blocking raycasts
    }

    void StartSpaceHintLoop()
    {
        if (spaceHintRect == null) return;

        SetSpaceHintVisible(true);

        if (spaceHintCo != null) StopCoroutine(spaceHintCo);
        spaceHintCo = StartCoroutine(SpaceHintLoop());
        // stop old loop if running and start fresh
    }

    void StopSpaceHintLoop()
    {
        if (spaceHintCo != null) StopCoroutine(spaceHintCo);
        spaceHintCo = null;

        if (spaceHintRect != null) spaceHintRect.localScale = Vector3.one;
        // stop and reset scale
    }

    IEnumerator SpaceHintLoop()
    {
        Vector3 baseScale = Vector3.one;
        Vector3 downScale = baseScale * pressDownScale;

        while (true)
        {
            float t = 0f;
            while (t < pressDownTime)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(baseScale, downScale, t / pressDownTime);
                yield return null;
            }
            spaceHintRect.localScale = downScale;
            // press down phase

            t = 0f;
            while (t < releaseTime)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(downScale, baseScale, t / releaseTime);
                yield return null;
            }
            spaceHintRect.localScale = baseScale;
            // release phase, bounce back

            yield return new WaitForSecondsRealtime(pressPause);
            yield return new WaitForSecondsRealtime(loopDelay);
            // pause between each press animation cycle
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float time)
    {
        if (cg == null) yield break;

        float start = cg.alpha;
        if (time <= 0.0001f)
        {
            cg.alpha = target;
            yield break;
        }
        // snap instantly if time is near zero

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / time));
            yield return null;
        }

        cg.alpha = target;
        // reusable fade coroutine used for blackout
    }
}


// Game manager pattern:
// Code Monkey (2021) Code Monkey [YouTube channel]
// https://www.youtube.com/@CodeMonkeyUnity
//
// Video playback implementation:
// Brackeys (2020) Brackeys [YouTube channel]
// https://www.youtube.com/@Brackeys
// Unity Technologies (2023) VideoPlayer API
// https://docs.unity3d.com/ScriptReference/Video.VideoPlayer.html
//
// Scene transition:
// Unity Technologies (2023) SceneManager.LoadScene
// https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html
//
// Coroutine pattern:
// Unity Technologies (2023) Coroutines
// https://docs.unity3d.com/Manual/Coroutines.html
//
// CanvasGroup fade technique:
// Unity Technologies (2023) CanvasGroup
// https://docs.unity3d.com/ScriptReference/CanvasGroup.html
//
// Singleton pattern for game state:
// Nystrom, R. (2014) Game Programming Patterns
// https://gameprogrammingpatterns.com/singleton.html
//
// New Input System:
// Unity Technologies (2023) Input System
// https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html