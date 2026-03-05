using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Chapter1TwoController : MonoBehaviour
{
    [Header("Scenes")]
    public string miniGameSceneName = "MiniGame";   // ✅ 你 runner scene 名
    public string returnSceneName = "Chapter1two";  // 一般唔使改

    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;   // Canvas/VideoRawImage
    public RawImage videoRawImage;           // optional (can be None)

    [Header("BG (show when video hidden)")]
    public GameObject bgImageObject;         // Canvas/BG_Image

    [Header("Hub UI")]
    public CanvasGroup optionStudyGroup;     // Canvas/Option_Study (CanvasGroup)
    public CanvasGroup optionPlayGroup;      // Canvas/Option_Play (CanvasGroup)
    public Button studyButton;
    public Button playButton;

    [Header("URLs")]
    public string studyTogetherURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/233Studying4ogether.mp4";
    public string peerInfluenceURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/232PeerInfluence1.mp4";

    [Header("Exam / Result URLs")]
    public string examVideoURL = "";        // fill in Inspector
    public string successVideoURL = "";     // optional
    public string failureVideoURL = "";     // optional

    [Header("System")]
    public int MAX_DAYS = 21;
    public int dayPerChoice = 3;
    public int requiredProgress = 4;

    [Header("Hidden Vars (NO UI)")]
    public int day = 0;
    public int progress = 0;

    bool isPlaying = false;

    // ----------------- Study: press-rate controls speed (Chapter1 style) -----------------
    [Header("StudyTogether: press rate -> playbackSpeed")]
    public float sampleWindowSeconds = 0.6f;
    public float maxPressesPerSecond = 8f;
    public float maxPlaybackSpeed = 5f;
    public float speedSmoothing = 10f;
    public float stopAfterNoPressSeconds = 0.25f;
    public float minPlaybackSpeed = 0f;
    public float endPadding = 0.05f;

    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;

    [Header("Space Hint")]
    public RectTransform spaceHintRect;      // Canvas/SpaceHint RectTransform
    public CanvasGroup spaceHintGroup;       // CanvasGroup on SpaceHint (recommended)
    public float hintShowDelay = 0.25f;

    [Header("Space Hint Animation")]
    public float pressDownScale = 0.88f;
    public float pressDownTime = 0.10f;
    public float releaseTime = 0.14f;
    public float pressPause = 0.70f;
    public float loopDelay = 0.50f;

    [Header("Hint Rules")]
    public int hintHideAfterPresses = 3;       // 第一次 Study：按 3 次先收埋 hint
    public bool showHintOnlyFirstTime = true;  // 第二次起唔再 show hint

    Coroutine spaceHintCo;
    bool studyHintAlreadyShown = false;

    // mini game return flag (static)
    static bool returnedFromMiniGame = false;

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

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        SetSpaceHintVisible(false);
        StopSpaceHintLoop();

        ShowHub(true);
    }

    void Start()
    {
        if (returnedFromMiniGame)
        {
            returnedFromMiniGame = false;
            StartCoroutine(ReturnFromMiniGameRoutine());
        }
    }

    IEnumerator ReturnFromMiniGameRoutine()
    {
        yield return null;
        EndChoiceAndMaybeExam();
    }

    // ---------------- Hub ----------------
    void OnStudyClicked()
    {
        if (isPlaying) return;

        day += dayPerChoice;
        progress += 1;

        StopAllCoroutines();
        StartCoroutine(PlayStudyTogether_PressRateSpeed());
    }

    void OnPlayClicked()
    {
        if (isPlaying) return;

        // ✅ day +3（Play）
        day += dayPerChoice;

        // ✅ 先播 peer influence video，再入 MiniGame
        StopAllCoroutines();
        StartCoroutine(PlayPeerInfluenceThenGoMiniGame());
    }

    // ✅ Play: peerInfluence video -> go minigame
    IEnumerator PlayPeerInfluenceThenGoMiniGame()
    {
        isPlaying = true;
        ShowHub(false);

        if (!string.IsNullOrEmpty(peerInfluenceURL))
            yield return PlayUrlFull(peerInfluenceURL);

        // 入 runner scene
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

        yield return WaitUntilVideoActuallyPlays(2f);

        bool shouldShowHintThisPlay = true;
        if (showHintOnlyFirstTime && studyHintAlreadyShown)
            shouldShowHintThisPlay = false;

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

        while (videoPlayer != null && videoPlayer.time < duration - endPadding)
        {
            bool pressedThisFrame =
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (pressedThisFrame)
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

        if (showHintOnlyFirstTime)
            studyHintAlreadyShown = true;

        if (videoPlayer != null) videoPlayer.Stop();
        ShowVideo(false);

        EndChoiceAndMaybeExam();
    }

    // ---------------- End / Exam ----------------
    void EndChoiceAndMaybeExam()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        ShowVideo(false);

        if (day >= MAX_DAYS)
        {
            StopAllCoroutines();
            StartCoroutine(PlayExamAndResolve());
        }
        else
        {
            ShowHub(true);
            isPlaying = false;
        }
    }

    IEnumerator PlayExamAndResolve()
    {
        isPlaying = true;
        ShowHub(false);

        if (string.IsNullOrEmpty(examVideoURL))
        {
            Debug.LogWarning("ExamVideoURL is empty (Chapter1two).");
            isPlaying = false;
            yield break;
        }

        yield return PlayUrlFull(examVideoURL);

        bool pass = (progress >= requiredProgress);

        if (pass && !string.IsNullOrEmpty(successVideoURL))
            yield return PlayUrlFull(successVideoURL);
        else if (!pass && !string.IsNullOrEmpty(failureVideoURL))
            yield return PlayUrlFull(failureVideoURL);

        ShowHub(false);
        ShowVideo(false);
        isPlaying = false;
    }

    // ---------------- Video helpers ----------------
    IEnumerator PrepareVideoNoBgFlash(string url)
    {
        if (videoPlayer == null || string.IsNullOrEmpty(url)) yield break;

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
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.Play();

        yield return WaitUntilVideoActuallyPlays(2f);

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        if (videoPlayer != null) videoPlayer.Stop();
        ShowVideo(false);
    }

    // ---------------- UI helpers ----------------
    void ShowVideo(bool show)
    {
        if (videoRawImageObject != null) videoRawImageObject.SetActive(show);
        if (bgImageObject != null) bgImageObject.SetActive(!show);
    }

    void ShowHub(bool show)
    {
        SetCanvasGroup(optionStudyGroup, show);
        SetCanvasGroup(optionPlayGroup, show);
    }

    void SetCanvasGroup(CanvasGroup cg, bool show)
    {
        if (cg == null) return;
        cg.alpha = show ? 1f : 0f;
        cg.interactable = show;
        cg.blocksRaycasts = show;
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

    // ---------------- MiniGame return helper ----------------
    public static void MarkReturnedFromMiniGame()
    {
        returnedFromMiniGame = true;
    }
}