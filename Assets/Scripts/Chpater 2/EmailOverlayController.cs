using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailOverlayController : MonoBehaviour
{
    [Header("Root Group (static)")]
    public RectTransform emailGroup;                 // EmailGroup（唔郁）
    public CanvasGroup emailGroupCanvasGroup;        // 可留空，自動抓

    [Header("Panel to animate (move this only)")]
    public RectTransform emailPanelObject;           // EmailPanel（要推上去嗰個）

    [Header("Input")]
    public TMP_InputField inputField;                // TMP InputField
    public TMP_Text maskedDisplayText;               // MaskedText（你新建嘅 TMP_Text）
    public bool useXMask = true;

    [Tooltip("如果 true：打幾多字就顯示幾多個 x")]
    public bool maskMatchLength = true;

    [Tooltip("如果 false：固定顯示 maskText（例如 xxxxxx）")]
    public string maskText = "xxxxxx";

    [Header("True caret settings (TMP real cursor)")]
    public bool useTrueCaret = true;
    public Color caretColor = Color.black;
    public int caretWidth = 2;

    [Header("Optional caret blink (fake cursor)")]
    public bool blinkCaret = false;                  // 想要游標閃就開（建議關，因為你要真 caret）
    public string caretChar = "|";
    public float caretBlinkSpeed = 0.5f;

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

    Coroutine caretRoutine;

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

            // ✅ 仍然隱藏 InputField 自己顯示文字（保留可輸入）
            // （但真 caret 會因為透明而可能消失，所以要配合下面 useTrueCaret）
            if (inputField.textComponent != null)
            {
                var c = inputField.textComponent.color;
                c.a = 0f; // 你想完全唔見真字
                inputField.textComponent.color = c;
            }

            // ✅ 真 caret：強制用 customCaretColor（多數情況下就算文字透明都仍可見 caret）
            if (useTrueCaret)
            {
                inputField.customCaretColor = true;
                inputField.caretColor = caretColor;     // alpha 要 1
                inputField.caretWidth = caretWidth;

                // 可選：避免 selection 藍底干擾（你唔想見到就設透明）
                var sel = inputField.selectionColor;
                sel.a = 0f;
                inputField.selectionColor = sel;
            }
        }

        UpdateMaskDisplay(false);
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
        if (inputField != null) inputField.text = "";

        // ✅ 重點：Email 出現就自動 focus InputField
        StartCoroutine(AutoFocusInputNextFrame());

        // caret blink（假 |）
        StopCaret();
        if (blinkCaret)
            caretRoutine = StartCoroutine(CaretBlinkLoop());
        else
            UpdateMaskDisplay(false);
    }

    void OnDisable()
    {
        StopCaret();
    }

    IEnumerator AutoFocusInputNextFrame()
    {
        // 等一 frame，確保 UI 已經 active & EventSystem ready
        yield return null;

        if (inputField == null) yield break;

        inputField.interactable = true;

        // Select + ActivateInputField 先可以唔 click 就打到字
        inputField.Select();
        inputField.ActivateInputField();

        // 有時會失焦，再補一次
        yield return null;
        inputField.Select();
        inputField.ActivateInputField();
    }

    void OnInputChanged(string current)
    {
        if (sent) return;

        realInput = current;

        // 一輸入就更新
        UpdateMaskDisplay(false);
    }

    void UpdateMaskDisplay(bool caretOn)
{
    if (maskedDisplayText == null) return;

    if (!useXMask)
    {
        maskedDisplayText.text = realInput + (caretOn ? caretChar : "");
        return;
    }

    string masked;

    if (maskMatchLength)
    {
        // ✅ 保留換行，其他字變 x
        System.Text.StringBuilder sb = new System.Text.StringBuilder(realInput.Length);
        for (int i = 0; i < realInput.Length; i++)
        {
            char ch = realInput[i];
            if (ch == '\n' || ch == '\r')
                sb.Append(ch);
            else
                sb.Append('x');
        }
        masked = sb.ToString();
    }
    else
    {
        // 固定顯示 maskText（如果你想多行都固定顯示，呢個模式唔建議）
        masked = string.IsNullOrEmpty(realInput) ? "" : maskText;
    }

    maskedDisplayText.text = masked + (caretOn ? caretChar : "");
}

    IEnumerator CaretBlinkLoop()
    {
        bool caretOn = false;

        while (!sent)
        {
            caretOn = !caretOn;
            UpdateMaskDisplay(caretOn);
            yield return new WaitForSecondsRealtime(caretBlinkSpeed);
        }
    }

    void StopCaret()
    {
        if (caretRoutine != null)
        {
            StopCoroutine(caretRoutine);
            caretRoutine = null;
        }
    }

    public void OnSendClicked()
    {
        if (sent) return;
        sent = true;

        StopCaret();
        UpdateMaskDisplay(false);

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

        // 只推走 panel，EmailBG 會留低
        // emailPanelObject.gameObject.SetActive(false);
    }
}