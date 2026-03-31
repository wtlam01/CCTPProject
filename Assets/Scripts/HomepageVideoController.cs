using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class HomepageVideoController : MonoBehaviour
// controls the homepage video sequence, icon plays first then switches to looping homepage video
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("UI (show when homepage starts)")]
    public GameObject[] uiToShowOnHomepage;
    // all the UI elements to show after homepage video starts, like buttons etc

    [Header("Transition Cover (CanvasGroup on a full-screen black Image)")]
    public CanvasGroup blackCoverGroup;
    public float fadeOutToBlack = 0.12f;
    public float fadeInFromBlack = 0.25f;
    // black cover hides the flash between icon and homepage video switching

    [Header("URLs")]
    public string iconURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/Icon.mp4";
    public string homepageURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/homepage.mp4";
    // icon plays once at start, homepage loops after

    void Reset()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        // auto assign in editor when component first added
    }

    void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }
        // make sure video doesnt auto play on its own

        SetUIVisible(false);
        // hide all UI at start, show after homepage video begins

        if (blackCoverGroup != null)
        {
            blackCoverGroup.alpha = 1f;
            blackCoverGroup.blocksRaycasts = true;
            blackCoverGroup.interactable = true;
        }
        // black cover starts fully on to prevent any flash on first frame
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
        // subscribe to video end event, remove first to avoid duplicates

        yield return PlayURL(iconURL, loop: false);
        // prepare and play icon video

        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, 1f, 0f, fadeInFromBlack);
        // fade out black to reveal icon video
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (vp.url == iconURL)
            StartCoroutine(SwitchToHomepage_NoFlash());
        // when icon ends, switch to homepage
    }

    IEnumerator SwitchToHomepage_NoFlash()
    {
        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, blackCoverGroup.alpha, 1f, fadeOutToBlack);
        // quickly fade to black to hide the last frame of icon before switching

        yield return PlayURL(homepageURL, loop: true);
        // load and play homepage video on loop

        SetUIVisible(true);
        // show UI now that homepage is playing

        if (blackCoverGroup != null)
            yield return Fade(blackCoverGroup, 1f, 0f, fadeInFromBlack);
        // fade black out to reveal homepage
    }

    IEnumerator PlayURL(string url, bool loop)
    {
        videoPlayer.Stop();
        videoPlayer.isLooping = loop;
        videoPlayer.url = url;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        // wait until video is fully prepared before playing

        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    void SetUIVisible(bool visible)
    {
        if (uiToShowOnHomepage == null) return;
        foreach (var go in uiToShowOnHomepage)
            if (go != null) go.SetActive(visible);
        // loop through all UI objects and show or hide them
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }
        // if duration basically 0, just snap instantly

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
        // reusable fade function, lerps alpha over time
    }
}