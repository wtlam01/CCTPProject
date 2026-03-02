using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;

public class SofaEmailController : MonoBehaviour
{
    public enum State { Sofa, CheckEmail, Exiting, Ending }

    [Header("Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;
    public CanvasGroup videoCanvasGroup;

    [Header("Optional: disable other flow script while this runs")]
    public MonoBehaviour flowScriptToDisable;

    [Header("URLs")]
    public string sofaVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/3OnSofa.mp4";
    public string checkEmailURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32CheckEmail.mp4";
    public string doNothingURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/33DoNth.mp4";

    [Header("DoNothing Start Time")]
    public float doNothingStartAtSeconds = 2f;

    [Header("Email Button UI")]
    public GameObject emailButtonObject;
    public Button emailButton;
    public CanvasGroup emailButtonCanvasGroup; // IMPORTANT: EmailButton 自己的 CanvasGroup

    [Header("Email Button Timing (when back to Sofa)")]
    public float emailButtonDelayOnSofa = 0.6f;
    public float emailButtonFadeInDuration = 0.8f;

    [Header("Hover Scale (optional)")]
    public RectTransform emailButtonRect; // IMPORTANT: EmailButton 自己的 RectTransform
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
    [Tooltip("最少要 CheckEmail 播完幾多次先出 Exit Door（達到後可無限 check）")]
    public int minChecksToShowDoor = 3;

    [Header("Door Exit UI")]
    public GameObject exitDoorButtonObject;     // DoorButton1
    public Button exitDoorButton;               // DoorButton1(Button)
    public CanvasGroup exitDoorCanvasGroup;     // DoorButton1(CanvasGroup)
    public bool syncDoorWithEmailButton = true; // 同 Email 同步出現
    public bool showDoorOnlyAfterMinChecks = true;

    [Header("Exit Flow")]
    public float afterDoorPressedHoldOnSofaSeconds = 0f;
    public float fadeOutLastSeconds = 3f;

    [Header("End Screen UI")]
    public GameObject endScreenPanel;
    public TMP_Text endScreenText;
    [TextArea] public string endMessage = "";

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
    Coroutine showUiCo;
    Coroutine playCo;

    int emailCompleteCount = 0;     // ✅ CheckEmail 播完先 +1
    bool emailHasFadedInOnce = false; // ✅ Email 只 fade 第一次

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

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

        if (exitDoorCanvasGroup == null && exitDoorButtonObject != null)
            exitDoorCanvasGroup = exitDoorButtonObject.GetComponent<CanvasGroup>();

        ShowEmailButton(false);
        ShowExitDoor(false);

        if (fingerHintDown != null) fingerHintDown.StopAndHide();
        if (endScreenPanel != null) endScreenPanel.SetActive(false);

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
        // Hover scale (Email button)
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

                if (fingerHintDown != null) fingerHintDown.StopAndHide();
                OnEmailDragCompleted();
            }
        }
    }

    // ===== 外部入口 =====
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

        if (fingerHintDown != null) fingerHintDown.StopAndHide();

        // 先隱藏，再 show
        ShowEmailButton(false);
        ShowExitDoor(false);

        if (showUiCo != null) StopCoroutine(showUiCo);
        showUiCo = StartCoroutine(ShowSofaButtonsSynced());

        PlayUrl(sofaVideoURL, loop: true, startTimeSeconds: 0f);
    }

    // ✅ 提供俾 Door click 用：即刻收起兩個 button
    public void HideSofaButtonsImmediate()
    {
        ShowEmailButton(false);
        ShowExitDoor(false);
    }

    // ===== Email icon click =====
    public void OnEmailClicked()
    {
        if (state != State.Sofa) return;

        state = State.CheckEmail;

        if (showUiCo != null) StopCoroutine(showUiCo);
        showUiCo = null;

        waitingForDrag = false;
        dragSum = 0f;
        hasLastMousePos = false;

        ShowEmailButton(false);
        ShowExitDoor(false);

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

        // hint 只出一次
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
        // ✅ drag 完只係解鎖繼續播放
        if (videoPlayer != null) videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (state == State.CheckEmail)
        {
            // ✅ 播完 CheckEmail 先算一次
            emailCompleteCount++;

            // 返 sofa，可無限
            StartSofaMode();
        }
        else if (state == State.Exiting)
        {
            ShowEndScreen();
        }
    }

    // ===== DoorButton1 pressed 會 call 呢個 =====
    public void RequestExit()
    {
        if (state == State.Exiting || state == State.Ending) return;

        state = State.Exiting;

        if (afterDoorPressedHoldOnSofaSeconds > 0f)
            StartCoroutine(ExitRoutine());
        else
            StartCoroutine(PlayDoNothingAndEnd());
    }

    IEnumerator ExitRoutine()
    {
        PlayUrl(sofaVideoURL, loop: true, startTimeSeconds: 0f);
        HideSofaButtonsImmediate();

        yield return new WaitForSecondsRealtime(afterDoorPressedHoldOnSofaSeconds);

        yield return PlayDoNothingAndEnd();
    }

    IEnumerator PlayDoNothingAndEnd()
    {
        PlayUrl(doNothingURL, loop: false, startTimeSeconds: doNothingStartAtSeconds);

        while (videoPlayer != null && !videoPlayer.isPrepared) yield return null;
        if (videoCanvasGroup != null) videoCanvasGroup.alpha = 1f;

        if (videoPlayer == null) yield break;

        double length = videoPlayer.length;
        float timeout = 2f;
        while ((length <= 0.1 || double.IsNaN(length)) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            length = videoPlayer.length;
            yield return null;
        }

        if (length > 0.1 && !double.IsNaN(length) && fadeOutLastSeconds > 0.01f)
        {
            double fadeStartTime = Mathf.Max(0f, (float)length - fadeOutLastSeconds);

            while (videoPlayer.isPlaying && videoPlayer.time < fadeStartTime)
                yield return null;

            if (videoCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, 0f, fadeOutLastSeconds));
        }

        ShowEndScreen();
    }

    // ===== sofa 時同步顯示 Email + Door =====
    IEnumerator ShowSofaButtonsSynced()
    {
        yield return new WaitForSecondsRealtime(emailButtonDelayOnSofa);
        if (state != State.Sofa) yield break;

        bool reachedMin = (emailCompleteCount >= minChecksToShowDoor);
        bool shouldShowDoor = (!showDoorOnlyAfterMinChecks) || reachedMin;

        bool shouldShowEmail = true;

        if (syncDoorWithEmailButton)
        {
            // ✅ Email：只 fade 第一次；之後即刻出
            if (shouldShowEmail)
            {
                if (!emailHasFadedInOnce)
                {
                    // Door 同 Email 要「同一時間」出，所以：如果 door 都應該出，就用雙 fade
                    if (shouldShowDoor)
                    {
                        yield return StartCoroutine(FadeTwoCanvasGroupsIn(
                            emailButtonCanvasGroup, emailButtonObject,
                            exitDoorCanvasGroup, exitDoorButtonObject,
                            emailButtonFadeInDuration
                        ));
                    }
                    else
                    {
                        yield return StartCoroutine(FadeCanvasGroupIn(emailButtonCanvasGroup, emailButtonObject, emailButtonFadeInDuration));
                    }

                    emailHasFadedInOnce = true;
                }
                else
                {
                    // ✅ 之後唔 fade：即刻顯示
                    ShowEmailButton(true);

                    if (shouldShowDoor)
                        ShowExitDoor(true);
                }
            }
        }
        else
        {
            // 唔同步就各自處理
            if (!emailHasFadedInOnce)
            {
                if (shouldShowEmail) yield return StartCoroutine(FadeCanvasGroupIn(emailButtonCanvasGroup, emailButtonObject, emailButtonFadeInDuration));
                emailHasFadedInOnce = true;
            }
            else
            {
                if (shouldShowEmail) ShowEmailButton(true);
            }

            if (shouldShowDoor) ShowExitDoor(true);
        }
    }

    IEnumerator FadeTwoCanvasGroupsIn(CanvasGroup cgA, GameObject goA, CanvasGroup cgB, GameObject goB, float duration)
    {
        if (goA != null) goA.SetActive(true);
        if (goB != null) goB.SetActive(true);

        if (cgA == null || cgB == null) yield break;

        cgA.alpha = 0f; cgA.blocksRaycasts = false; cgA.interactable = false;
        cgB.alpha = 0f; cgB.blocksRaycasts = false; cgB.interactable = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);
            cgA.alpha = a;
            cgB.alpha = a;
            yield return null;
        }

        cgA.alpha = 1f; cgA.blocksRaycasts = true; cgA.interactable = true;
        cgB.alpha = 1f; cgB.blocksRaycasts = true; cgB.interactable = true;
    }

    IEnumerator FadeCanvasGroupIn(CanvasGroup cg, GameObject go, float duration)
    {
        if (go != null) go.SetActive(true);
        if (cg == null) yield break;

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
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
        state = State.Ending;

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);

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
        if (emailButtonObject != null) emailButtonObject.SetActive(true);

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

    void ShowExitDoor(bool show)
    {
        if (exitDoorButtonObject != null) exitDoorButtonObject.SetActive(true);

        if (exitDoorCanvasGroup != null)
        {
            exitDoorCanvasGroup.alpha = show ? 1f : 0f;
            exitDoorCanvasGroup.interactable = show;
            exitDoorCanvasGroup.blocksRaycasts = show;
        }
        else
        {
            if (exitDoorButtonObject != null) exitDoorButtonObject.SetActive(show);
            if (exitDoorButton != null) exitDoorButton.interactable = show;
        }
    }

    // UI EventTrigger hooks（Email hover 用）
    public void UI_OnPointerEnter() => isHovering = true;
    public void UI_OnPointerExit() => isHovering = false;
}