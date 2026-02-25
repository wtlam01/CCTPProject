using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class Chapter2Flow : MonoBehaviour
{
    [Header("UI Groups")]
    public CanvasGroup introGroup;     // IntroOverlay (BlackBG + ChapterText)
    public CanvasGroup sofaGroup;      // SofaImage
    public CanvasGroup videoGroup;     // VideoRawImage

    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Timing")]
    public float introHoldTime = 1.5f;
    public float introFadeDuration = 1.5f;
    public float sofaShowTime = 2f;

    [Header("Stop Times (seconds)")]
    public List<float> stopTimes = new List<float>
    {
        80f, 200f, 320f, 440f, 560f, 680f, 790f // 1:20, 3:20, 5:20, 7:20, 9:20, 11:20, 13:10
    };

    [Header("Swipe")]
    public float swipeThreshold = 100f;

    int stopIndex = 0;
    bool waitingForSwipe = false;
    Vector2 startPos;

    IEnumerator Start()
    {
        // 初始狀態
        SetGroup(introGroup, 1f, true);
        SetGroup(sofaGroup, 0f, false);
        SetGroup(videoGroup, 0f, false);

        // 1) Intro 停留
        yield return new WaitForSeconds(introHoldTime);

        // 2) Intro 淡出
        yield return FadeCanvasGroup(introGroup, 1f, 0f, introFadeDuration);
        SetGroup(introGroup, 0f, false);

        // 3) Sofa 顯示 2 秒
        SetGroup(sofaGroup, 1f, true);
        yield return new WaitForSeconds(sofaShowTime);
        SetGroup(sofaGroup, 0f, false);

        // 4) 顯示影片
        SetGroup(videoGroup, 1f, true);

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.Play();
    }

    void Update()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;

        // 到達 stop time -> pause
        if (!waitingForSwipe && stopIndex < stopTimes.Count)
        {
            if (videoPlayer.time >= stopTimes[stopIndex])
            {
                videoPlayer.Pause();
                waitingForSwipe = true;
            }
        }

        if (waitingForSwipe)
            HandleSwipe();
    }

    void HandleSwipe()
    {
        // 如果你 Player Settings 用新 Input System，Input.* 會報錯
        // 最快修法：Project Settings > Player > Active Input Handling 改為 Both
        // 或用下面新版 InputSystem（你想我可以給你 InputSystem 版）

        if (Input.GetMouseButtonDown(0))
            startPos = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            float deltaY = Input.mousePosition.y - startPos.y;

            if (deltaY > swipeThreshold)
            {
                waitingForSwipe = false;
                stopIndex++;
                videoPlayer.Play();
            }
        }
    }

    void SetGroup(CanvasGroup g, float alpha, bool enabled)
    {
        if (g == null) return;
        g.alpha = alpha;
        g.gameObject.SetActive(enabled);
        g.blocksRaycasts = enabled;
        g.interactable = enabled;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float duration)
    {
        if (g == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }
}