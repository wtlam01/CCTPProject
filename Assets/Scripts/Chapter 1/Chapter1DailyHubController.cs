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

    [Header("Hub UI (CanvasGroups on each option)")]
    public CanvasGroup chatOptionGroup;     // Option_Chat CanvasGroup
    public CanvasGroup studyOptionGroup;    // Option_Study CanvasGroup
    public CanvasGroup coffeeOptionGroup;   // Option_Coffee CanvasGroup
    public Button chatButton;
    public Button studyButton;
    public Button coffeeButton;

    [Header("URLs")]
    public string studyVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/2Studying.mp4";
    public string coffeeVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/23Resting.mp4";

    [Header("Study: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;     // count presses within this window
    public float maxPressesPerSecond = 8f;       // this APS reaches max speed
    public float maxPlaybackSpeed = 5f;          // speed when APS hits maxPressesPerSecond
    public float speedSmoothing = 10f;           // higher = snappier
    public float stopAfterNoPressSeconds = 0.25f;// no press => stop
    public float minPlaybackSpeed = 0f;          // 0 = fully stop
    public float endPadding = 0.05f;             // close-to-end threshold

    [Header("Space Hint (press demo)")]
    public RectTransform spaceHintRect;  // SpaceHint rect
    public CanvasGroup spaceHintGroup;   // SpaceHint canvas group
    public float hintShowDelay = 0.25f;

    [Header("Space Hint Animation")]
    public float pressDownScale = 0.88f;
    public float pressDownTime = 0.10f;
    public float releaseTime = 0.14f;
    public float pressPause = 0.70f;   // ✅ 你話想「相隔耐d」：加大呢個
    public float loopDelay = 0.50f;    // ✅ 同樣可以加大

    [Header("Rest: swipe stops")]
    public double restStop1 = 6.0;
    public double restStop2 = 10.0;

    [Header("Swipe Hint (Chapter1 only)")]
    public RectTransform swipePos6s;             // SwipeHintPos_6s (RectTransform)
    public RectTransform swipePos10s;            // SwipeHintPos_10s (RectTransform)
    public SwipeHintAnimator swipeAnim;          // FingerHint(Image) 上嘅 SwipeHintAnimator
    public float swipeMinDistance = 120f;        // px
    public float swipeMaxTime = 0.6f;            // seconds

    bool isPlaying = false;

    bool chatLocked = false;
    bool coffeeUnlocked = false;
    bool coffeeFirstTimeSwipeStops = true;

    // study press tracking
    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;

    // hint coroutine
    Coroutine spaceHintCo;

    // swipe state
    bool waitingSwipe = false;
    Vector2 swipeStartPos;
    float swipeStartTime;
    bool swipeTriggered = false;

    void Awake()
    {
        // Video defaults
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.playbackSpeed = 1f;
            videoPlayer.Stop();
        }

        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);

        // Button listeners
        if (studyButton != null)
        {
            studyButton.onClick.RemoveListener(OnStudyClicked);
            studyButton.onClick.AddListener(OnStudyClicked);
        }

        if (coffeeButton != null)
        {
            coffeeButton.onClick.RemoveListener(OnCoffeeClicked);
            coffeeButton.onClick.AddListener(OnCoffeeClicked);
        }

        // Initial state:
        chatLocked = false;
        coffeeUnlocked = false;
        coffeeFirstTimeSwipeStops = true;

        // Hard-hide coffee at start (no “appear at beginning”)
        SetOption(coffeeOptionGroup, coffeeButton, show:false, disableGO:true);

        // Show hub (Chat + Study only)
        ApplyHubState(showHub:true);

        // Hide hints
        SetSpaceHintVisible(false);
        StopSpaceHintLoop();

        if (swipeAnim != null) swipeAnim.StopAndHide();
    }

    void Update()
    {
        // swipe detection only while waiting
        if (!waitingSwipe) return;

        // allow keyboard UpArrow as backup test
        if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            swipeTriggered = true;
            return;
        }

        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            swipeStartPos = Pointer.current.position.ReadValue();
            swipeStartTime = Time.unscaledTime;
        }

        if (Pointer.current.press.wasReleasedThisFrame)
        {
            Vector2 endPos = Pointer.current.position.ReadValue();
            float dt = Time.unscaledTime - swipeStartTime;
            float dy = endPos.y - swipeStartPos.y;

            if (dt <= swipeMaxTime && dy >= swipeMinDistance)
            {
                swipeTriggered = true;
            }
        }
    }

    // -------------------- UI Actions --------------------
    void OnStudyClicked()
    {
        if (isPlaying) return;

        chatLocked = true; // after first real choice, chat disappears forever
        StartCoroutine(PlayStudyRoutine());
    }

    void OnCoffeeClicked()
    {
        if (isPlaying) return;
        if (!coffeeUnlocked) return;

        chatLocked = true;

        if (coffeeFirstTimeSwipeStops)
            StartCoroutine(PlayCoffeeSwipeStopsRoutine());
        else
            StartCoroutine(PlayCoffeeSimpleRoutine());
    }

    // -------------------- Study Routine --------------------
    IEnumerator PlayStudyRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub:false);

        yield return PrepareVideo(studyVideoURL);
        ShowVideo(true);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;

        // Start frozen until presses
        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();

        // show hint after delay, until first press
        yield return new WaitForSecondsRealtime(hintShowDelay);
        StartSpaceHintLoop();

        double duration = videoPlayer.length;
        if (duration <= 0.01) duration = 12.0;

        float currentSpeed = 0f;
        bool firstPress = false;

        while (videoPlayer.time < duration - endPadding)
        {
            bool pressed =
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

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

            // if no press recently -> stop
            if (Time.unscaledTime - lastPressAt > stopAfterNoPressSeconds)
                targetSpeed = 0f;

            float lerpFactor = 1f - Mathf.Exp(-speedSmoothing * Time.unscaledDeltaTime);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpFactor);

            videoPlayer.playbackSpeed = currentSpeed;

            yield return null;
        }

        // finish
        videoPlayer.playbackSpeed = 1f;
        StopSpaceHintLoop();
        SetSpaceHintVisible(false);

        ShowVideo(false);

        // unlock coffee after first Study completes
        coffeeUnlocked = true;

        ApplyHubState(showHub:true);
        isPlaying = false;
    }

    // -------------------- Coffee (Rest) Routine with Swipe Stops --------------------
    IEnumerator PlayCoffeeSwipeStopsRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub:false);

        yield return PrepareVideo(coffeeVideoURL);
        ShowVideo(true);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();

        // play to 6s then pause
        yield return PlayUntilTime(restStop1);
        yield return WaitForSwipeAtPos(swipePos6s);

        // play to 10s then pause
        videoPlayer.Play();
        yield return PlayUntilTime(restStop2);
        yield return WaitForSwipeAtPos(swipePos10s);

        // play to end
        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        ShowVideo(false);

        coffeeFirstTimeSwipeStops = false; // next time, no stops (optional)

        ApplyHubState(showHub:true);
        isPlaying = false;
    }

    // If you want later coffee clicks play normally (no swipe stops)
    IEnumerator PlayCoffeeSimpleRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub:false);

        yield return PrepareVideo(coffeeVideoURL);
        ShowVideo(true);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        ShowVideo(false);
        ApplyHubState(showHub:true);
        isPlaying = false;
    }

    IEnumerator PlayUntilTime(double stopTime)
    {
        if (videoPlayer == null) yield break;

        while (videoPlayer.time < stopTime)
            yield return null;

        videoPlayer.Pause();
    }

    IEnumerator WaitForSwipeAtPos(RectTransform pos)
    {
        swipeTriggered = false;
        waitingSwipe = true;

        // pause while waiting
        if (videoPlayer != null) videoPlayer.Pause();

        // show finger hint at desired position
        if (swipeAnim != null)
        {
            if (pos != null)
                swipeAnim.transform.position = pos.position; // world position
            swipeAnim.ShowAndPlay();
        }

        while (!swipeTriggered)
            yield return null;

        waitingSwipe = false;

        if (swipeAnim != null) swipeAnim.StopAndHide();

        yield return null;
    }

    // -------------------- Video Helpers --------------------
    IEnumerator PrepareVideo(string url)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;
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

    // -------------------- Hub UI State --------------------
    void ApplyHubState(bool showHub)
    {
        if (!showHub)
        {
            SetOption(chatOptionGroup, chatButton, show:false, disableGO:true);
            SetOption(studyOptionGroup, studyButton, show:false, disableGO:true);
            SetOption(coffeeOptionGroup, coffeeButton, show:false, disableGO:true);
            return;
        }

        // chat: only before first choice
        if (!chatLocked)
            SetOption(chatOptionGroup, chatButton, show:true, disableGO:false);
        else
            SetOption(chatOptionGroup, chatButton, show:false, disableGO:true);

        // study always available
        SetOption(studyOptionGroup, studyButton, show:true, disableGO:false);

        // coffee only after unlocked
        if (coffeeUnlocked)
            SetOption(coffeeOptionGroup, coffeeButton, show:true, disableGO:false);
        else
            SetOption(coffeeOptionGroup, coffeeButton, show:false, disableGO:true);
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
            // press down
            float t = 0f;
            while (t < pressDownTime)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(baseScale, downScale, t / pressDownTime);
                yield return null;
            }
            spaceHintRect.localScale = downScale;

            // release
            t = 0f;
            while (t < releaseTime)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(downScale, baseScale, t / releaseTime);
                yield return null;
            }
            spaceHintRect.localScale = baseScale;

            // ✅ “相隔耐d” 就調大 pressPause / loopDelay
            yield return new WaitForSecondsRealtime(pressPause);
            yield return new WaitForSecondsRealtime(loopDelay);
        }
    }
}