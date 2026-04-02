using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;
// scene management needed so we can return to home screen at the end

public class SofaEmailController : MonoBehaviour
// this is the main controller for the sofa scene, handles video switching, email interaction, exit flow and end screen
{
    public enum State { Sofa, CheckEmail, Exiting, Ending }
    // tracks which phase the game is currently in

    [Header("Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;
    public CanvasGroup videoCanvasGroup;
    // main video player setup

    [Header("Optional: disable other flow script while this runs")]
    public MonoBehaviour flowScriptToDisable;
    // lets us pause another script while sofa mode is active, avoid conflicts

    [Header("URLs")]
    public string sofaVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/3OnSofa.mp4";
    public string checkEmailURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/32CheckEmail.mp4";
    public string doNothingURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/33DoNth.mp4";
    // three videos, sofa loops, check email plays on click, do nothing plays on exit

    [Header("DoNothing Start Time")]
    public float doNothingStartAtSeconds = 2f;
    // skip the beginning of do nothing video, starts mid way

    [Header("Email Button UI")]
    public GameObject emailButtonObject;
    public Button emailButton;
    public CanvasGroup emailButtonCanvasGroup;
    // email button references, canvasgroup used for fade in and blocking raycasts

    [Header("Email Button Timing (when back to Sofa)")]
    public float emailButtonDelayOnSofa = 0.6f;
    public float emailButtonFadeInDuration = 0.8f;
    // small delay before email button appears, then fades in smoothly

    [Header("Hover Scale (optional)")]
    public RectTransform emailButtonRect;
    public float hoverScale = 1.12f;
    public float hoverScaleSpeed = 12f;
    // email button scales up on hover, handled in update every frame

    [Header("Check Email Pause + Drag Gate (drag down)")]
    public float pauseAtSeconds = 1f;
    public float dragAccumulation = 140f;
    public bool requireDragDown = true;
    public bool requireLeftMouseHeld = true;
    // video pauses at set time and waits for player to drag down before continuing

    [Header("Finger Hint (Drag Down)")]
    public SwipeHintAnimator fingerHintDown;
    // animated finger hint showing player to drag down, only shows once

    [Header("Progress Logic")]
    [Tooltip("最少要 CheckEmail 播完幾多次先出 Exit Door（達到後可無限 check!!!）")]
    public int minChecksToShowDoor = 3;
    // player must check email this many times before exit door appears

    [Header("Door Exit UI")]
    public GameObject exitDoorButtonObject;
    public Button exitDoorButton;
    public CanvasGroup exitDoorCanvasGroup;
    public bool syncDoorWithEmailButton = true;
    public bool showDoorOnlyAfterMinChecks = true;
    // exit door settings, can sync its appearance with email button or show separately

    [Header("Exit Flow")]
    public float afterDoorPressedHoldOnSofaSeconds = 0f;
    public float fadeOutLastSeconds = 3f;
    // optional hold on sofa after door pressed, then do nothing video fades out at the end

    [Header("End Screen UI")]
    public GameObject endScreenPanel;
    public TMP_Text endScreenText;
    [TextArea] public string endMessage = "";
    // the final screen shown after everything ends

    [Header("Return To Home")]
    public string homeSceneName = "HomePage";
    public float returnToHomeDelay = 8f;
    // after end screen shows, wait then load home scene

    State state = State.Sofa;

    bool waitingForDrag = false;
    float dragSum = 0f;
    Vector2 lastMousePos;
    bool hasLastMousePos = false;
    // drag gate variables, accumulates drag distance until threshold is hit

    bool hasShownHint2 = false;
    // tracks if drag hint has been shown, only show once

    Vector3 baseScale;
    bool isHovering = false;
    // for hover scale on email button

    Coroutine pauseCo;
    Coroutine showUiCo;
    Coroutine playCo;
    Coroutine returnHomeCo;
    // storing coroutine references so we can stop them if needed

    int emailCompleteCount = 0;
    bool emailHasFadedInOnce = false;
    // count how many times email was fully watched, and track if first fade already happened

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        // subscribe to video end event, remove first to avoid duplicates

        if (emailButton != null)
        {
            emailButton.onClick.RemoveListener(OnEmailClicked);
            emailButton.onClick.AddListener(OnEmailClicked);
        }
        // add email button click listener

        if (emailButtonRect == null && emailButtonObject != null)
            emailButtonRect = emailButtonObject.GetComponent<RectTransform>();
        if (emailButtonRect != null)
            baseScale = emailButtonRect.localScale;
        // grab rect transform and save base scale for hover effect

        if (videoCanvasGroup == null && videoRawImageObject != null)
            videoCanvasGroup = videoRawImageObject.GetComponent<CanvasGroup>();

        if (emailButtonCanvasGroup == null && emailButtonObject != null)
            emailButtonCanvasGroup = emailButtonObject.GetComponent<CanvasGroup>();

        if (exitDoorCanvasGroup == null && exitDoorButtonObject != null)
            exitDoorCanvasGroup = exitDoorButtonObject.GetComponent<CanvasGroup>();
        // auto grab canvas groups if not assigned in inspector

        ShowEmailButton(false);
        ShowExitDoor(false);
        // hide both buttons at start

        if (fingerHintDown != null) fingerHintDown.StopAndHide();
        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        if (endScreenText != null && !string.IsNullOrEmpty(endMessage))
            endScreenText.text = endMessage;
        // set end message text early
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
        // unsubscribe when destroyed to avoid memory leaks
    }

    void Update()
    {
        if (emailButtonRect != null)
        {
            Vector3 target = baseScale * (isHovering ? hoverScale : 1f);
            emailButtonRect.localScale = Vector3.Lerp(
                emailButtonRect.localScale,
                target,
                Time.unscaledDeltaTime * hoverScaleSpeed
            );
        }
        // smooth hover scale on email button every frame

        if (!waitingForDrag) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (requireLeftMouseHeld && !mouse.leftButton.isPressed)
        {
            hasLastMousePos = false;
            return;
        }
        // only track drag if left mouse is held down

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
        // accumulate drag distance, once it hits threshold trigger drag complete
    }

    public void StartSofaMode()
    {
        state = State.Sofa;

        waitingForDrag = false;
        dragSum = 0f;
        hasLastMousePos = false;

        if (flowScriptToDisable != null)
            flowScriptToDisable.enabled = false;
        // disable the other flow script so they dont interfere

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        if (videoCanvasGroup != null)
            videoCanvasGroup.alpha = 1f;

        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);

        if (fingerHintDown != null) fingerHintDown.StopAndHide();

        ShowEmailButton(false);
        ShowExitDoor(false);

        if (showUiCo != null) StopCoroutine(showUiCo);
        showUiCo = StartCoroutine(ShowSofaButtonsSynced());
        // start coroutine to show buttons after delay

        PlayUrl(sofaVideoURL, loop: true, startTimeSeconds: 0f);
        // play sofa video on loop
    }

    public void HideSofaButtonsImmediate()
    {
        ShowEmailButton(false);
        ShowExitDoor(false);
        // called by door button to instantly hide both buttons
    }

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
        // play check email video then pause it waiting for drag
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
        // pause at the set timestamp

        if (!hasShownHint2)
        {
            hasShownHint2 = true;
            if (fingerHintDown != null) fingerHintDown.ShowAndPlay();
        }
        else
        {
            if (fingerHintDown != null) fingerHintDown.StopAndHide();
        }
        // show drag hint only first time

        waitingForDrag = true;
        dragSum = 0f;
        hasLastMousePos = false;
    }

    void OnEmailDragCompleted()
    {
        if (videoPlayer != null) videoPlayer.Play();
        // drag done, resume the video
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (state == State.CheckEmail)
        {
            emailCompleteCount++;
            // increment count only when full check email video finishes

            StartSofaMode();
            // return to sofa after email watched
        }
        else if (state == State.Exiting)
        {
            ShowEndScreen();
        }
    }

    public void RequestExit()
    {
        if (state == State.Exiting || state == State.Ending) return;

        state = State.Exiting;

        if (afterDoorPressedHoldOnSofaSeconds > 0f)
            StartCoroutine(ExitRoutine());
        else
            StartCoroutine(PlayDoNothingAndEnd());
        // if hold time set, play sofa briefly first, otherwise go straight to do nothing
    }

    IEnumerator ExitRoutine()
    {
        PlayUrl(sofaVideoURL, loop: true, startTimeSeconds: 0f);
        HideSofaButtonsImmediate();

        yield return new WaitForSecondsRealtime(afterDoorPressedHoldOnSofaSeconds);

        yield return PlayDoNothingAndEnd();
        // hold on sofa for a moment then go to do nothing video
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
        // wait for length to load properly, timeout after 2 seconds

        if (length > 0.1 && !double.IsNaN(length) && fadeOutLastSeconds > 0.01f)
        {
            double fadeStartTime = Mathf.Max(0f, (float)length - fadeOutLastSeconds);

            while (videoPlayer.isPlaying && videoPlayer.time < fadeStartTime)
                yield return null;

            if (videoCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, 0f, fadeOutLastSeconds));
        }
        // fade out the video near the end before showing end screen

        ShowEndScreen();
    }

    IEnumerator ShowSofaButtonsSynced()
    {
        yield return new WaitForSecondsRealtime(emailButtonDelayOnSofa);
        if (state != State.Sofa) yield break;
        // if state changed while waiting, cancel

        bool reachedMin = (emailCompleteCount >= minChecksToShowDoor);
        bool shouldShowDoor = (!showDoorOnlyAfterMinChecks) || reachedMin;
        bool shouldShowEmail = true;

        if (syncDoorWithEmailButton)
        {
            if (shouldShowEmail)
            {
                if (!emailHasFadedInOnce)
                {
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
                    ShowEmailButton(true);

                    if (shouldShowDoor)
                        ShowExitDoor(true);
                }
            }
        }
        else
        {
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
        // first time = fade in, after that just show instantly. door only appears after min checks
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
        // fades two canvas groups at the same time so they appear together
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
        // single canvas group fade in version
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
        // general purpose fade, used for fading video out at the end
    }

    void ShowEndScreen()
    {
        if (state == State.Ending) return;

        state = State.Ending;

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);

        if (endScreenText != null && !string.IsNullOrEmpty(endMessage))
            endScreenText.text = endMessage;

        if (endScreenPanel != null)
            endScreenPanel.SetActive(true);
        // stop video, hide it, show end screen panel

        if (returnHomeCo != null) StopCoroutine(returnHomeCo);
        returnHomeCo = StartCoroutine(ReturnHomeAfterDelay());
    }

    IEnumerator ReturnHomeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(returnToHomeDelay);
        SceneManager.LoadScene(homeSceneName);
        // wait then load home scene
    }

    void PlayUrl(string url, bool loop, float startTimeSeconds)
    {
        if (videoPlayer == null) return;

        if (playCo != null) StopCoroutine(playCo);
        playCo = StartCoroutine(PlayWhenPrepared(url, loop, startTimeSeconds));
        // stop any current play coroutine and start new one
    }

    IEnumerator PlayWhenPrepared(string url, bool loop, float startTimeSeconds)
    {
        if (videoPlayer == null) yield break;

        videoPlayer.Stop();
        videoPlayer.isLooping = loop;
        videoPlayer.url = url;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared) yield return null;
        // wait until video is ready before playing

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
        // use canvasgroup to show/hide if available, otherwise toggle gameobject directly

        isHovering = false;
        if (emailButtonRect != null)
            emailButtonRect.localScale = baseScale;
        // reset hover scale when hiding
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
        // same pattern as email button, canvasgroup preferred
    }

    public void UI_OnPointerEnter() => isHovering = true;
    public void UI_OnPointerExit() => isHovering = false;
    // hooked up via EventTrigger in inspector for email button hover
}