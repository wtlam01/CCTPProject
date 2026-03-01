using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class Chapter1HubController : MonoBehaviour
{
    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;

    [Header("Hub UI (CanvasGroups on each option)")]
    public CanvasGroup chatOptionGroup;
    public CanvasGroup studyOptionGroup;
    public CanvasGroup coffeeOptionGroup;
    public Button chatButton;
    public Button studyButton;
    public Button coffeeButton;

    [Header("URLs")]
    public string studyVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/2Studying.mp4";
    public string coffeeVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/23Resting.mp4";

    [Header("Study Speed (press rate -> playbackSpeed)")]
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

    [Header("Swipe Up Hint (Rest)")]
    public GameObject swipeHintObject;      // finger hint GO
    public CanvasGroup swipeHintGroup;      // optional

    [Header("Swipe Hint Positions (IMPORTANT)")]
    public RectTransform swipeHintRect;     // ✅ finger hint RectTransform (same GO as swipeHintObject)
    public RectTransform swipeHintPosAt6s;  // ✅ empty RectTransform position for first stop
    public RectTransform swipeHintPosAt10s; // ✅ empty RectTransform position for second stop

    public float swipeMinDistance = 120f;   // px
    public float swipeMaxTime = 0.6f;       // seconds

    [Header("Rest Stops (seconds)")]
    public double restStop1 = 6.0;
    public double restStop2 = 10.0;

    bool isPlaying = false;

    bool chatLocked = false;
    bool coffeeUnlocked = false;

    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;

    Coroutine hintCo;

    // swipe tracking
    bool swipeWaiting = false;
    Vector2 swipeStartPos;
    float swipeStartTime;
    bool _swipeTriggered = false;

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

        // Start state: show Chat + Study only, hide Coffee
        chatLocked = false;
        coffeeUnlocked = false;
        ApplyHubState(showHub: true);

        // avoid coffee flash
        if (coffeeOptionGroup != null) coffeeOptionGroup.gameObject.SetActive(false);

        SetHintVisible(false);
        SetSwipeHintVisible(false);
    }

    void Update()
    {
        if (!swipeWaiting) return;
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
                swipeWaiting = false;
                SetSwipeHintVisible(false);
                _swipeTriggered = true;
            }
        }
    }

    void OnStudyClicked()
    {
        if (isPlaying) return;

        chatLocked = true;
        StartCoroutine(PlayStudyRateControlsSpeedRoutine());
    }

    void OnCoffeeClicked()
    {
        if (isPlaying) return;
        if (!coffeeUnlocked) return;

        chatLocked = true;
        StartCoroutine(PlayCoffeeSwipeStopsRoutine());
    }

    IEnumerator PlayStudyRateControlsSpeedRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        yield return PrepareVideo(studyVideoURL);
        ShowVideo(true);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();

        yield return new WaitForSecondsRealtime(hintShowDelay);
        StartHintLoop();

        double duration = videoPlayer.length;
        if (duration <= 0.01) duration = 12.0;

        float currentSpeed = 0f;
        bool firstPressHappened = false;

        while (videoPlayer.time < duration - endPadding)
        {
            bool pressedThisFrame =
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (pressedThisFrame)
            {
                pressTimes.Enqueue(Time.unscaledTime);
                lastPressAt = Time.unscaledTime;

                if (!firstPressHappened)
                {
                    firstPressHappened = true;
                    StopHintLoop();
                    SetHintVisible(false);
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
        StopHintLoop();
        SetHintVisible(false);

        ShowVideo(false);

        // unlock coffee after study ends
        coffeeUnlocked = true;

        ApplyHubState(showHub: true);
        isPlaying = false;
    }

    // ✅ Rest video: stop at 6s -> swipe(pos1) -> stop at 10s -> swipe(pos2) -> play to end
    IEnumerator PlayCoffeeSwipeStopsRoutine()
    {
        isPlaying = true;
        ApplyHubState(showHub: false);

        yield return PrepareVideo(coffeeVideoURL);
        ShowVideo(true);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();

        // Segment 1: play to 6s
        yield return PlayUntilTime(restStop1);

        // wait swipe 1 (move hint to posAt6s)
        yield return WaitForSwipeUp(swipeHintPosAt6s);

        // Segment 2: play to 10s
        videoPlayer.Play();
        yield return PlayUntilTime(restStop2);

        // wait swipe 2 (move hint to posAt10s)
        yield return WaitForSwipeUp(swipeHintPosAt10s);

        // Segment 3: play to end
        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        ShowVideo(false);
        ApplyHubState(showHub: true);
        isPlaying = false;
    }

    IEnumerator PlayUntilTime(double stopTime)
    {
        if (videoPlayer == null) yield break;

        while (videoPlayer.time < stopTime)
            yield return null;

        videoPlayer.Pause();
    }

    IEnumerator WaitForSwipeUp(RectTransform pos)
    {
        _swipeTriggered = false;
        swipeWaiting = true;

        MoveSwipeHintTo(pos);
        SetSwipeHintVisible(true);

        if (videoPlayer != null) videoPlayer.Pause();

        while (!_swipeTriggered)
            yield return null;

        yield return null;
    }

    void MoveSwipeHintTo(RectTransform targetPos)
    {
        if (swipeHintRect == null || targetPos == null) return;

        // ✅ simplest: copy anchoredPosition (works when same Canvas space)
        swipeHintRect.anchoredPosition = targetPos.anchoredPosition;
    }

    IEnumerator PrepareVideo(string url)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.url = url;
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

    void ApplyHubState(bool showHub)
    {
        if (!showHub)
        {
            SetOption(chatOptionGroup, chatButton, false, disableGO: true);
            SetOption(studyOptionGroup, studyButton, false, disableGO: true);
            SetOption(coffeeOptionGroup, coffeeButton, false, disableGO: true);
            return;
        }

        if (!chatLocked)
            SetOption(chatOptionGroup, chatButton, true, disableGO: false);
        else
            SetOption(chatOptionGroup, chatButton, false, disableGO: true);

        SetOption(studyOptionGroup, studyButton, true, disableGO: false);

        if (coffeeUnlocked)
            SetOption(coffeeOptionGroup, coffeeButton, true, disableGO: false);
        else
            SetOption(coffeeOptionGroup, coffeeButton, false, disableGO: true);
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

    // ---------- Space Hint ----------
    void SetHintVisible(bool show)
    {
        if (spaceHintRect != null) spaceHintRect.gameObject.SetActive(show);

        if (spaceHintGroup != null)
        {
            spaceHintGroup.alpha = show ? 1f : 0f;
            spaceHintGroup.blocksRaycasts = false;
            spaceHintGroup.interactable = false;
        }
    }

    void StartHintLoop()
    {
        if (spaceHintRect == null) return;

        SetHintVisible(true);

        if (hintCo != null) StopCoroutine(hintCo);
        hintCo = StartCoroutine(HintLoop());
    }

    void StopHintLoop()
    {
        if (hintCo != null) StopCoroutine(hintCo);
        hintCo = null;

        if (spaceHintRect != null) spaceHintRect.localScale = Vector3.one;
    }

    IEnumerator HintLoop()
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

    // ---------- Swipe Hint ----------
    void SetSwipeHintVisible(bool show)
    {
        if (swipeHintObject != null) swipeHintObject.SetActive(show);

        if (swipeHintGroup != null)
        {
            swipeHintGroup.alpha = show ? 1f : 0f;
            swipeHintGroup.blocksRaycasts = false;
            swipeHintGroup.interactable = false;
        }
    }
}