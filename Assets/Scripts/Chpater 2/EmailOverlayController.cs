using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailOverlayController : MonoBehaviour
{
    [Header("Root Group")]
    public GameObject emailGroup;              // EmailGroup (整組 UI)
    public GameObject emailOverlayObject;      // EmailOverlay (Email2.png 那張 Image)

    [Header("Input")]
    public TMP_InputField inputField;          // TMP InputField
    public TMP_Text maskedDisplayText;         // 用嚟顯示 xxxxxx 的文字（建議用 InputField 入面嘅 Text Component）
    public bool useXMask = true;

    [Header("Send Button")]
    public Button sendButton;

    [Header("Slide Up Animation")]
    public float slideUpDistance = 900f;       // 往上滑幾多（UI 像素）
    public float slideDuration = 0.6f;
    public bool fadeOut = true;

    RectTransform overlayRect;
    CanvasGroup overlayCanvasGroup;

    string realTypedText = "";
    bool isAnimating = false;
    Vector2 overlayStartPos;

    void Awake()
    {
        if (emailGroup != null) emailGroup.SetActive(false);

        if (emailOverlayObject != null)
        {
            overlayRect = emailOverlayObject.GetComponent<RectTransform>();
            overlayCanvasGroup = emailOverlayObject.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
                overlayCanvasGroup = emailOverlayObject.AddComponent<CanvasGroup>();
        }

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);

        if (inputField != null)
        {
            // 每次輸入變更 -> 更新顯示
            inputField.onValueChanged.AddListener(OnInputChanged);
        }
    }

    void OnEnable()
    {
        ResetOverlayState();
    }

    public void ShowEmailUI()
    {
        if (emailGroup != null) emailGroup.SetActive(true);
        ResetOverlayState();
        FocusInput();
    }

    void ResetOverlayState()
    {
        isAnimating = false;
        realTypedText = "";

        if (inputField != null)
        {
            inputField.SetTextWithoutNotify("");
        }

        UpdateMaskedText("");

        if (overlayRect != null)
        {
            overlayStartPos = overlayRect.anchoredPosition;
            overlayRect.anchoredPosition = overlayStartPos;
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.blocksRaycasts = true;
            overlayCanvasGroup.interactable = true;
        }

        if (emailOverlayObject != null)
            emailOverlayObject.SetActive(true);
    }

    void FocusInput()
    {
        if (inputField == null) return;
        inputField.ActivateInputField();
        inputField.Select();
    }

    void OnInputChanged(string value)
    {
        if (isAnimating) return;

        // 保存真實輸入
        realTypedText = value;

        // 將 InputField 自己顯示嘅文字「遮罩化」
        UpdateMaskedText(realTypedText);
    }

    void UpdateMaskedText(string raw)
    {
        if (maskedDisplayText == null) return;

        if (!useXMask)
        {
            maskedDisplayText.text = raw;
            return;
        }

        // 例如輸入 5 個字 -> 顯示 xxxxx
        if (string.IsNullOrEmpty(raw))
        {
            maskedDisplayText.text = "";
            return;
        }

        maskedDisplayText.text = new string('x', raw.Length);
    }

    void OnSendClicked()
    {
        if (isAnimating) return;
        if (emailOverlayObject == null) return;

        // 你可以喺呢度檢查是否有輸入，例如空就唔俾 send
        // if (string.IsNullOrWhiteSpace(realTypedText)) return;

        StartCoroutine(SlideOverlayUp());
    }

    IEnumerator SlideOverlayUp()
    {
        isAnimating = true;

        if (overlayRect == null)
            yield break;

        Vector2 from = overlayRect.anchoredPosition;
        Vector2 to = from + Vector2.up * slideUpDistance;

        float t = 0f;
        float startAlpha = overlayCanvasGroup != null ? overlayCanvasGroup.alpha : 1f;

        // disable interaction while animating
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.blocksRaycasts = false;
            overlayCanvasGroup.interactable = false;
        }

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideDuration);

            overlayRect.anchoredPosition = Vector2.Lerp(from, to, p);

            if (fadeOut && overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, p);
            }

            yield return null;
        }

        overlayRect.anchoredPosition = to;

        if (fadeOut && overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = 0f;

        // 動畫完：直接隱藏 overlay（Email2）
        emailOverlayObject.SetActive(false);

        isAnimating = false;

        // 你要「overlay 上滑走後下一步」可以喺呢度觸發，例如：
        // OnEmailSent();
    }

    // 俾其他 script 讀取真實輸入（如你之後要存檔/顯示）
    public string GetRealTypedText()
    {
        return realTypedText;
    }
}