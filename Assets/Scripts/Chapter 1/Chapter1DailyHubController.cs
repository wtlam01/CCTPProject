using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class Chapter1DailyHubController : MonoBehaviour
{
    [Header("Loop Manager")]
    public Chapter1LoopManager loopManager;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;

    [Header("Options (CanvasGroups + Buttons)")]
    public CanvasGroup studyGroup;
    public CanvasGroup coffeeGroup;
    public CanvasGroup playGroup;

    public Button studyButton;
    public Button coffeeButton;
    public Button playButton;

    [Header("URLs")]
    public string studyVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/2Studying.mp4";
    public string restVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/23Resting.mp4";
    public string playVideoURL  = ""; // optional

    [Header("Study: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;
    public float maxPressesPerSecond = 8f;
    public float maxPlaybackSpeed = 5f;
    public float speedSmoothing = 10f;
    public float stopAfterNoPressSeconds = 0.25f;
    public float endPadding = 0.05f;

    [Header("Space Hint")]
    public RectTransform spaceHintRect;
    public CanvasGroup spaceHintGroup;
    public float hintShowDelay = 0.25f;

    [Header("Rest: swipe stops")]
    public double restStop1 = 6.0;
    public double restStop2 = 10.0;

    [Header("Swipe Hint (Chapter1 only)")]
    public RectTransform swipePos6s;
    public RectTransform swipePos10s;
    public SwipeHintAnimator_Chapter1 swipeAnim;

    bool isPlaying;
    readonly System.Collections.Generic.Queue<float> pressTimes = new System.Collections.Generic.Queue<float>();
    float lastPressAt = -999f;
    Coroutine hintCo;

    // swipe detect
    bool waitSwipe;
    bool swipeTriggered;
    Vector2 swipeStartPos;
    float swipeStartTime;
    public float swipeMinDistance = 120f;
    public float swipeMaxTime = 0.6f;

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
        }

        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        SetSpaceHint(false);

        if (studyButton != null) studyButton.onClick.AddListener(OnStudy);
        if (coffeeButton != null) coffeeButton.onClick.AddListener(OnRest);
        if (playButton != null) playButton.onClick.AddListener(OnPlay);

        ShowHub(true);
    }

    void Update()
    {
        if (!waitSwipe) return;
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
                waitSwipe = false;
                if (swipeAnim != null) swipeAnim.StopAndHide();
            }
        }
    }

    void OnStudy()
    {
        if (isPlaying) return;
        if (loopManager != null && loopManager.IsBusy) return;
        StartCoroutine(StudyRoutine());
    }

    void OnRest()
    {
        if (isPlaying) return;
        if (loopManager != null && loopManager.IsBusy) return;
        StartCoroutine(RestRoutine());
    }

    void OnPlay()
    {
        if (isPlaying) return;
        if (loopManager != null && loopManager.IsBusy) return;
        StartCoroutine(PlayRoutine());
    }

    IEnumerator StudyRoutine()
    {
        isPlaying = true;
        ShowHub(false);

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
                    StopHintLoop();
                    SetSpaceHint(false);
                }
            }

            while (pressTimes.Count > 0 && Time.unscaledTime - pressTimes.Peek() > sampleWindowSeconds)
                pressTimes.Dequeue();

            float aps = pressTimes.Count / Mathf.Max(0.0001f, sampleWindowSeconds);
            float t = Mathf.Clamp01(aps / maxPressesPerSecond);
            float targetSpeed = Mathf.Lerp(0f, maxPlaybackSpeed, t);

            if (Time.unscaledTime - lastPressAt > stopAfterNoPressSeconds)
                targetSpeed = 0f;

            float lerpFactor = 1f - Mathf.Exp(-speedSmoothing * Time.unscaledDeltaTime);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpFactor);

            videoPlayer.playbackSpeed = currentSpeed;
            yield return null;
        }

        videoPlayer.playbackSpeed = 1f;
        StopHintLoop();
        SetSpaceHint(false);

        ShowVideo(false);

        // ✅ apply day stats AFTER action finished
        if (loopManager != null) loopManager.ApplyStudy();

        ShowHub(true);
        isPlaying = false;
    }

    IEnumerator RestRoutine()
    {
        isPlaying = true;
        ShowHub(false);

        yield return PrepareVideo(restVideoURL);
        ShowVideo(true);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();

        // play to 6s
        yield return PlayUntil(restStop1);
        yield return WaitSwipeAt(swipePos6s);

        // play to 10s
        videoPlayer.Play();
        yield return PlayUntil(restStop2);
        yield return WaitSwipeAt(swipePos10s);

        // play to end
        videoPlayer.Play();
        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;

        ShowVideo(false);

        if (loopManager != null) loopManager.ApplyRest();

        ShowHub(true);
        isPlaying = false;
    }

    IEnumerator PlayRoutine()
    {
        isPlaying = true;
        ShowHub(false);

        if (!string.IsNullOrEmpty(playVideoURL))
        {
            yield return PrepareVideo(playVideoURL);
            ShowVideo(true);
            videoPlayer.time = 0;
            videoPlayer.playbackSpeed = 1f;
            videoPlayer.Play();
            while (videoPlayer != null && videoPlayer.isPlaying) yield return null;
            ShowVideo(false);
        }

        if (loopManager != null) loopManager.ApplyPlayAvoid();

        ShowHub(true);
        isPlaying = false;
    }

    IEnumerator PrepareVideo(string url)
    {
        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
    }

    IEnumerator PlayUntil(double stopTime)
    {
        while (videoPlayer.time < stopTime) yield return null;
        videoPlayer.Pause();
    }

    IEnumerator WaitSwipeAt(RectTransform pos)
    {
        swipeTriggered = false;
        waitSwipe = true;

        if (videoPlayer != null) videoPlayer.Pause();

        if (swipeAnim != null)
        {
            swipeAnim.SetBaseFrom(pos);
            swipeAnim.ShowAndPlay();
        }

        while (!swipeTriggered) yield return null;
    }

    void ShowVideo(bool show)
    {
        if (videoRawImageObject != null) videoRawImageObject.SetActive(show);
        if (!show && videoPlayer != null) videoPlayer.Stop();
    }

    void ShowHub(bool show)
    {
        SetOption(studyGroup, studyButton, show);
        SetOption(coffeeGroup, coffeeButton, show);
        SetOption(playGroup, playButton, show);
    }

    void SetOption(CanvasGroup g, Button b, bool show)
    {
        if (g == null) return;
        g.gameObject.SetActive(show);
        g.alpha = show ? 1f : 0f;
        g.blocksRaycasts = show;
        g.interactable = show;
        if (b != null) b.interactable = show;
    }

    // ---------- Space Hint ----------
    void SetSpaceHint(bool show)
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
        SetSpaceHint(true);
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
        Vector3 down = baseScale * 0.88f;

        while (true)
        {
            // press down
            float t = 0f;
            while (t < 0.10f)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(baseScale, down, t / 0.10f);
                yield return null;
            }

            // release
            t = 0f;
            while (t < 0.14f)
            {
                t += Time.unscaledDeltaTime;
                spaceHintRect.localScale = Vector3.Lerp(down, baseScale, t / 0.14f);
                yield return null;
            }

            // ✅ “相隔耐啲” 就加呢兩個
            yield return new WaitForSecondsRealtime(0.70f); // pressPause
            yield return new WaitForSecondsRealtime(0.50f); // loopDelay
        }
    }
}