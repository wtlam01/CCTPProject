using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class Chapter1DailyHubController : MonoBehaviour
{
    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject; // RawImage GO (VideoRawImage)

    [Header("BG (show when video hidden)")]
    public GameObject bgImageObject;       // 拖 BG_Image (GameObject)

    [Header("Hub UI (CanvasGroups on each option)")]
    public CanvasGroup chatOptionGroup;     // Option_Chat CanvasGroup (optional)
    public CanvasGroup studyOptionGroup;    // Option_Study CanvasGroup
    public CanvasGroup coffeeOptionGroup;   // Option_Coffee CanvasGroup (Rest)
    public Button chatButton;
    public Button studyButton;
    public Button coffeeButton;

    [Header("System Overlay (optional)")]
    public CanvasGroup blackoutGroup;       // BlackFadeOverlay CanvasGroup (optional)
    public float blackoutFadeIn = 0.35f;
    public float blackoutHold = 0.7f;
    public float blackoutFadeOut = 0.35f;

    [Header("URLs")]
    public string studyVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/2Studying.mp4";
    public string restVideoURL   = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/23Resting.mp4";
    public string overworkURL    = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/21Fire.mp4";
    public string examURL        = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/24Exam.mp4";
    public string successURL     = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/25academicsuccess.mp4";
    public string failureURL     = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/26Failure.mp4";

    [Header("Study: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;
    public float maxPressesPerSecond = 8f;
    public float maxPlaybackSpeed = 5f;
    public float speedSmoothing = 10f;
    public float stopAfterNoPressSeconds = 0.25f;
    public float minPlaybackSpeed = 0f;
    public float endPadding = 0.05f;

    [Header("Space Hint (press demo)")]
    public RectTransform spaceHintRect;     // SpaceHint rect
    public CanvasGroup spaceHintGroup;      // SpaceHint canvas group
    public float hintShowDelay = 0.25f;

    [Header("Space Hint Animation")]
    public float pressDownScale = 0.88f;
    public float pressDownTime = 0.10f;
    public float releaseTime = 0.14f;
    public float pressPause = 0.70f;
    public float loopDelay = 0.50f;

    [Header("Rest: swipe stops (first time only)")]
    public double restStop1 = 6.0;
    public double restStop2 = 10.0;

    [Header("Rest: second time plays only this segment")]
    public double restRepeatStart = 0.0;
    public double restRepeatEnd = 4.0;

    [Header("Swipe Hint (Rest stops)")]
    public RectTransform swipePos6s;             // SwipeHintPos_6s
    public RectTransform swipePos10s;            // SwipeHintPos_10s
    public SwipeHintAnimator_Chapter1 swipeAnim; // FingerHint(Image) 上嘅 SwipeHintAnimator_Chapter1
    public float swipeMinDistance = 120f;
    public float swipeMaxTime = 0.6f;

    [Header("Hidden System (NO countdown shown)")]
    public int day = 1;
    public int progress = 0;
    public int studyStreak = 0;

    [Header("Targets")]
    public int finalDay = 15;
    public int passProgress = 18; // SUCCESS threshold

    [Header("Overwork (trigger by streak)")]
    public int overworkTriggerStreak = 3;   // studyStreak >= 3 triggers
    public int overworkMinSkipDays = 2;     // random skip +2 or +3
    public int overworkMaxSkipDays = 3;
    public int overworkProgressLoss = 2;    // progress -2

    [Header("Overwork wipe-to-clean overlay")]
    public WipeToClearOverlay wipeOverlay; // 拖 OrangeOverlay 上嘅 WipeToClearOverlay component
    public Color orangeColor = new Color(1f, 0.5f, 0f, 1f);
    public float orangeTriggerLastSeconds = 2f; // 火片尾前幾秒開始

    // runtime state
    bool isPlaying = false;
    bool chatLocked = false;
    bool coffeeUnlocked = false;
    int restTimesChosen = 0;

    // study press tracking
    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;

    // hint coroutine
    Coroutine spaceHintCo;

    // swipe state (rest stops)
    bool waitingSwipe = false;
    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swipeTriggered = false;

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

        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);

        if (studyButton != null)
        {
            studyButton.onClick.RemoveListener(OnStudyClicked);
            studyButton.onClick.AddListener(OnStudyClicked);
        }

        if (coffeeButton != null)
        {
            coffeeButton.onClick.RemoveListener(OnRestClicked);
            coffeeButton.onClick.AddListener(OnRestClicked);
        }

        if (chatButton != null)
        {
            chatButton.onClick.RemoveListener(OnChatClicked);
            chatButton.onClick.AddListener(OnChatClicked);
        }

        chatLocked = false;
        coffeeUnlocked = false;
        restTimesChosen = 0;

        // Rest hidden at start
        SetOption(coffeeOptionGroup, coffeeButton, show: false, disableGO: true);
        ApplyHubState(showHub: true);

        // hints
        SetSpaceHintVisible(false);
        StopSpaceHintLoop();
        if (swipeAnim != null) swipeAnim.StopAndHide();

        // blackout
        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
        }
    }

    void Update()
    {
        if (!waitingSwipe) return;

        // backup test
        if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            swipeTriggered = true;
            return;
        }

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            swipeStartPos = Mouse.current.position.ReadValue();
            swipeStartTime = Time.unscaledTime;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 endPos = Mouse.current.position.ReadValue();
            float dt = Time.unscaledTime - swipeStartTime;
            float dy = endPos.y - swipeStartPos.y;

            if (dt <= swipeMaxTime && dy >= swipeMinDistance)
                swipeTriggered = true;
        }
    }

    // -------------------- UI Actions --------------------
    void OnStudyClicked()
    {
        if (isPlaying) return;
        chatLocked = true;
        StartCoroutine(StudyDayRoutine());
    }

    void OnRestClicked()
    {
        if (isPlaying) return;
        if (!coffeeUnlocked) return;

        chatLocked = true;
        StartCoroutine(RestDayRoutine());
    }

    void OnChatClicked()
    {
        if (isPlaying) return;

        // 暫時當 Rest（如果未解鎖 Rest，就當 Study）
        chatLocked = true;
        if (coffeeUnlocked) StartCoroutine(RestDayRoutine());
        else StartCoroutine(StudyDayRoutine());
    }

    // -------------------- Day Routines --------------------
    IEnumerator StudyDayRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        // Hidden rules
        day += 1;
        progress += 2;
        studyStreak += 1;

        // video
        yield return PlayStudyWithSpace(studyVideoURL);

        // unlock Rest after first Study completes
        coffeeUnlocked = true;
        SetOption(coffeeOptionGroup, coffeeButton, show: true, disableGO: false);

        // system check
        yield return SystemCheckRoutine();

        if (!isPlaying) yield break;
        ApplyHubState(showHub: true);
        isPlaying = false;
    }

    IEnumerator RestDayRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        // Hidden rules
        day += 1;
        studyStreak = 0;

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
        // Final => exam + result
        if (day >= finalDay)
        {
            day = finalDay;

            yield return PlayUrlFull(examURL);

            if (progress >= passProgress) yield return PlayUrlFull(successURL);
            else yield return PlayUrlFull(failureURL);

            ApplyHubState(showHub: false);
            isPlaying = false;
            yield break;
        }

        // Overwork by streak
        if (studyStreak >= overworkTriggerStreak)
        {
            yield return BlackoutRoutine(true);

            // Fire video -> last seconds -> wipe-to-clean overlay
            yield return PlayOverworkFireThenWipe(overworkURL);

            // punishment AFTER wipe finished
            int skip = Random.Range(overworkMinSkipDays, overworkMaxSkipDays + 1);
            day += skip;

            progress -= overworkProgressLoss;
            if (progress < 0) progress = 0;

            studyStreak = 0;

            if (day > finalDay) day = finalDay;

            yield return BlackoutRoutine(false);

            if (day >= finalDay)
            {
                yield return SystemCheckRoutine();
                yield break;
            }
        }
    }

    // -------------------- Overwork Fire -> Wipe-to-clean --------------------
    IEnumerator PlayOverworkFireThenWipe(string url)
    {
        // 1) Play fire video
        yield return PrepareVideo(url);
        ShowVideo(true);
        if (bgImageObject != null) bgImageObject.SetActive(false);

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        // 2) Wait until near end
        double len = videoPlayer.length;
        if (len <= 0.01) len = 8.0; // fallback

        double triggerAt = Mathf.Max(0f, (float)len - orangeTriggerLastSeconds);

        while (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.time < triggerAt)
            yield return null;

        // 3) Cut to BG and start wipe (solid orange)
        if (videoPlayer != null) videoPlayer.Pause();
        ShowVideo(false);

        if (bgImageObject != null) bgImageObject.SetActive(true);

        if (wipeOverlay != null)
        {
            wipeOverlay.StartWipe(orangeColor);
            yield return new WaitUntil(() => wipeOverlay.Finished);
        }
        else
        {
            // If you forgot to assign wipeOverlay, just wait a moment so flow doesn't break
            yield return new WaitForSecondsRealtime(0.5f);
        }
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
    IEnumerator PrepareVideo(string url)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.playbackSpeed = 1f;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
    }

    void ShowVideo(bool show)
    {
        if (show)
        {
            if (videoRawImageObject != null) videoRawImageObject.SetActive(true);
        }
        else
        {
            if (videoPlayer != null) videoPlayer.Stop();
            if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        }
    }

    IEnumerator PlayUrlFull(string url)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        yield return PrepareVideo(url);
        ShowVideo(true);
        if (bgImageObject != null) bgImageObject.SetActive(false);

        videoPlayer.time = 0;
        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;

        ShowVideo(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    IEnumerator PlayUrlSegment(string url, double start, double end)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        if (end <= start) yield break;

        yield return PrepareVideo(url);
        ShowVideo(true);
        if (bgImageObject != null) bgImageObject.SetActive(false);

        videoPlayer.time = start;
        videoPlayer.Play();

        while (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.time < end)
            yield return null;

        videoPlayer.Pause();
        ShowVideo(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    // Study video: press space => speed
    IEnumerator PlayStudyWithSpace(string url)
    {
        if (string.IsNullOrEmpty(url) || videoPlayer == null) yield break;

        yield return PrepareVideo(url);
        ShowVideo(true);
        if (bgImageObject != null) bgImageObject.SetActive(false);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();

        yield return new WaitForSecondsRealtime(hintShowDelay);
        StartSpaceHintLoop();

        double duration = videoPlayer.length;
        if (duration <= 0.01) duration = 12.0;

        float currentSpeed = 0f;
        bool firstPress = false;

        while (videoPlayer.time < duration - endPadding)
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (pressed)
            {
                pressTimes.Enqueue(Time.unscaledTime);
                lastPressAt = Time.unscaledTime;

                if (!firstPress)
                {
                    firstPress = true;
                    StopSpaceHintLoop();
                    SetSpaceHintVisible(false);
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

        ShowVideo(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    // Rest first time: stop at 6s/10s and require swipe
    IEnumerator PlayRestWithSwipeStops(string url, double stop1, double stop2)
    {
        if (string.IsNullOrEmpty(url) || videoPlayer == null) yield break;

        yield return PrepareVideo(url);
        ShowVideo(true);
        if (bgImageObject != null) bgImageObject.SetActive(false);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();

        // stop 6s
        yield return PlayUntilTime(stop1);
        yield return WaitForSwipeAtPos(swipePos6s);

        // stop 10s
        videoPlayer.Play();
        yield return PlayUntilTime(stop2);
        yield return WaitForSwipeAtPos(swipePos10s);

        // to end
        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;

        ShowVideo(false);
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

    // -------------------- Hub UI State --------------------
    void ApplyHubState(bool showHub)
    {
        if (!showHub)
        {
            SetOption(chatOptionGroup, chatButton, show: false, disableGO: true);
            SetOption(studyOptionGroup, studyButton, show: false, disableGO: true);
            SetOption(coffeeOptionGroup, coffeeButton, show: false, disableGO: true);
            return;
        }

        if (!chatLocked) SetOption(chatOptionGroup, chatButton, show: true, disableGO: false);
        else SetOption(chatOptionGroup, chatButton, show: false, disableGO: true);

        SetOption(studyOptionGroup, studyButton, show: true, disableGO: false);

        if (coffeeUnlocked) SetOption(coffeeOptionGroup, coffeeButton, show: true, disableGO: false);
        else SetOption(coffeeOptionGroup, coffeeButton, show: false, disableGO: true);
    }

    void SetOption(CanvasGroup g, Button b, bool show, bool disableGO)
    {
        if (g == null) return;

        if (show)
        {
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
        else
        {
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;

            if (disableGO && g.gameObject.activeSelf)
                g.gameObject.SetActive(false);
        }

        if (b != null) b.interactable = show;
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