using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;

public class SofaEmailController : MonoBehaviour
{
    public enum State { Sofa, CheckEmail, Ending }

    [Header("Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;         // VideoRawImage (GameObject)
    public CanvasGroup videoCanvasGroup;           // CanvasGroup on VideoRawImage (for fade)

    [Header("Optional: disable other flow script while this runs")]
    public MonoBehaviour flowScriptToDisable;

    [Header("URLs")]
    public string sofaVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/3OnSofa.mp4";
    public string checkEmailURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32CheckEmail.mp4";
    public string doNothingURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/33DoNth.mp4";

    [Header("DoNothing Start Time")]
    [Tooltip("DoNothing video starts at this time (seconds). E.g. 2 means start from 00:02.")]
    public float doNothingStartAtSeconds = 2f;

    [Header("Email Button UI")]
    public GameObject emailButtonObject;
    public Button emailButton;

    [Header("Email Button Timing")]
    [Tooltip("Return to sofa -> wait X seconds -> email icon fades in.")]
    public float emailButtonDelayOnSofa = 2f;
    public float emailButtonFadeInDuration = 0.8f;
    public CanvasGroup emailButtonCanvasGroup;

    [Header("Hover Scale (optional)")]
    public RectTransform emailButtonRect;          // EmailButton's RectTransform
    public float hoverScale = 1.12f;
    public float hoverScaleSpeed = 12f;

    [Header("Check Email Pause + Drag Gate (drag down)")]
    public float pauseAtSeconds = 1f;
    public float dragAccumulation = 140f;
    public bool requireDragDown = true;
    public bool requireLeftMouseHeld = true;

    [Header("Finger Hint (Drag Down)")]
    public SwipeHintAnimator fingerHintDown;

    [Header("Progress Logic")]
    public int emailsToTriggerEnding = 3;
    public float afterThirdBackToSofaHideSeconds = 2f;

    [Header("Ending Fade")]
    public float fadeOutLastSeconds = 3f;

    [Header("End Screen UI")]
    public GameObject endScreenPanel;              // Blue panel, default SetActive(false)
    public TMP_Text endScreenText;                 // Optional
    [TextArea] public string endMessage = "";      // Optional: leave blank if you type directly in End(Text)

    State state = State.Sofa;

    // Drag gate
    bool waitingForDrag = false;
    float dragSum = 0f;
    Vector2 lastMousePos;
    bool hasLastMousePos = false;

    // Hint logic: show Hint2 only once
    bool hasShownHint2 = false;

    // Hover
    Vector3 baseScale;
    bool isHovering = false;

    Coroutine pauseCo;
    Coroutine emailButtonDelayCo;
    Coroutine playCo;

    int emailCompleteCount = 0;

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        if (emailButton != null)
        {
            emailButton.onClick.RemoveListener(OnEmailClicked);
            emailButton.onClick.AddListener(OnEmailClicked);
        }

        if (emailButtonRect == null && emailButtonObject != null)
            emailButtonRect = emailButtonObject.GetComponent<RectTransform>();

        if (emailButtonRect != null)
            baseScale = emailButtonRect.localScale;

        if (videoCanvasGroup == null && videoRawImageObject != null)
            videoCanvasGroup = videoRawImageObject.GetComponent<CanvasGroup>();

        if (emailButtonCanvasGroup == null && emailButtonObject != null)
            emailButtonCanvasGroup = emailButtonObject.GetComponent<CanvasGroup>();

        ShowEmailButton(false);

        if (fingerHintDown != null) fingerHintDown.StopAndHide();

        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        // Only override end text if endMessage is not empty
        if (endScreenText != null && !string.IsNullOrEmpty(endMessage))
            endScreenText.text = endMessage;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    void Update()
    {
        // Hover scale
        if (emailButtonRect != null)
        {
            Vector3 target = baseScale * (isHovering ? hoverScale : 1f);
            emailButtonRect.localScale = Vector3.Lerp(
                emailButtonRect.localScale,
                target,
                Time.unscaledDeltaTime * hoverScaleSpeed
            );
        }

        if (!waitingForDrag) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (requireLeftMouseHeld && !mouse.leftButton.isPressed)
        {
            hasLastMousePos = false;
            return;
        }

        Vector2 pos = mouse.position.ReadValue();

        if (!hasLastMousePos)
        {
            lastMousePos = pos;
            hasLastMousePos = true;
            return;
        }

        Vector2 delta = pos - lastMousePos;
        lastMousePos = pos;

        // Drag down: delta.y < 0
        if (Mathf.Abs(delta.y) > 0.01f)
        {
            bool isDown = delta.y < 0f;

            if (!requireDragDown || isDown)
                dragSum += Mathf.Abs(delta.y);

            if (dragSum >= dragAccumulation)
            {
                dragSum = 0f;
                waitingForDrag = false;
                hasLastMousePos = false;

                // Always hide hint once the user succeeds
                if (fingerHintDown != null) fingerHintDown.StopAndHide();

                OnEmailDragCompleted();
            }
        }
    }

    // Call after door click
    public void StartSofaMode()
    {
        state = State.Sofa;

        waitingForDrag = false;
        dragSum = 0f;
        hasLastMousePos = false;

        if (flowScriptToDisable != null)
            flowScriptToDisable.enabled = false;

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        if (videoCanvasGroup != null)
            videoCanvasGroup.alpha = 1f;

        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);

        // Don't pop email icon instantly
        ShowEmailButton(false);

        if (emailButtonDelayCo != null) StopCoroutine(emailButtonDelayCo);
        emailButtonDelayCo = null;

        if (emailCompleteCount < emailsToTriggerEnding)
            emailButtonDelayCo = StartCoroutine(ShowEmailButtonAfterDelayAndFade(emailButtonDelayOnSofa, emailButtonFadeInDuration));

        PlayUrl(sofaVideoURL, loop: true, startTimeSeconds: 0f);

        if (fingerHintDown != null) fingerHintDown.StopAndHide();
    }

    IEnumerator ShowEmailButtonAfterDelayAndFade(float delay, float fadeDuration)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (state != State.Sofa) yield break;
        if (emailCompleteCount >= emailsToTriggerEnding) yield break;

        // If no CanvasGroup, fallback to instant show
        if (emailButtonCanvasGroup == null)
        {
            ShowEmailButton(true);
            yield break;
        }

        // Fade in
        if (emailButtonObject != null) emailButtonObject.SetActive(true);
        emailButtonCanvasGroup.alpha = 0f;
        emailButtonCanvasGroup.interactable = false;
        emailButtonCanvasGroup.blocksRaycasts = false;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            emailButtonCanvasGroup.alpha = p;
            yield return null;
        }

        emailButtonCanvasGroup.alpha = 1f;
        emailButtonCanvasGroup.interactable = true;
        emailButtonCanvasGroup.blocksRaycasts = true;
    }

    public void OnEmailClicked()
    {
        if (state != State.Sofa) return;
        if (emailCompleteCount >= emailsToTriggerEnding) return;

        state = State.CheckEmail;

        if (emailButtonDelayCo != null) StopCoroutine(emailButtonDelayCo);
        emailButtonDelayCo = null;

        waitingForDrag = false;
        dragSum = 0f;
        hasLastMousePos = false;

        ShowEmailButton(false);

        PlayUrl(checkEmailURL, loop: false, startTimeSeconds: 0f);

        if (pauseCo != null) StopCoroutine(pauseCo);
        pauseCo = StartCoroutine(PauseAtTimeThenWaitDrag());
    }

    IEnumerator PauseAtTimeThenWaitDrag()
    {
        if (videoPlayer == null) yield break;

        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();

        while (videoPlayer.time < pauseAtSeconds)
            yield return null;

        videoPlayer.Pause();

        // ✅ Show hint only the FIRST time
        if (!hasShownHint2)
        {
            hasShownHint2 = true;
            if (fingerHintDown != null) fingerHintDown.ShowAndPlay();
        }
        else
        {
            if (fingerHintDown != null) fingerHintDown.StopAndHide();
        }

        waitingForDrag = true;
        dragSum = 0f;
        hasLastMousePos = false;
    }

    void OnEmailDragCompleted()
    {
        if (videoPlayer != null) videoPlayer.Play();

        emailCompleteCount++;

        if (emailCompleteCount >= emailsToTriggerEnding)
            ShowEmailButton(false);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (state == State.CheckEmail)
        {
            if (emailCompleteCount >= emailsToTriggerEnding)
                StartCoroutine(EndingSequence());
            else
                StartSofaMode();
        }
        else if (state == State.Ending)
        {
            ShowEndScreen();
        }
    }

    IEnumerator EndingSequence()
    {
        state = State.Ending;

        // 1) back to sofa, NO icon
        PlayUrl(sofaVideoURL, loop: true, startTimeSeconds: 0f);
        ShowEmailButton(false);

        yield return new WaitForSecondsRealtime(afterThirdBackToSofaHideSeconds);

        // 2) play doNothing FROM 2 seconds
        PlayUrl(doNothingURL, loop: false, startTimeSeconds: doNothingStartAtSeconds);

        while (videoPlayer != null && !videoPlayer.isPrepared) yield return null;

        if (videoCanvasGroup != null) videoCanvasGroup.alpha = 1f;

        if (videoPlayer == null) yield break;

        // Try get length
        double length = videoPlayer.length;
        float timeout = 2f;
        while ((length <= 0.1 || double.IsNaN(length)) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            length = videoPlayer.length;
            yield return null;
        }

        if (length <= 0.1 || double.IsNaN(length))
            yield break;

        double fadeStartTime = Mathf.Max(0f, (float)length - fadeOutLastSeconds);

        while (videoPlayer.isPlaying && videoPlayer.time < fadeStartTime)
            yield return null;

        if (videoCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, 0f, fadeOutLastSeconds));

        ShowEndScreen();
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        cg.alpha = to;
    }

    void ShowEndScreen()
    {
        if (videoPlayer != null) videoPlayer.Stop();

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(false);

        // Only override text if endMessage is not empty
        if (endScreenText != null && !string.IsNullOrEmpty(endMessage))
            endScreenText.text = endMessage;

        if (endScreenPanel != null)
            endScreenPanel.SetActive(true);
    }

    void PlayUrl(string url, bool loop, float startTimeSeconds)
    {
        if (videoPlayer == null) return;

        if (playCo != null) StopCoroutine(playCo);
        playCo = StartCoroutine(PlayWhenPrepared(url, loop, startTimeSeconds));
    }

    IEnumerator PlayWhenPrepared(string url, bool loop, float startTimeSeconds)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.isLooping = loop;
        videoPlayer.url = url;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared) yield return null;

        if (startTimeSeconds < 0f) startTimeSeconds = 0f;
        videoPlayer.time = startTimeSeconds;

        videoPlayer.Play();
    }

    void ShowEmailButton(bool show)
    {
        // Keep object active; use alpha control if available
        if (emailButtonObject != null)
            emailButtonObject.SetActive(true);

        if (emailButtonCanvasGroup != null)
        {
            emailButtonCanvasGroup.alpha = show ? 1f : 0f;
            emailButtonCanvasGroup.interactable = show;
            emailButtonCanvasGroup.blocksRaycasts = show;
        }
        else
        {
            if (emailButtonObject != null) emailButtonObject.SetActive(show);
            if (emailButton != null) emailButton.interactable = show;
        }

        isHovering = false;
        if (emailButtonRect != null)
            emailButtonRect.localScale = baseScale;
    }

    // UI EventTrigger hooks
    public void UI_OnPointerEnter() => isHovering = true;
    public void UI_OnPointerExit() => isHovering = false;
}