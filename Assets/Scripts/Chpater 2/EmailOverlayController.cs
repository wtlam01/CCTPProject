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

    [Tooltip("如果 true：打幾多字就顯示幾多個 x（保留換行）")]
    public bool maskMatchLength = true;

    [Tooltip("如果 false：固定顯示 maskText（例如 xxxxxx）")]
    public string maskText = "xxxxxx";

    [Header("True caret settings (TMP real cursor)")]
    public bool useTrueCaret = true;
    public Color caretColor = Color.black;
    public int caretWidth = 2;

    [Header("Optional caret blink (fake cursor)")]
    public bool blinkCaret = false;                  // 建議關（你要真 caret）
    public string caretChar = "|";
    public float caretBlinkSpeed = 0.5f;

    [Header("Send Button")]
    public Button sendButton;

    [Header("Slide Up Animation (panel only)")]
    public float slideUpDistance = 900f;
    public float slideDuration = 0.6f;
    public bool fadeOutPanel = true;

    [Header("After Send -> Show Door")]
    public GameObject doorButtonObject;              // 拖 DoorButton
    public GameObject sofaImageObject;               // 拖 SofaImage
    public GameObject videoRawImageObject;           // (可選) 拖 VideoRawImage

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

        // ✅ Door 一開始一定要收埋
        if (doorButtonObject != null) doorButtonObject.SetActive(false);

        // ✅ Sofa 一開始唔好出（除非你想）
        if (sofaImageObject != null) sofaImageObject.SetActive(false);

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
            sendButton.onClick.AddListener(OnSendClicked);
        }

        if (inputField != null)
        {
            // ✅ 你想真游標可以換行：InputField 要 MultiLineNewline
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            inputField.onValueChanged.RemoveListener(OnInputChanged);
            inputField.onValueChanged.AddListener(OnInputChanged);

            // 隱藏 InputField 自己顯示文字（保留可輸入）
            if (inputField.textComponent != null)
            {
                var c = inputField.textComponent.color;
                c.a = 0f;
                inputField.textComponent.color = c;
            }

            // 真 caret
            if (useTrueCaret)
            {
                inputField.customCaretColor = true;
                inputField.caretColor = caretColor;     // alpha 要 1
                inputField.caretWidth = caretWidth;

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

        // EmailGroup 可互動
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

        // Door 每次 Email 出現都先收埋
        if (doorButtonObject != null) doorButtonObject.SetActive(false);

        realInput = "";
        if (inputField != null) inputField.text = "";

        // ✅ Email 出現就嘗試 focus（Editor 通常 OK；WebGL 可能仍需要玩家先點一下頁面）
        StartCoroutine(AutoFocusInputNextFrame());

        StopCaret();
        if (blinkCaret) caretRoutine = StartCoroutine(CaretBlinkLoop());
        else UpdateMaskDisplay(false);
    }

    void OnDisable()
    {
        StopCaret();
    }

    IEnumerator AutoFocusInputNextFrame()
    {
        yield return null;

        if (inputField == null) yield break;

        inputField.interactable = true;
        inputField.Select();
        inputField.ActivateInputField();

        // 再補一次
        yield return null;
        inputField.Select();
        inputField.ActivateInputField();
    }

    void OnInputChanged(string current)
    {
        if (sent) return;

        realInput = current;
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

        StartCoroutine(SlideUpPanelAndThenShowDoor());
    }

    IEnumerator SlideUpPanelAndThenShowDoor()
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

        // ✅ 推走 panel 後：顯示 Door
        if (doorButtonObject != null) doorButtonObject.SetActive(true);

        // ✅ 令 Door click 一定有效：保證佢係可點（CanvasGroup 無 block 住）
        // （EmailGroup 仲存在，但 DoorButton 喺 EmailGroup 之外，你而家 hierarchy 係 ok）
    }

    // ✅ 給 DoorButton 的 Button OnClick 呼叫（最穩）
    public void GoToSofa()
    {
        // 顯示 sofa
        if (sofaImageObject != null) sofaImageObject.SetActive(true);

        // 關掉影片（可選）
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);

        // 收埋 Door
        if (doorButtonObject != null) doorButtonObject.SetActive(false);

        // 收埋 Email 整組（你想 EmailBG 留低就改成只關 EmailPanel）
        if (emailGroup != null) emailGroup.gameObject.SetActive(false);
    }
}