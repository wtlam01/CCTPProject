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

    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;   // VideoRawImage GO
    public RawImage videoRawImage;           // drag RawImage component here (for black)

    [Header("BG (show when video hidden)")]
    public GameObject bgImageObject;         // BG_Image GO

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
    public RectTransform spaceHintRect;
    public CanvasGroup spaceHintGroup;
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
    public RectTransform swipePos6s;
    public RectTransform swipePos10s;
    public SwipeHintAnimator_Chapter1 swipeAnim;
    public float swipeMinDistance = 120f;
    public float swipeMaxTime = 0.6f;

    [Header("Hidden System (NO countdown shown)")]
    public int day = 1;
    public int progress = 0;
    public int studyStreak = 0;

    [Header("Targets")]
    public int finalDay = 15;
    public int passProgress = 18;

    [Header("Overwork (trigger by streak)")]
    public int overworkTriggerStreak = 3;
    public int overworkMinSkipDays = 2;
    public int overworkMaxSkipDays = 3;
    public int overworkProgressLoss = 2;

    [Header("Overwork: wipe-to-clean overlay")]
    public WipeToClearOverlay wipeOverlay;
    public float orangeTriggerLastSeconds = 2.0f;
    [Range(0.1f, 0.99f)] public float nearlyCleanThreshold = 0.85f;

    // runtime state
    bool isPlaying = false;
    bool coffeeUnlocked = false;
    int restTimesChosen = 0;

    // study press tracking
    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;
    Coroutine spaceHintCo;

    // rest swipe state
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

        // ✅ IMPORTANT: 初始 Hub 要顯示 Chat + Study（Coffee 未解鎖）
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
    void OnChatClicked()
    {
        if (isPlaying) return;

        // ✅ 轉去新 scene 播 chat video
        SceneManager.LoadScene(chapter1twoSceneName);
    }

    void OnStudyClicked()
    {
        if (isPlaying) return;
        StartCoroutine(StudyDayRoutine());
    }

    void OnRestClicked()
    {
        if (isPlaying) return;
        if (!coffeeUnlocked) return;
        StartCoroutine(RestDayRoutine());
    }

    // -------------------- Day Routines --------------------
    IEnumerator StudyDayRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        day += 1;
        progress += 2;
        studyStreak += 1;

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

        if (studyStreak >= overworkTriggerStreak)
        {
            yield return BlackoutRoutine(true);

            yield return PlayOverworkFireThenWipe(overworkURL);

            yield break; // wipe flow ends by event
        }
    }

    // -------------------- Overwork flow --------------------
    IEnumerator PlayOverworkFireThenWipe(string url)
    {
        if (wipeOverlay != null) wipeOverlay.EndWipeHide();

        // no flash while preparing
        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        // wait until time advances (first frame)
        yield return WaitUntilVideoActuallyPlays(2f);

        double len = videoPlayer.length;
        if (len <= 0.01) len = 8.0;

        double showAt = Mathf.Max(0f, (float)len - orangeTriggerLastSeconds);

        while (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.time < showAt)
            yield return null;

        // wipe begins: freeze video, show BG only, hide options
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

        // ✅ wipe 完返 Hub（Chat + Study + Coffee(如已解鎖)）
        ApplyHubState(showHub: true);

        isPlaying = false;

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.interactable = false;
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

    // -------------------- Video Helpers (NO BG FLASH) --------------------
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

    // Study: press space -> speed
    IEnumerator PlayStudyWithSpace(string url)
    {
        if (string.IsNullOrEmpty(url) || videoPlayer == null) yield break;

        yield return PrepareVideoNoBgFlash(url);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();
        yield return WaitUntilVideoActuallyPlays(2f);

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

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (bgImageObject != null) bgImageObject.SetActive(true);
    }

    // Rest: swipe stops
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

        // ✅ Chat + Study always visible
        SetOptionActive(chatOptionGroup, chatButton, true);
        SetOptionActive(studyOptionGroup, studyButton, true);

        // Coffee only when unlocked
        SetOptionActive(coffeeOptionGroup, coffeeButton, coffeeUnlocked);
    }

    void ForceHideAllOptions()
    {
        if (chatOptionGroup != null) chatOptionGroup.gameObject.SetActive(false);
        if (studyOptionGroup != null) studyOptionGroup.gameObject.SetActive(false);
        if (coffeeOptionGroup != null) coffeeOptionGroup.gameObject.SetActive(false);
    }

    void SetOptionActive(CanvasGroup g, Button b, bool show)
    {
        if (g == null) return;

        g.gameObject.SetActive(show);
        if (show)
        {
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
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