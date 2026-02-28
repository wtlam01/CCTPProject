using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class HomepageVideoController : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("UI (show when homepage starts)")]
    public GameObject[] uiToShowOnHomepage;

    [Header("Transition Cover (CanvasGroup on a full-screen black Image)")]
    public CanvasGroup blackCoverGroup;
    public float fadeOutToBlack = 0.12f;   // 遮住最後一幀閃
    public float fadeInFromBlack = 0.25f;

    [Header("URLs")]
    public string iconURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/Icon.mp4";
    public string homepageURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/homepage.mp4";

    void Reset()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
    }

    void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }

        SetUIVisible(false);

        if (blackCoverGroup != null)
        {
            blackCoverGroup.alpha = 1f;          // 一開始先黑住，避免任何 flash
            blackCoverGroup.blocksRaycasts = true;
            blackCoverGroup.interactable = true;
        }
    }

    IEnumerator Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[HomepageVideoController] VideoPlayer not assigned.");
            yield break;
        }

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        // 先播 icon
        yield return PlayURL(iconURL, loop: false);

        // icon 開始後，淡出黑幕（顯示 icon）
        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, 1f, 0f, fadeInFromBlack);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (vp.url == iconURL)
            StartCoroutine(SwitchToHomepage_NoFlash());
    }

    IEnumerator SwitchToHomepage_NoFlash()
    {
        // 1) 先淡入黑幕遮住最後一幀
        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, blackCoverGroup.alpha, 1f, fadeOutToBlack);

        // 2) 換 homepage + loop
        yield return PlayURL(homepageURL, loop: true);

        // 3) 顯示 UI
        SetUIVisible(true);

        // 4) 淡出黑幕
        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, 1f, 0f, fadeInFromBlack);
    }

    IEnumerator PlayURL(string url, bool loop)
    {
        videoPlayer.Stop();
        videoPlayer.isLooping = loop;
        videoPlayer.url = url;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    void SetUIVisible(bool visible)
    {
        if (uiToShowOnHomepage == null) return;
        foreach (var go in uiToShowOnHomepage)
            if (go != null) go.SetActive(visible);
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        cg.alpha = to;
    }
}