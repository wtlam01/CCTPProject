using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Chapter1DailyHubController : MonoBehaviour
{
    [Header("Scene Names")]
    public string chapter1SceneName = "Chapter1";
    public string chapter1twoSceneName = "Chapter1two";
    public string chapter2SceneName = "Chapter2"; // after result -> Chapter2

    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;
    public RawImage videoRawImage;

    [Header("BG (show when video hidden)")]
    public GameObject bgImageObject;

    [Header("Hub UI (CanvasGroups on each option)")]
    public CanvasGroup chatOptionGroup;
    public CanvasGroup studyOptionGroup;
    public CanvasGroup coffeeOptionGroup;
    public Button chatButton;
    public Button studyButton;
    public Button coffeeButton;

    [Header("System Overlay (optional)")]
    public CanvasGroup blackoutGroup;
    public float blackoutFadeIn = 0.35f;
    public float blackoutHold = 0.7f;
    public float blackoutFadeOut = 0.35f;

    [Header("URLs")]
    public string studyVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/2Studying.mp4";
    public string restVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/23Resting.mp4";
    public string overworkURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/21Fire.mp4";
    public string examURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/24Exam.mp4";
    public string successURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/25academicsuccess.mp4";
    public string failureURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/26Failure.mp4";
    public string chatVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/231Chatwithfriend.mp4";

    [Header("Study: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;
    public float maxPressesPerSecond = 8f;
    public float maxPlaybackSpeed = 5f;
    public float speedSmoothing = 10f;
    public float stopAfterNoPressSeconds = 0.25f;
    public float minPlaybackSpeed = 0f;
    public float endPadding = 0.05f;

    [Header("Space Hint (press demo)")]
    public RectTransform spaceHintRect;
    public CanvasGroup spaceHintGroup;
    public float hintShowDelay = 0.25f;

    [Header("Space Hint Animation")]
    public float pressDownScale = 0.88f;
    public float pressDownTime = 0.10f;
    public float releaseTime = 0.14f;
    public float pressPause = 0.70f;
    public float loopDelay = 0.50f;

    [Header("Hint Rule")]
    [Tooltip("只係第一次播放 Study 先需要 Hint")]
    public bool studyHintOnlyFirstTime = true;

    [Tooltip("第一次 Study 時：玩家按幾多次 Space 先收埋 Hint")]
    public int hintHideAfterPresses = 3;

    [Header("Rest: swipe stops (first time only)")]
    public double restStop1 = 6.0;
    public double restStop2 = 10.0;

    [Header("Rest: second time plays only this segment")]
    public double restRepeatStart = 0.0;
    public double restRepeatEnd = 4.0;

    [Header("Swipe Hint (Rest stops)")]
    public RectTransform swipePos6s;
    public RectTransform swipePos10s;
    public SwipeHintAnimator_Chapter1 swipeAnim;
    public float swipeMinDistance = 120f;
    public float swipeMaxTime = 0.6f;

    // ===================== NEW RESULT SYSTEM =====================
    [Header("Hidden System")]
    public int day = 1;

    [Tooltip("Count how many times player chose Study")]
    public int studyCount = 0;

    [Tooltip("Streak for overwork trigger")]
    public int studyStreak = 0;

    [Header("Choice System")]
    public int totalChoices = 0;
    public int maxChoices = 7;

    [Header("Day pacing")]
    public int dayPerChoice = 2;

    [Header("Balanced Success Rule")]
    public int successStudyMin = 4; // pass if studyCount == 4 or 5
    public int successStudyMax = 5;

    // ===================== OVERWORK EFFECT =====================
    [Header("Overwork (trigger by streak)")]
    public int overworkTriggerStreak = 3;

    [Header("Overwork: wipe-to-clean overlay")]
    public WipeToClearOverlay wipeOverlay;
    public float orangeTriggerLastSeconds = 2.0f;
    [Range(0.1f, 0.99f)] public float nearlyCleanThreshold = 0.85f;

    // ---------------- Runtime State ----------------
    bool isPlaying = false;
    bool coffeeUnlocked = false;
    int restTimesChosen = 0;

    bool chatLockedAfterStudy = false;
    bool studyHintAlreadyShown = false;

    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;
    Coroutine spaceHintCo;

    bool waitingSwipe = false;
    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swipeTriggered = false;

    bool overworkPending = false;

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

        if (videoRawImage != null) videoRawImage.color = Color.black;

        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);

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

        SetSpaceHintVisible(false);
        StopSpaceHintLoop();

        if (swipeAnim != null) swipeAnim.StopAndHide();

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
        }

        if (wipeOverlay != null)
        {
            wipeOverlay.EndWipeHide();
            wipeOverlay.OnFinished -= OnOrangeWipeFinished;
            wipeOverlay.OnFinished += OnOrangeWipeFinished;
        }

        ApplyHubState(showHub: true);
    }

    void Update()
    {
        if (!waitingSwipe) return;

        if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            swipeTriggered = true;
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            swipeStartPos = mouse.position.ReadValue();
            swipeStartTime = Time.unscaledTime;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            Vector2 endPos = mouse.position.ReadValue();
            float dt = Time.unscaledTime - swipeStartTime;
            float dy = endPos.y - swipeStartPos.y;

            if (dt <= swipeMaxTime && dy >= swipeMinDistance)
                swipeTriggered = true;
        }
    }

    // -------------------- UI Actions --------------------
    void OnChatClicked()
    {
        if (isPlaying) return;
        StartCoroutine(ChatThenGoScene());
    }

    void OnStudyClicked()
    {
        if (isPlaying) return;

        chatLockedAfterStudy = true;
        StartCoroutine(StudyDayRoutine());
    }

    void OnRestClicked()
    {
        if (isPlaying) return;
        if (!coffeeUnlocked) return;

        StartCoroutine(RestDayRoutine());
    }

    // -------------------- Chat Flow --------------------
    IEnumerator ChatThenGoScene()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        yield return PlayUrlFull(chatVideoURL);

        isPlaying = false;
        SceneManager.LoadScene(chapter1twoSceneName);
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

        yield return PlayStudyWithSpace(studyVideoURL);

        coffeeUnlocked = true;

        yield return SystemCheckRoutine();
        if (!isPlaying) yield break;

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

        restTimesChosen++;

        if (restTimesChosen == 1)
            yield return PlayRestWithSwipeStops(restVideoURL, restStop1, restStop2);
        else
            yield return PlayUrlSegment(restVideoURL, restRepeatStart, restRepeatEnd);

        yield return SystemCheckRoutine();
        if (!isPlaying) yield break;

        ApplyHubState(showHub: true);
        isPlaying = false;
    }

    // -------------------- System Check --------------------
    IEnumerator SystemCheckRoutine()
    {
        // 固定 7 次 choices 後 exam
        if (totalChoices >= maxChoices)
        {
            yield return PlayUrlFull(examURL);

            bool pass = (studyCount >= successStudyMin && studyCount <= successStudyMax);

            if (pass) yield return PlayUrlFull(successURL);
            else yield return PlayUrlFull(failureURL);

            ApplyHubState(showHub: false);
            isPlaying = false;

            SceneManager.LoadScene(chapter2SceneName);
            yield break;
        }

        // 3 studies in a row -> overwork
        if (studyStreak >= overworkTriggerStreak)
        {
            studyStreak = 0;
            overworkPending = true;

            yield return BlackoutRoutine(true);
            yield return PlayOverworkFireThenWipe(overworkURL);
            yield break;
        }

        yield break;
    }

    // -------------------- Overwork flow --------------------
    IEnumerator PlayOverworkFireThenWipe(string url)
    {
        if (wipeOverlay != null) wipeOverlay.EndWipeHide();

        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        yield return WaitUntilVideoActuallyPlays(2f);

        double len = videoPlayer.length;
        if (len <= 0.01) len = 8.0;

        double showAt = Mathf.Max(0f, (float)len - orangeTriggerLastSeconds);

        while (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.time < showAt)
            yield return null;

        ForceHideAllOptions();

        if (videoPlayer != null) videoPlayer.Pause();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);

        if (wipeOverlay != null)
        {
            wipeOverlay.clearToFinish = nearlyCleanThreshold;
            wipeOverlay.BeginWipe();
        }

        while (wipeOverlay != null && wipeOverlay.gameObject.activeInHierarchy)
            yield return null;
    }

    void OnOrangeWipeFinished()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
        }

        if (overworkPending)
        {
            overworkPending = false;

            // hidden consequence: lose 1 extra choice worth of time
            day += dayPerChoice;
        }

        ApplyHubState(showHub: true);
        isPlaying = false;
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
        }
        else
        {
            yield return FadeCanvasGroup(blackoutGroup, 0f, blackoutFadeOut);
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
        }
    }

    // -------------------- Video Helpers --------------------
    IEnumerator PrepareVideoNoBgFlash(string url)
    {
        if (videoPlayer == null) yield break;

        if (bgImageObject != null) bgImageObject.SetActive(false);
        if (videoRawImageObject != null) videoRawImageObject.SetActive(true);

        if (videoRawImage != null) videoRawImage.color = Color.black;

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.playbackSpeed = 1f;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
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
    }

    IEnumerator PlayUrlFull(string url)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.time = 0;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);

        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
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

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);

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

        double duration = videoPlayer.length;
        if (duration <= 0.01) duration = 12.0;

        float currentSpeed = 0f;

        while (videoPlayer.time < duration - endPadding)
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (pressed)
            {
                pressTimes.Enqueue(Time.unscaledTime);
                lastPressAt = Time.unscaledTime;

                if (shouldShowHintThisPlay)
                {
                    pressCount++;
                    if (pressCount >= hintHideAfterPresses)
                    {
                        StopSpaceHintLoop();
                        SetSpaceHintVisible(false);
                    }
                }
            }

            while (pressTimes.Count > 0 && Time.unscaledTime - pressTimes.Peek() > sampleWindowSeconds)
                pressTimes.Dequeue();

            float aps = (sampleWindowSeconds > 0.0001f) ? (pressTimes.Count / sampleWindowSeconds) : 0f;
            float t = Mathf.Clamp01(aps / maxPressesPerSecond);
            float targetSpeed = Mathf.Lerp(minPlaybackSpeed, maxPlaybackSpeed, t);

            if (Time.unscaledTime - lastPressAt > stopAfterNoPressSeconds)
                targetSpeed = 0f;

            float lerpFactor = 1f - Mathf.Exp(-speedSmoothing * Time.unscaledDeltaTime);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpFactor);

            videoPlayer.playbackSpeed = currentSpeed;
            yield return null;
        }

        videoPlayer.playbackSpeed = 1f;
        StopSpaceHintLoop();
        SetSpaceHintVisible(false);

        if (studyHintOnlyFirstTime)
            studyHintAlreadyShown = true;

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

        videoPlayer.Play();
        yield return PlayUntilTime(stop2);
        yield return WaitForSwipeAtPos(swipePos10s);

        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    IEnumerator PlayUntilTime(double stopTime)
    {
        if (videoPlayer == null) yield break;
        while (videoPlayer.time < stopTime) yield return null;
        videoPlayer.Pause();
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

        while (!swipeTriggered) yield return null;

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

        SetOptionVisible(studyOptionGroup, studyButton, true);
        SetOptionVisible(coffeeOptionGroup, coffeeButton, coffeeUnlocked);
    }

    void ForceHideAllOptions()
    {
        SetOptionVisible(chatOptionGroup, chatButton, false);
        SetOptionVisible(studyOptionGroup, studyButton, false);
        SetOptionVisible(coffeeOptionGroup, coffeeButton, false);
    }

    void SetOptionVisible(CanvasGroup g, Button b, bool visible)
    {
        if (g == null) return;

        g.alpha = visible ? 1f : 0f;
        g.interactable = visible;
        g.blocksRaycasts = visible;

        if (b != null) b.interactable = visible;
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
    }

    void StartSpaceHintLoop()
    {
        if (spaceHintRect == null) return;

        SetSpaceHintVisible(true);

        if (spaceHintCo != null) StopCoroutine(spaceHintCo);
        spaceHintCo = StartCoroutine(SpaceHintLoop());
    }

    void StopSpaceHintLoop()
    {
        if (spaceHintCo != null) StopCoroutine(spaceHintCo);
        spaceHintCo = null;

        if (spaceHintRect != null) spaceHintRect.localScale = Vector3.one;
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

            t = 0f;
            while (t < releaseTime)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(downScale, baseScale, t / releaseTime);
                yield return null;
            }
            spaceHintRect.localScale = baseScale;

            yield return new WaitForSecondsRealtime(pressPause);
            yield return new WaitForSecondsRealtime(loopDelay);
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

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / time));
            yield return null;
        }

        cg.alpha = target;
    }
}