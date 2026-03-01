using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class Chapter1LoopManager : MonoBehaviour
{
    [Header("State (hidden from player)")]
    public int Day = 1;
    public int Progress = 0;
    public int Fatigue = 0;

    [Header("UI (optional display, NOT countdown)")]
    public TMP_Text dayText;                     // optional
    public CanvasGroup warningDimGroup;          // DimOverlay CanvasGroup
    public float warningDimAlpha = 0.35f;
    public float warningFadeTime = 0.35f;

    [Header("Blackout / System lock")]
    public CanvasGroup blackoutGroup;            // BlackFadeOverlay CanvasGroup
    public float blackoutFadeIn = 0.35f;
    public float blackoutHold = 0.7f;
    public float blackoutFadeOut = 0.35f;

    [Header("Thresholds")]
    public int warningMin = 6;
    public int warningMax = 8;

    public int overworkThreshold = 10;
    public int overworkSkipDays = 5;
    public int overworkProgressLoss = 4;
    public int overworkResetFatigue = 3;

    [Header("Result")]
    public int finalDay = 30;
    public int passProgress = 28;
    public CanvasGroup resultOverlay;            // optional
    public TMP_Text resultText;                  // optional

    [Header("Video Core (shared VideoPlayer)")]
    public VideoPlayer videoPlayer;

    [Header("Video URLs")]
    public string overworkVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/21Fire.mp4";
    public string successVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/25academicsuccess.mp4";
    public string failureVideoURL  = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/26Failure.mp4";

    public bool IsBusy => _busy;
    bool _busy;

    Coroutine _dimCo;

    void Start()
    {
        ApplyWarningDim();
        UpdateDayText();
        HideCanvasGroup(blackoutGroup, instant:true);
        HideCanvasGroup(resultOverlay, instant:true);
    }

    public void AddDay(int v)      { Day += v; ClampState(); UpdateDayText(); }
    public void AddProgress(int v) { Progress += v; ClampState(); }
    public void AddFatigue(int v)  { Fatigue += v; ClampState(); ApplyWarningDim(); }

    public void SetFatigue(int v)  { Fatigue = v; ClampState(); ApplyWarningDim(); }
    public void SetDay(int v)      { Day = v; ClampState(); UpdateDayText(); }

    public void ClampState()
    {
        if (Day < 1) Day = 1;
        if (Progress < 0) Progress = 0;
        if (Fatigue < 0) Fatigue = 0;
        if (Day > finalDay) Day = finalDay;
    }

    void UpdateDayText()
    {
        if (dayText != null) dayText.text = Day.ToString();
    }

    public void ApplyWarningDim()
    {
        if (warningDimGroup == null) return;

        bool inWarning = (Fatigue >= warningMin && Fatigue <= warningMax);
        float targetA = inWarning ? warningDimAlpha : 0f;

        if (_dimCo != null) StopCoroutine(_dimCo);
        _dimCo = StartCoroutine(FadeCanvasGroup(warningDimGroup, targetA, warningFadeTime));
    }

    // 每次玩家做完一日選擇後呼叫
    public void AfterDailyChoice(System.Action onFinished)
    {
        if (_busy) return;
        StartCoroutine(AfterChoiceRoutine(onFinished));
    }

    IEnumerator AfterChoiceRoutine(System.Action onFinished)
    {
        _busy = true;

        // Day 30 result 優先
        if (Day >= finalDay)
        {
            yield return ResultRoutine();
            // result 後保持 busy（鎖死）
            yield break;
        }

        // Overwork
        if (Fatigue >= overworkThreshold)
        {
            yield return OverworkRoutine();
        }

        _busy = false;
        onFinished?.Invoke();
    }

    IEnumerator OverworkRoutine()
    {
        // blackout overlay
        if (blackoutGroup != null)
        {
            yield return FadeCanvasGroup(blackoutGroup, 1f, blackoutFadeIn);
            yield return new WaitForSecondsRealtime(blackoutHold);
        }

        // play fire video
        yield return PlayUrlAndWait(overworkVideoURL);

        // apply rules
        Day += overworkSkipDays;
        Progress -= overworkProgressLoss;
        if (Progress < 0) Progress = 0;
        Fatigue = overworkResetFatigue;

        ClampState();
        ApplyWarningDim();
        UpdateDayText();

        // fade out blackout
        if (blackoutGroup != null)
        {
            yield return FadeCanvasGroup(blackoutGroup, 0f, blackoutFadeOut);
        }

        // 如果 overwork 直接跳到 day30
        if (Day >= finalDay)
        {
            yield return ResultRoutine();
        }
    }

    IEnumerator ResultRoutine()
    {
        // optional overlay
        if (resultOverlay != null)
        {
            ShowCanvasGroup(resultOverlay, 1f, instant:true);
        }

        if (resultText != null)
        {
            resultText.text = "Results released";
        }

        // play success/failure
        if (Progress >= passProgress)
        {
            yield return PlayUrlAndWait(successVideoURL);
        }
        else
        {
            yield return PlayUrlAndWait(failureVideoURL);
        }

        // result 後保持停留（你可加 restart/menu）
        _busy = true;
    }

    IEnumerator PlayUrlAndWait(string url)
    {
        if (videoPlayer == null || string.IsNullOrEmpty(url)) yield break;

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.playbackSpeed = 1f;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.Play();
        while (videoPlayer.isPlaying) yield return null;

        yield return null;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float time)
    {
        if (cg == null) yield break;
        float start = cg.alpha;
        if (time <= 0.0001f)
        {
            cg.alpha = target;
            cg.blocksRaycasts = target > 0.001f;
            cg.interactable = target > 0.001f;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / time);
            cg.alpha = Mathf.Lerp(start, target, p);
            yield return null;
        }

        cg.alpha = target;
        cg.blocksRaycasts = target > 0.001f;
        cg.interactable = target > 0.001f;
    }

    void HideCanvasGroup(CanvasGroup cg, bool instant)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    void ShowCanvasGroup(CanvasGroup cg, float alpha, bool instant)
    {
        if (cg == null) return;
        cg.alpha = alpha;
        cg.blocksRaycasts = alpha > 0.001f;
        cg.interactable = alpha > 0.001f;
    }
}