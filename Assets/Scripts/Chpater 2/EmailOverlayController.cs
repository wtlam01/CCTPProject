// script 控制 email overlay 互動流程：

// 一開始（OnEnable）：
// 1. 顯示 email overlay（可互動）
// 2. 重置 panel 位置同透明度
// 3. 清空 input field
// 4. 自動 focus 輸入框（方便玩家即刻開始打字）
// 5. 隱藏 door button 同 sofa

// 當玩家輸入文字：
// 6. 將真實輸入儲存喺 realInput
// 7. 將顯示文字轉為 X mask（保留換行）
// 8. 隱藏真實文字，只顯示 masked text

// 當按下 Send：
// 9. 停止 cursor（caret）動畫
// 10. 開始 panel 向上滑動動畫（slide up）
// 11. 同時可選擇 fade out panel

// Panel 動畫期間：
// 12. 禁用 panel interaction（避免誤觸）
// 13. 使用 Lerp 將 panel 向上移動（Vector2.up）
// 14. 同步將透明度由 1 → 0（如果有 fade）

// 動畫完成後：
// 15. 顯示 door button（進入下一步 interaction）

// Door 之後：
// 16. GoToSofa() 被呼叫 → 顯示 sofa，隱藏 email + video + door

// This script controls the full email overlay interaction flow:

// On enable:
// 1. Show the email overlay (interactive)
// 2. Reset panel position and visibility
// 3. Clear the input field
// 4. Auto-focus the input field for immediate typing
// 5. Hide the door button and sofa

// On text input:
// 6. Store the real input in realInput
// 7. Convert the displayed text into an X mask (preserving line breaks)
// 8. Hide the real text and only show the masked version

// On send click:
// 9. Stop the caret animation
// 10. Start the panel slide-up animation
// 11. Optionally fade out the panel

// During animation:
// 12. Disable interaction on the panel
// 13. Move the panel upward using Lerp
// 14. Fade alpha from 1 → 0 (if enabled)

// After animation:
// 15. Show the door button (transition to next interaction)

// After door interaction:
// 16. GoToSofa() → shows sofa, hides email, video, and door

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// importing the usual stuff, TMPro is for the text mesh pro input field and text

public class EmailOverlayController : MonoBehaviour
// this whole script controls the email overlay, the typing, masking, send animation and what happens after
{
    [Header("Root Group (static)")]
    public RectTransform emailGroup;
    public CanvasGroup emailGroupCanvasGroup;
    // emailGroup is the whole email UI, canvasgroup lets us control alpha and raycast blocking

    [Header("Panel to animate (move this only)")]
    public RectTransform emailPanelObject;
    // this is the part that slides up when send is clicked, not the whole group

    [Header("Input")]
    public TMP_InputField inputField;
    public TMP_Text maskedDisplayText;
    public bool useXMask = true;
    // inputField is where the player types, maskedDisplayText shows the x version on top

    [Tooltip("如果 true：打幾多字就顯示幾多個 x（保留換行）")]
    public bool maskMatchLength = true;
    // if true the x count matches how many characters typed

    [Tooltip("如果 false：固定顯示 maskText（例如 xxxxxx）")]
    public string maskText = "xxxxxx";
    // fallback fixed mask text if maskMatchLength is off

    [Header("True caret settings (TMP real cursor)")]
    public bool useTrueCaret = true;
    public Color caretColor = Color.black;
    public int caretWidth = 2;
    // using the real TMP caret instead of a fake blinking one, looks more natural

    [Header("Optional caret blink (fake cursor)")]
    public bool blinkCaret = false;
    public string caretChar = "|";
    public float caretBlinkSpeed = 0.5f;
    // fake blinking cursor option, kept it off since we using real caret

    [Header("Send Button")]
    public Button sendButton;
    // the button that triggers the send animation

    [Header("Slide Up Animation (panel only)")]
    public float slideUpDistance = 900f;
    public float slideDuration = 0.6f;
    public bool fadeOutPanel = true;
    // controls how far and how fast the panel slides up, and whether it fades while sliding

    [Header("After Send -> Show Door")]
    public GameObject doorButtonObject;
    public GameObject sofaImageObject;
    public GameObject videoRawImageObject;
    // after email is sent, door appears, sofa shows, video hides

    CanvasGroup panelCg;
    Vector2 panelStartPos;

    string realInput = "";
    bool sent = false;

    Coroutine caretRoutine;
    // storing the real typed text separately so we can mask it

    void Awake()
    {
        if (emailGroup == null) emailGroup = GetComponent<RectTransform>();
        if (emailGroup != null && emailGroupCanvasGroup == null)
            emailGroupCanvasGroup = emailGroup.GetComponent<CanvasGroup>();
        // auto grab components if i forgot to drag them in

        if (emailPanelObject != null)
        {
            panelCg = emailPanelObject.GetComponent<CanvasGroup>();
            if (panelCg == null) panelCg = emailPanelObject.gameObject.AddComponent<CanvasGroup>();
            panelStartPos = emailPanelObject.anchoredPosition;
        }
        // save the panel starting position so we know where to slide from

        if (doorButtonObject != null) doorButtonObject.SetActive(false);
        if (sofaImageObject != null) sofaImageObject.SetActive(false);
        // hide door and sofa at start, they show up later after send

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
            sendButton.onClick.AddListener(OnSendClicked);
        }
        // add send button listener, remove first to avoid double calling

        if (inputField != null)
        {
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            // allow multiline input so enter key works

            inputField.onValueChanged.RemoveListener(OnInputChanged);
            inputField.onValueChanged.AddListener(OnInputChanged);

            if (inputField.textComponent != null)
            {
                var c = inputField.textComponent.color;
                c.a = 0f;
                inputField.textComponent.color = c;
            }
            // hide the real input text so only the masked version shows

            if (useTrueCaret)
            {
                inputField.customCaretColor = true;
                inputField.caretColor = caretColor;
                inputField.caretWidth = caretWidth;

                var sel = inputField.selectionColor;
                sel.a = 0f;
                inputField.selectionColor = sel;
            }
            // set up the real caret color and hide selection highlight
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
        // make sure email group is fully visible and interactable when it appears

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
        // reset panel position and visibility every time email shows up

        if (doorButtonObject != null) doorButtonObject.SetActive(false);
        // always hide door when email opens

        realInput = "";
        if (inputField != null) inputField.text = "";
        // clear the input field each time

        StartCoroutine(AutoFocusInputNextFrame());
        // auto focus the input so player can type straight away

        StopCaret();
        if (blinkCaret) caretRoutine = StartCoroutine(CaretBlinkLoop());
        else UpdateMaskDisplay(false);
    }

    void OnDisable()
    {
        StopCaret();
        // stop the fake caret when object disabled
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
        // doing it twice bc sometimes the first one doesnt stick, especially in webgl
    }

    void OnInputChanged(string current)
    {
        if (sent) return;

        realInput = current;
        UpdateMaskDisplay(false);
        // every time input changes, update the masked display
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
        // loop through each character, keep newlines as is, replace everything else with x

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
        // toggles fake caret on and off at set interval, stops when email is sent
    }

    void StopCaret()
    {
        if (caretRoutine != null)
        {
            StopCoroutine(caretRoutine);
            caretRoutine = null;
        }
        // stops the coroutine cleanly
    }

    public void OnSendClicked()
    {
        if (sent) return;
        sent = true;

        StopCaret();
        UpdateMaskDisplay(false);

        StartCoroutine(SlideUpPanelAndThenShowDoor());
        // when send clicked, stop caret and start the slide up animation
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
        // disable interaction on panel while its animating

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideDuration);

            emailPanelObject.anchoredPosition = Vector2.Lerp(from, to, p);

            if (fadeOutPanel && panelCg != null)
                panelCg.alpha = Mathf.Lerp(startAlpha, 0f, p);

            yield return null;
        }
        // lerp the panel upward and fade it out at the same time each frame

        emailPanelObject.anchoredPosition = to;
        if (fadeOutPanel && panelCg != null) panelCg.alpha = 0f;
        // snap to final position just in case lerp didnt fully reach

        if (doorButtonObject != null) doorButtonObject.SetActive(true);
        // show the door after panel finishes sliding up
    }

    public void GoToSofa()
    {
        if (sofaImageObject != null) sofaImageObject.SetActive(true);
        if (videoRawImageObject != null) videoRawImageObject.SetActive(false);
        if (doorButtonObject != null) doorButtonObject.SetActive(false);
        if (emailGroup != null) emailGroup.gameObject.SetActive(false);
        // called by door button, shows sofa, hides door and email group
    }
}