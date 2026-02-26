using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailOverlayController : MonoBehaviour
{
    [Header("Root Group (static)")]
    public RectTransform emailGroup;          // EmailGroup（唔郁）
    public CanvasGroup emailGroupCanvasGroup; // 可留空，自動抓

    [Header("Panel to animate (move this only)")]
    public RectTransform emailPanelObject;    // EmailPanel（要推上去嗰個）

    [Header("Input")]
    public TMP_InputField inputField;         // TMP InputField
    public TMP_Text maskedDisplayText;        // 你新建嘅 MaskedText
    public bool useXMask = true;
    public string maskText = "xxxxxx";

    [Header("Send Button")]
    public Button sendButton;

    [Header("Slide Up Animation (panel only)")]
    public float slideUpDistance = 900f;
    public float slideDuration = 0.6f;
    public bool fadeOutPanel = true;          // 淡出 EmailPanel（不是整個 EmailGroup）

    CanvasGroup panelCg;
    Vector2 panelStartPos;

    string realInput = "";
    bool sent = false;

    void Awake()
    {
        // Auto-assign
        if (emailGroup == null)
        {
            // 如果你係掛喺 EmailGroup 上，呢句就會抓到
            emailGroup = GetComponent<RectTransform>();
        }

        if (emailGroup != null && emailGroupCanvasGroup == null)
            emailGroupCanvasGroup = emailGroup.GetComponent<CanvasGroup>();

        // Panel CanvasGroup（用來 fade panel）
        if (emailPanelObject != null)
        {
            panelCg = emailPanelObject.GetComponent<CanvasGroup>();
            if (panelCg == null) panelCg = emailPanelObject.gameObject.AddComponent<CanvasGroup>();

            panelStartPos = emailPanelObject.anchoredPosition;
        }

        // Button wiring
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
            sendButton.onClick.AddListener(OnSendClicked);
        }

        // Input wiring
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnInputChanged);
            inputField.onValueChanged.AddListener(OnInputChanged);

            // 隱藏 InputField 自己顯示文字（保留可輸入）
            if (inputField.textComponent != null)
            {
                var c = inputField.textComponent.color;
                c.a = 0f;
                inputField.textComponent.color = c;
            }
        }

        UpdateMaskDisplay();
    }

    void OnEnable()
    {
        sent = false;

        // 確保 EmailGroup 可互動（打得入、按得到）
        if (emailGroupCanvasGroup != null)
        {
            emailGroupCanvasGroup.alpha = 1f;
            emailGroupCanvasGroup.blocksRaycasts = true;
            emailGroupCanvasGroup.interactable = true;
        }

        // Reset panel
        if (emailPanelObject != null)
        {
            emailPanelObject.anchoredPosition = panelStartPos;
            if (panelCg != null)
            {
                panelCg.alpha = 1f;
                panelCg.blocksRaycasts = true;
                panelCg.interactable = true;
            }
        }

        realInput = "";
        if (inputField != null) inputField.text = "";
        UpdateMaskDisplay();
    }

    void OnInputChanged(string current)
    {
        if (sent) return;

        // 保存真實輸入
        realInput = current;

        // 你要求：無論打咩都顯示固定 xxxxxx
        UpdateMaskDisplay();
    }

    void UpdateMaskDisplay()
    {
        if (maskedDisplayText == null) return;

        if (!useXMask)
        {
            maskedDisplayText.text = realInput;
            return;
        }

        // 你可改：即使冇輸入都顯示 xxxxxx
        maskedDisplayText.text = string.IsNullOrEmpty(realInput) ? "" : maskText;
    }

    public void OnSendClicked()
    {
        if (sent) return;
        sent = true;

        // Debug 真實輸入（需要先取消註解）
        // Debug.Log("Real input: " + realInput);

        StartCoroutine(SlideUpPanelAndHide());
    }

    IEnumerator SlideUpPanelAndHide()
    {
        if (emailPanelObject == null) yield break;

        Vector2 from = emailPanelObject.anchoredPosition;
        Vector2 to = from + Vector2.up * slideUpDistance;

        float t = 0f;
        float startAlpha = (panelCg != null) ? panelCg.alpha : 1f;

        // 動畫期間禁用 panel 互動
        if (panelCg != null)
        {
            panelCg.blocksRaycasts = false;
            panelCg.interactable = false;
        }

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideDuration);

            emailPanelObject.anchoredPosition = Vector2.Lerp(from, to, p);

            if (fadeOutPanel && panelCg != null)
                panelCg.alpha = Mathf.Lerp(startAlpha, 0f, p);

            yield return null;
        }

        emailPanelObject.anchoredPosition = to;
        if (fadeOutPanel && panelCg != null) panelCg.alpha = 0f;

        // 你如果要「EmailBG 留低」，就唔好 SetActive(false)
        // emailPanelObject.gameObject.SetActive(false);
    }
}