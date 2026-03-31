using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
// lot of imports again, this script handles videos, input, UI and scene loading

public class Chapter1TwoController : MonoBehaviour
// controls the chapter 1 two hub, player can choose to study together or play, tracks progress and triggers exam
{
    [Header("Scenes")]
    public string miniGameSceneName = "MiniGame";
    public string returnSceneName = "Chapter1two";
    public string nextSceneName = "Chapter2";
    // three possible scenes to load depending on what happens

    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;
    public RawImage videoRawImage;
    // video player and the raw image displaying it

    [Header("BG (show when video hidden)")]
    public GameObject bgImageObject;
    // background shown when no video is playing

    [Header("Hub UI")]
    public CanvasGroup optionStudyGroup;
    public CanvasGroup optionPlayGroup;
    public Button studyButton;
    public Button playButton;
    // two choice buttons with canvas groups for show/hide

    [Header("URLs")]
    public string studyTogetherURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/233Studying4ogether.mp4";
    public string peerInfluenceURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/232PeerInfluence1.mp4";
    // study video uses space mash mechanic, peer influence plays before mini game

    [Header("Exam / Result URLs")]
    public string examVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/24Exam.mp4";
    public string successVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/25academicsuccess.mp4";
    public string failureVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/26Failure.mp4";
    // exam plays after max days, then either success or failure based on progress

    [Header("System")]
    public int MAX_DAYS = 7;
    public int dayPerChoice = 1;
    // game ends after 7 days, each choice costs 1 day

    bool isPlaying = false;
    // prevents multiple choices triggering at same time

    [Header("StudyTogether: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;
    public float maxPressesPerSecond = 8f;
    public float maxPlaybackSpeed = 5f;
    public float speedSmoothing = 10f;
    public float stopAfterNoPressSeconds = 0.25f;
    public float minPlaybackSpeed = 0f;
    public float endPadding = 0.05f;
    // same space mash mechanic as chapter 1, controls video playback speed

    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;
    // queue tracks recent press timestamps for calculating presses per second

    [Header("Space Hint")]
    public RectTransform spaceHintRect;
    public CanvasGroup spaceHintGroup;
    public float hintShowDelay = 0.25f;
    // space bar hint UI shown on first study

    [Header("Space Hint Animation")]
    public float pressDownScale = 0.88f;
    public float pressDownTime = 0.10f;
    public float releaseTime = 0.14f;
    public float pressPause = 0.70f;
    public float loopDelay = 0.50f;
    // press animation values same as chapter 1

    [Header("Hint Rules")]
    public int hintHideAfterPresses = 3;
    public bool showHintOnlyFirstTime = true;
    // hint disappears after 3 presses, and only shows on first study session

    Coroutine spaceHintCo;

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
        // make sure video is clean on start

        if (videoRawImage != null) videoRawImage.color = Color.black;
        // black to prevent flash before video loads

        if (videoRawImageObject != null) videoRawImageObject.SetActive(true);
        if (bgImageObject != null) bgImageObject.SetActive(true);
        // keep both active to avoid flash, video is black so bg shows through

        if (studyButton != null)
        {
            studyButton.onClick.RemoveAllListeners();
            studyButton.onClick.AddListener(OnStudyClicked);
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
        // add button listeners, remove first to avoid duplicates

        SetSpaceHintVisible(false);
        StopSpaceHintLoop();

        ShowHub(true);
        ShowVideo(false);
        // show hub choices, hide video on start
    }

    void Start()
    {
        var gs = Chapter1TwoGameState.Instance;
        if (gs != null && gs.returnedFromMiniGame)
        {
            gs.returnedFromMiniGame = false;
            StartCoroutine(ReturnFromMiniGameRoutine());
        }
        // if coming back from mini game, resume the choice logic
    }

    IEnumerator ReturnFromMiniGameRoutine()
    {
        yield return null;
        EndChoiceAndMaybeExam();
        // wait one frame then check if exam should trigger
    }

    // ---------------- Hub ----------------
    void OnStudyClicked()
    {
        if (isPlaying) return;

        var gs = Chapter1TwoGameState.Instance;
        if (gs != null) gs.AddStudyChoice(dayPerChoice, 1);
        // update game state with study choice before playing video

        StopAllCoroutines();
        StartCoroutine(PlayStudyTogether_PressRateSpeed());
    }

    void OnPlayClicked()
    {
        if (isPlaying) return;

        var gs = Chapter1TwoGameState.Instance;
        if (gs != null)
        {
            gs.AddPlayChoice(dayPerChoice);
            gs.ResetMiniGameRestartCount();
        }
        // update state and reset mini game restart count for fresh run

        StopAllCoroutines();
        StartCoroutine(PlayPeerInfluenceThenGoMiniGame());
    }

IEnumerator PlayPeerInfluenceThenGoMiniGame()
{
    isPlaying = true;
    ShowHub(false);

    if (!string.IsNullOrEmpty(peerInfluenceURL))
        yield return PlayUrlFull(peerInfluenceURL);

    // check if already hit max days before going to mini game
    var gs = Chapter1TwoGameState.Instance;
    if (gs != null && gs.day >= MAX_DAYS)
    {
        EndChoiceAndMaybeExam();
        yield break;
    }

    SceneManager.LoadScene(miniGameSceneName);
}

    // ---------------- StudyTogether ----------------
    IEnumerator PlayStudyTogether_PressRateSpeed()
    {
        isPlaying = true;
        ShowHub(false);

        yield return PrepareVideoNoBgFlash(studyTogetherURL);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;
        int pressCount = 0;

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();
        // start video at speed 0, player controls it by mashing space

        yield return WaitUntilVideoActuallyPlays(2f);

        var gs = Chapter1TwoGameState.Instance;
        bool already = (gs != null && gs.studyHintAlreadyShown);
        bool shouldShowHintThisPlay = !(showHintOnlyFirstTime && already);
        // check game state to decide if hint should show this time

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
        // fallback duration if length not loaded

        float currentSpeed = 0f;

        while (videoPlayer != null && videoPlayer.time < duration - endPadding)
        {
            bool pressedThisFrame = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (pressedThisFrame)
            {
                pressTimes.Enqueue(Time.unscaledTime);
                lastPressAt = Time.unscaledTime;
                // record press timestamp

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
            // remove old press times outside sample window

            float aps = (sampleWindowSeconds > 0.0001f) ? (pressTimes.Count / sampleWindowSeconds) : 0f;
            float t = Mathf.Clamp01(aps / maxPressesPerSecond);
            float targetSpeed = Mathf.Lerp(minPlaybackSpeed, maxPlaybackSpeed, t);
            // calculate target speed from press rate

            if (Time.unscaledTime - lastPressAt > stopAfterNoPressSeconds)
                targetSpeed = 0f;
            // stop video if player hasnt pressed for a while

            float lerpFactor = 1f - Mathf.Exp(-speedSmoothing * Time.unscaledDeltaTime);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpFactor);
            // smooth speed transition

            videoPlayer.playbackSpeed = currentSpeed;
            yield return null;
        }

        videoPlayer.playbackSpeed = 1f;
        StopSpaceHintLoop();
        SetSpaceHintVisible(false);
        // reset and clean up after video done

        if (gs != null && showHintOnlyFirstTime)
            gs.studyHintAlreadyShown = true;
        // mark hint as shown in game state so it doesnt appear again

        if (videoPlayer != null) videoPlayer.Stop();
        ShowVideo(false);

        EndChoiceAndMaybeExam();
    }

    // ---------------- End / Exam ----------------
    void EndChoiceAndMaybeExam()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        ShowVideo(false);

        var gs = Chapter1TwoGameState.Instance;
        int d = (gs != null) ? gs.day : 0;

        if (d >= MAX_DAYS)
        {
            StopAllCoroutines();
            StartCoroutine(PlayExamAndResolve());
        }
        else
        {
            ShowHub(true);
            isPlaying = false;
        }
        // check if max days reached, if yes trigger exam, otherwise return to hub
    }

    IEnumerator PlayExamAndResolve()
    {
        isPlaying = true;
        ShowHub(false);

        yield return PlayUrlFull(examVideoURL);
        // play exam video first

        var gs = Chapter1TwoGameState.Instance;
        int st = (gs != null) ? gs.studyTogetherCount : 0;
        int pc = (gs != null) ? gs.playCount : 0;

        bool pass = (st == 4 && pc == 3) || (st == 5 && pc == 2);
        // pass only with exact combo: 4 study + 3 play, or 5 study + 2 play
        // retrying mini game 3 times counts as extra play, so player cant just spam retry

        if (pass) yield return PlayUrlFull(successVideoURL);
        else yield return PlayUrlFull(failureVideoURL);
        // play result video

        ShowHub(false);
        ShowVideo(false);
        isPlaying = false;

        SceneManager.LoadScene(nextSceneName);
        // go to chapter 2 after result
    }

    // ---------------- Video helpers ----------------
    IEnumerator PrepareVideoNoBgFlash(string url)
    {
        if (videoPlayer == null || string.IsNullOrEmpty(url)) yield break;

        if (bgImageObject != null) bgImageObject.SetActive(false);
        if (videoRawImageObject != null) videoRawImageObject.SetActive(true);

        if (videoRawImage != null) videoRawImage.color = Color.black;
        // hide bg and show black video before loading to avoid flash

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.playbackSpeed = 1f;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        // wait until prepared before returning
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
        // wait for video time to actually move, confirms its playing not just buffering
    }

    IEnumerator PlayUrlFull(string url)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        yield return PrepareVideoNoBgFlash(url);

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        yield return WaitUntilVideoActuallyPlays(2f);

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;
        // wait for video to fully finish

        if (videoPlayer != null) videoPlayer.Stop();
        ShowVideo(false);
    }

    // ---------------- UI helpers ----------------
    void ShowVideo(bool show)
    {
        if (bgImageObject != null) bgImageObject.SetActive(!show);
        // showing video = hide bg, hiding video = show bg
    }

    void ShowHub(bool show)
    {
        SetCanvasGroup(optionStudyGroup, show);
        SetCanvasGroup(optionPlayGroup, show);
        // show or hide both choice buttons at same time
    }

    void SetCanvasGroup(CanvasGroup cg, bool show)
    {
        if (cg == null) return;
        cg.alpha = show ? 1f : 0f;
        cg.interactable = show;
        cg.blocksRaycasts = show;
        // reusable helper to toggle canvas group visibility and interactability
    }

    // ---------------- Space hint ----------------
    void SetSpaceHintVisible(bool show)
    {
        if (spaceHintRect != null && !spaceHintRect.gameObject.activeSelf)
            spaceHintRect.gameObject.SetActive(true);

        if (spaceHintGroup != null)
        {
            spaceHintGroup.alpha = show ? 1f : 0f;
            spaceHintGroup.blocksRaycasts = false;
            spaceHintGroup.interactable = false;
        }
        else
        {
            if (spaceHintRect != null) spaceHintRect.gameObject.SetActive(show);
        }
        // toggle hint visibility, never block raycasts
    }

    void StartSpaceHintLoop()
    {
        if (spaceHintRect == null) return;

        SetSpaceHintVisible(true);

        if (spaceHintCo != null) StopCoroutine(spaceHintCo);
        spaceHintCo = StartCoroutine(SpaceHintLoop());
        // stop old loop and start fresh
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
            // release phase

            yield return new WaitForSecondsRealtime(pressPause);
            yield return new WaitForSecondsRealtime(loopDelay);
            // pause between each animation cycle
        }
    }
}