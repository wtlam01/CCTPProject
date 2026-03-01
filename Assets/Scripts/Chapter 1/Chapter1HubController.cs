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

    [Header("Optional UI Hint")]
    public GameObject spaceHintObject;

    bool isPlaying = false;
    bool chatLocked = false;

    readonly Queue<float> pressTimes = new Queue<float>();
    float lastPressAt = -999f;

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
        if (spaceHintObject != null) spaceHintObject.SetActive(false);

        if (studyButton != null)
        {
            studyButton.onClick.RemoveAllListeners();
            studyButton.onClick.AddListener(OnStudyClicked);
        }

        if (coffeeButton != null)
        {
            coffeeButton.onClick.RemoveAllListeners();
            coffeeButton.onClick.AddListener(OnCoffeeClicked);
        }

        // ✅ IMPORTANT: do NOT show hub in Awake
        ForceHideAllOptions();
    }

    IEnumerator Start()
    {
        // ✅ IMPORTANT: first frame hide again (prevents 1-frame flash)
        yield return null;
        ForceHideAllOptions();

        // 如果你想「一開始只 show Study + Chat」，可以咁：
        ShowHub(show: true);
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
        chatLocked = true;
        StartCoroutine(PlayCoffeeRoutine());
    }

    IEnumerator PlayStudyRateControlsSpeedRoutine()
    {
        isPlaying = true;
        ShowHub(false);

        yield return PrepareVideo(studyVideoURL);
        ShowVideo(true);

        pressTimes.Clear();
        lastPressAt = Time.unscaledTime;

        if (spaceHintObject != null) spaceHintObject.SetActive(true);

        videoPlayer.time = 0;
        videoPlayer.playbackSpeed = 0f;
        videoPlayer.Play();

        double duration = videoPlayer.length;
        if (duration <= 0.01) duration = 12.0;

        float currentSpeed = 0f;

        while (videoPlayer.time < duration - endPadding)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                pressTimes.Enqueue(Time.unscaledTime);
                lastPressAt = Time.unscaledTime;
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
        if (spaceHintObject != null) spaceHintObject.SetActive(false);

        ShowVideo(false);
        ShowHub(true);

        isPlaying = false;
    }

    IEnumerator PlayCoffeeRoutine()
    {
        isPlaying = true;
        ShowHub(false);

        yield return PrepareVideo(coffeeVideoURL);
        ShowVideo(true);

        videoPlayer.playbackSpeed = 1f;
        videoPlayer.time = 0;
        videoPlayer.Play();

        while (videoPlayer != null && videoPlayer.isPlaying) yield return null;

        ShowVideo(false);
        ShowHub(true);

        isPlaying = false;
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

    // ✅ show hub: BEFORE first choice -> show Chat + Study only (hide Coffee)
    // ✅ after first choice -> show Study + Coffee only (Chat locked)
    void ShowHub(bool show)
    {
        if (show)
        {
            if (!chatLocked)
            {
                ShowOption(chatOptionGroup, chatButton, true);
                ShowOption(studyOptionGroup, studyButton, true);
                ShowOption(coffeeOptionGroup, coffeeButton, false); // ✅ hide coffee at beginning
            }
            else
            {
                ShowOption(chatOptionGroup, chatButton, false);
                ShowOption(studyOptionGroup, studyButton, true);
                ShowOption(coffeeOptionGroup, coffeeButton, true);
            }
        }
        else
        {
            ForceHideAllOptions();
        }
    }

    void ForceHideAllOptions()
    {
        ForceHide(chatOptionGroup, chatButton);
        ForceHide(studyOptionGroup, studyButton);
        ForceHide(coffeeOptionGroup, coffeeButton);
    }

    void ShowOption(CanvasGroup g, Button b, bool show)
    {
        if (g != null)
        {
            g.gameObject.SetActive(show); // ✅ key: prevents flash
            g.alpha = show ? 1f : 0f;
            g.interactable = show;
            g.blocksRaycasts = show;
        }
        if (b != null) b.interactable = show;
    }

    void ForceHide(CanvasGroup g, Button b)
    {
        if (g != null)
        {
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;
            g.gameObject.SetActive(false); // ✅ key: prevents flash
        }
        if (b != null) b.interactable = false;
    }
}