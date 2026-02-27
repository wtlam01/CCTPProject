using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SofaEmailController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum State { Sofa, CheckEmail }

    [Header("Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject;     // 你的 VideoRawImage (GameObject)

    [Header("Optional: disable other flow script while sofa/email runs")]
    public MonoBehaviour flowScriptToDisable;  // 例如 SegmentedVideoSwipe_NewInput

    [Header("URLs")]
    public string sofaVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/3OnSofa.mp4";
    public string checkEmailURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/32CheckEmail.mp4";

    [Header("Email Icon UI")]
    public GameObject emailButtonObject;   // ✅ 直接控制 SetActive
    public Button emailButton;             // ✅ Click
    public Image emailButtonImage;         // ✅ 換 sprite 做 hover
    public Sprite emailNormal;
    public Sprite emailHover;

    [Header("Check Email Pause & Scroll")]
    public float pauseAtSeconds = 1f;      // 停在 1 秒
    public float scrollThreshold = 20f;    // scroll down 多大才算
    public bool requireScrollDown = true;  // 只接受向下

    State state = State.Sofa;
    bool waitingForScroll = false;
    Coroutine pauseCo;

    void Reset()
    {
        // 方便你一拖上去就有機會自動抓到
        if (emailButtonObject == null) emailButtonObject = gameObject;
    }

    void Awake()
    {
        // 綁 video end
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // 綁 click（你就算唔設 Button OnClick，都會 work）
        if (emailButton != null)
        {
            emailButton.onClick.RemoveListener(OnEmailClicked);
            emailButton.onClick.AddListener(OnEmailClicked);
        }

        // 初始 hover sprite
        SetEmailHover(false);
    }

void Start()
{
    // 測試用：一入場就開 sofa + email icon
    StartSofaMode();
}

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    void Update()
    {
        if (!waitingForScroll) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scrollY = mouse.scroll.ReadValue().y;

        // 大多數平台：scroll down = 負數
        bool isScrollDown = scrollY < -scrollThreshold;
        bool isScrollUp = scrollY > scrollThreshold;

        if (requireScrollDown)
        {
            if (isScrollDown)
            {
                waitingForScroll = false;
                if (videoPlayer != null) videoPlayer.Play();
            }
        }
        else
        {
            if (isScrollDown || isScrollUp)
            {
                waitingForScroll = false;
                if (videoPlayer != null) videoPlayer.Play();
            }
        }
    }

    // ✅ 給你從 Door / 或任何地方 call：開始 sofa loop + 顯示 email icon
    public void StartSofaMode()
    {
        state = State.Sofa;
        waitingForScroll = false;

        if (flowScriptToDisable != null)
            flowScriptToDisable.enabled = false;

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        ShowEmailButton(true);

        PlayUrl(sofaVideoURL, loop: true);
    }

    // ✅ Button dropdown 都可以用（記得 public）
    public void OnEmailClicked()
    {
        if (state != State.Sofa) return;

        state = State.CheckEmail;
        waitingForScroll = false;

        // 播 check email 時先收埋 icon（你想保留都得，改為 true）
        ShowEmailButton(false);

        PlayUrl(checkEmailURL, loop: false);

        if (pauseCo != null) StopCoroutine(pauseCo);
        pauseCo = StartCoroutine(PauseAtTimeThenWaitScroll());
    }

    IEnumerator PauseAtTimeThenWaitScroll()
    {
        if (videoPlayer == null) yield break;

        // 等 prepare
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();

        // 等到 pauseAtSeconds
        while (videoPlayer.time < pauseAtSeconds)
            yield return null;

        videoPlayer.Pause();
        waitingForScroll = true;
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
        {
            // sofa：prepare 完就自動播
            StartCoroutine(PlayWhenPrepared());
        }
        // checkEmail：由 PauseAtTimeThenWaitScroll() 控制播/停
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
        {
            emailButton.interactable = show;
        }

        // reset hover
        SetEmailHover(false);
    }

    // ===== Hover support (requires EventSystem + Graphic Raycaster) =====
    public void OnPointerEnter(PointerEventData eventData) => SetEmailHover(true);
    public void OnPointerExit(PointerEventData eventData) => SetEmailHover(false);

    public void SetEmailHover(bool hover)
    {
        if (emailButtonImage == null) return;

        if (hover && emailHover != null) emailButtonImage.sprite = emailHover;
        else if (!hover && emailNormal != null) emailButtonImage.sprite = emailNormal;
    }
}