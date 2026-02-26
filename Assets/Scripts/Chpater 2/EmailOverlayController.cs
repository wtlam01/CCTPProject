using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailOverlayController : MonoBehaviour
{
    [Header("Root Group (static)")]
    public RectTransform emailGroup;
    public CanvasGroup emailGroupCanvasGroup;

    [Header("Panel to animate (move this only)")]
    public RectTransform emailPanelObject;

    [Header("Input")]
    public TMP_InputField inputField;
    public TMP_Text maskedDisplayText;
    public bool useXMask = true;

    [Tooltip("true: 打幾多字就顯示幾多個 x")]
    public bool maskMatchLength = true;

    [Tooltip("false: 固定顯示 maskText（例如 xxxxxx）")]
    public string maskText = "xxxxxx";

    [Header("Send Button")]
    public Button sendButton;

    [Header("Slide Up Animation (panel only)")]
    public float slideUpDistance = 900f;
    public float slideDuration = 0.6f;
    public bool fadeOutPanel = true;

    CanvasGroup panelCg;
    Vector2 panelStartPos;

    string realInput = "";
    bool sent = false;

    bool suppressCallback = false;

    void Awake()
    {
        if (emailGroup == null) emailGroup = GetComponent<RectTransform>();
        if (emailGroup != null && emailGroupCanvasGroup == null)
            emailGroupCanvasGroup = emailGroup.GetComponent<CanvasGroup>();

        if (emailPanelObject != null)
        {
            panelCg = emailPanelObject.GetComponent<CanvasGroup>();
            if (panelCg == null) panelCg = emailPanelObject.gameObject.AddComponent<CanvasGroup>();
            panelStartPos = emailPanelObject.anchoredPosition;
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
            sendButton.onClick.AddListener(OnSendClicked);
        }

        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnInputChanged);
            inputField.onValueChanged.AddListener(OnInputChanged);

            // ✅ 重要：保留 caret 可見，所以唔好把 textComponent alpha 設 0
            // 反而我哋會用「清空顯示文字」方法，只留 caret
            inputField.text = "";
        }

        UpdateMaskDisplay();
    }

    void OnEnable()
    {
        sent = false;

        if (emailGroupCanvasGroup != null)
        {
            emailGroupCanvasGroup.alpha = 1f;
            emailGroupCanvasGroup.blocksRaycasts = true;
            emailGroupCanvasGroup.interactable = true;
        }

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

        if (inputField != null)
        {
            suppressCallback = true;
            inputField.text = "";             // 顯示文字清空
            suppressCallback = false;
        }

        UpdateMaskDisplay();

        // ✅ Email 一出現就自動 focus
        StartCoroutine(AutoFocusInputNextFrame());
    }

    IEnumerator AutoFocusInputNextFrame()
    {
        yield return null;

        if (inputField == null) yield break;

        inputField.interactable = true;
        inputField.Select();
        inputField.ActivateInputField();

        yield return null;
        inputField.Select();
        inputField.ActivateInputField();
    }

    void OnInputChanged(string current)
    {
        if (sent || suppressCallback) return;

        // current 係 InputField 入面顯示緊嘅字
        // 我哋要「真實輸入」存去 realInput，但 InputField 顯示要清空（留 caret）

        realInput = current;

        // 清空 InputField 顯示文字，但唔影響 realInput
        suppressCallback = true;
        inputField.text = "";
        inputField.caretPosition = 0;        // caret 留喺開頭（你想喺尾都得）
        suppressCallback = false;

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

        if (maskMatchLength)
        {
            maskedDisplayText.text = new string('x', realInput.Length);
        }
        else
        {
            maskedDisplayText.text = string.IsNullOrEmpty(realInput) ? "" : maskText;
        }
    }

    public void OnSendClicked()
    {
        if (sent) return;
        sent = true;

        StartCoroutine(SlideUpPanelAndHide());
    }

    IEnumerator SlideUpPanelAndHide()
    {
        if (emailPanelObject == null) yield break;

        Vector2 from = emailPanelObject.anchoredPosition;
        Vector2 to = from + Vector2.up * slideUpDistance;

        float t = 0f;
        float startAlpha = (panelCg != null) ? panelCg.alpha : 1f;

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
    }
}