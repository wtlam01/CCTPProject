using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Chapter1LoopManager : MonoBehaviour
{
    [Header("State (hidden from player)")]
    [SerializeField] int day = 1;            // 1..30
    [SerializeField] int progress = 0;       // hidden
    [SerializeField] int fatigue = 0;        // hidden

    [Header("UI (optional display, NOT countdown)")]
    public TMP_Text dayText;                 // can show subtle label, not "Day x/30"
    public CanvasGroup warningDimGroup;       // DimOverlay CanvasGroup
    public float warningDimAlpha = 0.35f;
    public float warningFadeTime = 0.35f;

    [Header("Blackout / System lock")]
    public CanvasGroup blackoutGroup;        // BlackFadeOverlay CanvasGroup
    public float blackoutFadeIn = 0.35f;
    public float blackoutHold = 0.7f;
    public float blackoutFadeOut = 0.35f;

    [Header("Thresholds")]
    public int warningMin = 6;               // 6..8 warning zone
    public int warningMax = 8;
    public int overworkThreshold = 10;       // >=10 triggers blackout
    public int overworkSkipDays = 5;
    public int overworkProgressPenalty = 4;
    public int overworkFatigueReset = 3;

    [Header("Result")]
    public CanvasGroup resultOverlay;        // optional
    public TMP_Text resultText;              // optional
    public int passProgress = 28;            // >=28 pass

    public bool IsBusy { get; private set; }

    void Awake()
    {
        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0;
            blackoutGroup.gameObject.SetActive(false);
            blackoutGroup.blocksRaycasts = false;
        }

        if (warningDimGroup != null)
        {
            warningDimGroup.alpha = 0;
            warningDimGroup.blocksRaycasts = false;
            warningDimGroup.interactable = false;
        }

        if (resultOverlay != null)
        {
            resultOverlay.alpha = 0;
            resultOverlay.gameObject.SetActive(false);
            resultOverlay.blocksRaycasts = false;
        }

        RefreshDayLabel();
        RefreshWarningVisual();
    }

    // ---------- Public API: called after each daily action ----------
    public void ApplyStudy()
    {
        // day +1, progress +2, fatigue +2
        AdvanceDay(1);
        progress += 2;
        fatigue += 2;
        AfterStatsChanged();
    }

    public void ApplyRest()
    {
        // day +1, progress +0, fatigue -1
        AdvanceDay(1);
        fatigue -= 1;
        AfterStatsChanged();
    }

    public void ApplyPlayAvoid()
    {
        // 你可以自訂：例如 progress -1 / fatigue -0 (or +1)
        // 重點係「不確定」：可以有少少隨機，但唔好顯示數值
        AdvanceDay(1);

        // option A：固定
        progress -= 1;
        fatigue += 0;

        // option B：少少不確定（可開啟）
        // int roll = Random.Range(0, 3); // 0,1,2
        // if (roll == 0) { progress += 1; fatigue += 1; }    // "短暫放鬆反而回復少少"
        // if (roll == 1) { progress -= 1; fatigue += 0; }    // "拖延"
        // if (roll == 2) { progress -= 2; fatigue += 1; }    // "壓力反彈"
        
        AfterStatsChanged();
    }

    void AfterStatsChanged()
    {
        ClampStats();
        RefreshDayLabel();
        RefreshWarningVisual();

        // if reached day 30 -> show results
        if (day >= 30)
        {
            day = 30;
            StartCoroutine(ShowResultsRoutine());
            return;
        }

        // if overwork -> blackout lock + auto skip
        if (fatigue >= overworkThreshold)
        {
            StartCoroutine(OverworkRoutine());
        }
    }

    void ClampStats()
    {
        if (fatigue < 0) fatigue = 0;
        if (progress < 0) progress = 0;
    }

    void AdvanceDay(int delta)
    {
        day += delta;
        if (day > 30) day = 30;
    }

    void RefreshDayLabel()
    {
        if (dayText == null) return;

        // ✅ 唔顯示 countdown：你可以用更「含糊」嘅文字
        // 例：只顯示「今日」/「又一日」/「第 X 日」都得，但唔寫 /30
        dayText.text = $"又一日"; 
        // 或者你想要 subtle：dayText.text = $"記錄 #{day}";
    }

    void RefreshWarningVisual()
    {
        if (warningDimGroup == null) return;

        bool inWarning = fatigue >= warningMin && fatigue <= warningMax;
        StopAllCoroutines(); // avoid stacking fades (simple)
        StartCoroutine(FadeCanvasGroup(warningDimGroup, warningDimGroup.alpha, inWarning ? warningDimAlpha : 0f, warningFadeTime));
    }

    IEnumerator OverworkRoutine()
    {
        if (IsBusy) yield break;
        IsBusy = true;

        // lock input visually
        if (blackoutGroup != null)
        {
            blackoutGroup.gameObject.SetActive(true);
            blackoutGroup.blocksRaycasts = true;
            yield return FadeCanvasGroup(blackoutGroup, 0f, 1f, blackoutFadeIn);
        }

        yield return new WaitForSecondsRealtime(blackoutHold);

        // system override
        AdvanceDay(overworkSkipDays);
        progress -= overworkProgressPenalty;
        fatigue = overworkFatigueReset;
        ClampStats();

        RefreshDayLabel();
        RefreshWarningVisual();

        if (blackoutGroup != null)
        {
            yield return FadeCanvasGroup(blackoutGroup, 1f, 0f, blackoutFadeOut);
            blackoutGroup.blocksRaycasts = false;
            blackoutGroup.gameObject.SetActive(false);
        }

        // if skip pushes to day30 -> results
        if (day >= 30)
        {
            day = 30;
            yield return ShowResultsRoutine();
        }

        IsBusy = false;
    }

    IEnumerator ShowResultsRoutine()
    {
        if (IsBusy) yield break;
        IsBusy = true;

        // optional blackout before results
        if (blackoutGroup != null)
        {
            blackoutGroup.gameObject.SetActive(true);
            blackoutGroup.blocksRaycasts = true;
            yield return FadeCanvasGroup(blackoutGroup, 0f, 1f, blackoutFadeIn);
        }

        // show results overlay
        if (resultOverlay != null)
        {
            resultOverlay.gameObject.SetActive(true);
            resultOverlay.blocksRaycasts = true;

            if (resultText != null)
            {
                bool pass = progress >= passProgress;
                resultText.text = pass ? "Results released\n\nYou made it." : "Results released\n\nNot this time.";
            }

            resultOverlay.alpha = 0f;
            yield return FadeCanvasGroup(resultOverlay, 0f, 1f, 0.35f);
        }

        // keep blackout behind, or fade out if you want
        // (你亦可以喺呢度 Load ending scene / show ending image)

        IsBusy = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        cg.alpha = from;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}