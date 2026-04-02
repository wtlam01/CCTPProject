using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class SegmentedVideoSwipe_NewInput : MonoBehaviour
// this script controls the whole video sequence, swipe to continue, fade transitions, and what shows after
{
    [Header("Intro UI (Chapter Two)")]
    public CanvasGroup introOverlayGroup;
    public float introHoldTime = 1.5f;
    public float introFadeDuration = 1.5f;
    // the chapter title overlay that shows at the start, holds then fades out

    [Header("Sofa / Video UI")]
    public GameObject sofaImage;
    public GameObject videoRawImage;
    public VideoPlayer videoPlayer;
    public float sofaShowTime = 2f;
    // sofa shows briefly before the video starts, gives it a nice transition feel

    [Header("Swipe Hint UI (optional)")]
    public GameObject swipeHintText;
    public SwipeHintAnimator fingerAnimator;
    public GameObject fingerHintFallback;
    // hint UI to tell player to swipe up, only shows on first stop

    [Header("Fade To Black Overlay (between videos)")]
    public CanvasGroup blackFadeGroup;
    public float fadeFromBlackDuration = 0.8f;
    // black overlay used for transitions between videos

    [Header("Second -> Third Fade")]
    [Tooltip("第二條片最後幾秒開始淡出（要 2 秒）")]
    public float secondFadeOutLastSeconds = 2f;
    [Tooltip("淡出到全黑用幾耐（通常(?)同上面一樣 2 秒）")]
    public float fadeToBlackDuration = 2f;
    // second video fades to black near the end before switching to third

    [Header("After Third Video")]
    public GameObject emailImage;
    // after the last video finishes, show the email UI

    [Header("Video URLs")]
    public string preFirstVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/30IgNotice.mp4";
    public string firstVideoURL  = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/32Scrollingthephone.mp4";
    public string secondVideoURL = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/322StressOverload.mp4";
    public string thirdVideoURL  = "https://wtlam01.github.io/Poppion_CCTPUnityProject/videos/311street1.mp4";
    // four videos in order, pre-first plays once then goes into the swipeable first video

    [Header("Stop Times (seconds) for FIRST video only")]
    public List<double> stopTimes = new List<double>
    {
        1.8, 3.8, 5.8, 7.8, 9.8, 11.8, 13.4
    };
    // these are the timestamps where the first video pauses and waits for a swipe

    [Header("Swipe Settings")]
    public float swipeThreshold = 120f;
    // minimum drag distance to count as a swipe, stops accidental clicks triggering it

    int stopIndex = 0;
    bool waitingForSwipe = false;
    // tracks which stop we are at and whether we are waiting for player input

    Vector2 startPos;
    bool isPressing = false;
    // stores where the mouse press started so we can measure swipe distance

    bool hasShownHintOnce = false;
    bool switchingVideos = false;
    // hasShownHintOnce so hint only appears on first pause, switchingVideos prevents double triggers

    Coroutine secondWatcherCo;
    // reference to the coroutine watching the second video so we can stop it if needed

    void Awake()
    {
        if (introOverlayGroup != null)
        {
            introOverlayGroup.alpha = 1f;
            introOverlayGroup.blocksRaycasts = true;
            introOverlayGroup.interactable = true;
        }
        // intro starts fully visible and blocks everything underneath

        if (blackFadeGroup != null)
        {
            blackFadeGroup.alpha = 1f;
            blackFadeGroup.blocksRaycasts = true;
            blackFadeGroup.interactable = true;
        }
        // black overlay also starts fully on so first reveal is a fade in

        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(false);
        // hide both until the sequence starts

        SetTextHintVisible(false);
        HideFinger();

        if (emailImage != null) emailImage.SetActive(false);
        // hide email until after third video
    }

    IEnumerator Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.waitForFirstFrame = true;
        }
        // subscribe to video end event, remove first to avoid duplicate listeners

        if (introOverlayGroup != null)
        {
            yield return new WaitForSeconds(introHoldTime);
            yield return FadeCanvasGroup(introOverlayGroup, 1f, 0f, introFadeDuration);
            introOverlayGroup.blocksRaycasts = false;
            introOverlayGroup.interactable = false;
        }
        // hold intro then fade it out

        if (sofaImage != null) sofaImage.SetActive(true);
        if (videoRawImage != null) videoRawImage.SetActive(false);
        SetTextHintVisible(false);
        HideFinger();

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, 0.6f);
        // fade out black to reveal sofa image

        yield return new WaitForSeconds(sofaShowTime);
        // let sofa sit for a moment

        if (sofaImage != null) sofaImage.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(true);
        // swap sofa out for the video screen

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned.");
            yield break;
        }

        stopIndex = 0;
        waitingForSwipe = false;
        hasShownHintOnce = false;
        switchingVideos = false;

        yield return StartCoroutine(PlayPreFirstThenFirst());
        // start the video sequence, notice first then scrolling phone
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;
        if (switchingVideos) return;

        bool isFirstVideo = (videoPlayer.url == firstVideoURL);
        if (!isFirstVideo) return;
        // swipe stops only apply to the first video, other videos play straight through

        if (!waitingForSwipe && stopIndex < stopTimes.Count)
        {
            if (videoPlayer.time >= stopTimes[stopIndex])
            {
                videoPlayer.Pause();
                waitingForSwipe = true;

                if (!hasShownHintOnce)
                {
                    ShowFinger();
                    SetTextHintVisible(true);
                }
                else
                {
                    HideFinger();
                    SetTextHintVisible(false);
                }
            }
        }
        // check each frame if we hit a stop time, pause and show hint on first one only

        if (waitingForSwipe)
            HandleSwipe_NewInputSystem();
    }

    void HandleSwipe_NewInputSystem()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            isPressing = true;
            startPos = mouse.position.ReadValue();
        }
        // record where the press started

        if (isPressing && mouse.leftButton.wasReleasedThisFrame)
        {
            isPressing = false;
            Vector2 endPos = mouse.position.ReadValue();
            float deltaY = endPos.y - startPos.y;

            if (deltaY >= swipeThreshold)
            {
                waitingForSwipe = false;

                if (!hasShownHintOnce) hasShownHintOnce = true;

                HideFinger();
                SetTextHintVisible(false);

                stopIndex++;
                videoPlayer.Play();
            }
        }
        // on release check if dragged far enough upward, if yes advance to next segment
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (switchingVideos) return;

        if (vp.url == preFirstVideoURL)
        {
            StartCoroutine(SwitchPreFirstToFirst());
            return;
        }
        // notice finished, move to scrolling phone

        if (vp.url == firstVideoURL)
        {
            if (stopIndex < stopTimes.Count) return;
            StartCoroutine(SwitchToSecondVideo());
            return;
        }
        // first video only continues to second if all swipe stops are done

        if (vp.url == thirdVideoURL)
        {
            StartCoroutine(ShowEmailAfterThird());
            return;
        }

        if (vp.url == secondVideoURL)
        {
            if (secondWatcherCo == null)
                StartCoroutine(SwitchSecondToThird_Fallback());
            return;
        }
        // fallback in case the watcher coroutine missed the transition
    }

    IEnumerator PlayPreFirstThenFirst()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        yield return PlayVideoCovered(preFirstVideoURL, fadeFromBlack: true);
        // play the notice video covered by black then fade in

        switchingVideos = false;
    }

    IEnumerator SwitchPreFirstToFirst()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        stopIndex = 0;
        hasShownHintOnce = false;
        // reset swipe tracking for the first video

        yield return PlayVideoCovered(firstVideoURL, fadeFromBlack: true);
        // now play the swipeable scrolling phone video

        switchingVideos = false;
    }

    IEnumerator SwitchToSecondVideo()
    {
        switchingVideos = true;

        waitingForSwipe = false;
        isPressing = false;
        HideFinger();
        SetTextHintVisible(false);

        yield return PlayVideoCovered(secondVideoURL, fadeFromBlack: true);

        if (secondWatcherCo != null) StopCoroutine(secondWatcherCo);
        secondWatcherCo = StartCoroutine(WatchSecondAndFadeToThird());
        // start watching the second video so we can fade to third near the end

        switchingVideos = false;
    }

    IEnumerator WatchSecondAndFadeToThird()
    {
        double length = videoPlayer.length;
        float timeout = 3f;
        while ((length <= 0.1 || double.IsNaN(length)) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            length = videoPlayer.length;
            yield return null;
        }
        // wait until video length is properly loaded, timeout after 3 seconds just in case

        if (length <= 0.1 || double.IsNaN(length))
        {
            secondWatcherCo = null;
            yield break;
        }
        // if still couldnt get length, give up and let fallback handle it

        double fadeStartTime = System.Math.Max(0.0, length - secondFadeOutLastSeconds);

        while (videoPlayer.isPlaying && videoPlayer.time < fadeStartTime)
            yield return null;
        // wait until we reach the point where we should start fading

        switchingVideos = true;

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);
        // fade to black over the last few seconds of second video

        yield return PlayVideoCovered(thirdVideoURL, fadeFromBlack: true);

        switchingVideos = false;
        secondWatcherCo = null;
    }

    IEnumerator SwitchSecondToThird_Fallback()
    {
        switchingVideos = true;

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, fadeToBlackDuration);

        yield return PlayVideoCovered(thirdVideoURL, fadeFromBlack: true);
        // backup path if watcher coroutine wasnt running when second video ended

        switchingVideos = false;
    }

    IEnumerator ShowEmailAfterThird()
    {
        switchingVideos = true;

        if (videoPlayer != null) videoPlayer.Stop();

        if (blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 1f, 0.8f);
        // quick fade to black before showing email

        if (emailImage != null)
            emailImage.SetActive(true);
        // show email UI after all videos done

        switchingVideos = false;
    }

    IEnumerator PlayVideoCovered(string url, bool fadeFromBlack)
    {
        if (videoPlayer == null) yield break;

        if (blackFadeGroup != null)
            blackFadeGroup.alpha = 1f;
        // make sure black is on before loading new video

        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        // wait for video to fully prepare before playing

        videoPlayer.time = 0;
        videoPlayer.Play();

        float t = 0f;
        while (videoPlayer.texture == null && t < 2f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // wait for first frame to be ready, max 2 seconds

        if (fadeFromBlack && blackFadeGroup != null)
            yield return FadeCanvasGroup(blackFadeGroup, blackFadeGroup.alpha, 0f, fadeFromBlackDuration);
        // fade black out to reveal video
    }

    void SetTextHintVisible(bool visible)
    {
        if (swipeHintText != null) swipeHintText.SetActive(visible);
        // toggle the swipe hint text
    }

    void ShowFinger()
    {
        if (fingerAnimator != null)
        {
            fingerAnimator.gameObject.SetActive(true);
            return;
        }
        if (fingerHintFallback != null) fingerHintFallback.SetActive(true);
        // show animated finger if available, otherwise use fallback image
    }

    void HideFinger()
    {
        if (fingerAnimator != null) fingerAnimator.gameObject.SetActive(false);
        if (fingerHintFallback != null) fingerHintFallback.SetActive(false);
        // hide both versions of the finger hint
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float t = 0f;
        cg.alpha = from;

        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }
        // if duration is basically 0, just snap instantly

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
        // reusable fade function used everywhere in this script, lerps alpha over time
    }
}