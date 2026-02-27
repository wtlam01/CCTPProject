using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class SofaEmailController : MonoBehaviour
{
    public enum State { Sofa, CheckEmail }

    [Header("Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;     // VideoRawImage (GameObject)

    [Header("Optional: disable other flow script while this runs")]
    public MonoBehaviour flowScriptToDisable;  // 例如 SegmentedVideoSwipe_NewInput

    [Header("URLs")]
    public string sofaVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/3OnSofa.mp4";
    public string checkEmailURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32CheckEmail.mp4";

    [Header("Email Button UI")]
    public GameObject emailButtonObject;   // EmailButton (整個物件)
    public Button emailButton;             // EmailButton 上嘅 Button component

    [Header("Hover Scale (optional)")]
    public RectTransform emailButtonRect;  // EmailButton 的 RectTransform
    public float hoverScale = 1.12f;
    public float hoverScaleSpeed = 12f;

    [Header("Check Email Pause + Drag Gate (Hint2 = drag down like Hint1)")]
    public float pauseAtSeconds = 1f;

    [Tooltip("要按住左鍵拖幾多 pixels（累積）先算完成")]
    public float dragAccumulation = 140f;

    [Tooltip("true = 必須向下拖 (deltaY < 0); false = 任何方向都算")]
    public bool requireDragDown = true;

    [Tooltip("拖拉時必須按住左鍵")]
    public bool requireLeftMouseHeld = true;

    [Header("Finger Hint 2 (Drag Down)")]
    public SwipeHintAnimator fingerHintDown; // 拖 FingerHint2 (有 SwipeHintAnimator) 入嚟

    State state = State.Sofa;

    bool waitingForDrag = false;
    float dragSum = 0f;
    Vector2 lastMousePos;
    bool hasLastMousePos = false;

    Vector3 baseScale;
    bool isHovering = false;

    Coroutine pauseCo;

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

        ShowEmailButton(false);
        if (fingerHintDown != null) fingerHintDown.StopAndHide();
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

        // 等 drag 續播
        if (!waitingForDrag) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // 必須按住左鍵先計 drag
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

        // ✅ 向下拖：delta.y < 0
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
                if (videoPlayer != null) videoPlayer.Play();
            }
        }
    }

    // ✅ 由 DoorClick 之後 call 呢個：開始 sofa + 顯示 email icon
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

        ShowEmailButton(true);

        PlayUrl(sofaVideoURL, loop: true);

        if (fingerHintDown != null) fingerHintDown.StopAndHide();
    }

    public void OnEmailClicked()
    {
        if (state != State.Sofa) return;

        state = State.CheckEmail;

        waitingForDrag = false;
        dragSum = 0f;
        hasLastMousePos = false;

        ShowEmailButton(false); // 播 email video 時收埋 icon

        PlayUrl(checkEmailURL, loop: false);

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

        // 顯示「向下拖」finger hint
        if (fingerHintDown != null) fingerHintDown.ShowAndPlay();

        waitingForDrag = true;
        dragSum = 0f;
        hasLastMousePos = false;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // check email 播完 → 回 sofa
        if (state == State.CheckEmail)
        {
            StartSofaMode();
        }
    }

    void PlayUrl(string url, bool loop)
    {
        if (videoPlayer == null) return;

        videoPlayer.Stop();
        videoPlayer.isLooping = loop;
        videoPlayer.url = url;
        videoPlayer.Prepare();

        if (loop)
            StartCoroutine(PlayWhenPrepared());
    }

    IEnumerator PlayWhenPrepared()
    {
        if (videoPlayer == null) yield break;
        while (!videoPlayer.isPrepared) yield return null;
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    void ShowEmailButton(bool show)
    {
        if (emailButtonObject != null)
            emailButtonObject.SetActive(show);

        if (emailButton != null)
            emailButton.interactable = show;

        isHovering = false;
        if (emailButtonRect != null)
            emailButtonRect.localScale = baseScale;
    }

    // ===== 俾 UI EventTrigger 用（Pointer Enter/Exit）=====
    public void UI_OnPointerEnter() => isHovering = true;
    public void UI_OnPointerExit() => isHovering = false;
}