using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ExitDoorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;

    [Header("UI References")]
    public Image doorImage;
    public CanvasGroup doorCanvasGroup;
    public GameObject emailGroupToHide; // optional
    public GameObject hideOnClick;      // optional extra hide

    [Header("Next Controller")]
    public SofaEmailController sofaEmailController;

    [Header("Timings")]
    public float openHoldSeconds = 0.15f;

    bool busy = false;

    void Reset()
    {
        doorImage = GetComponent<Image>();
        doorCanvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        if (doorCanvasGroup == null) doorCanvasGroup = GetComponent<CanvasGroup>();
        SetClosed();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorCanvasGroup != null && doorCanvasGroup.alpha <= 0.001f) return;
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetClosed();
    }

    void SetClosed()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
    }

    public void OnExitDoorClicked()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(ClickRoutine());
    }

    IEnumerator ClickRoutine()
    {
        if (doorCanvasGroup != null && doorCanvasGroup.alpha <= 0.001f)
        {
            busy = false;
            yield break;
        }

        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;

        if (openHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(openHoldSeconds);

        // ✅ 一撳 door：即刻收起兩個 button（唔等到下一個 coroutine）
        if (sofaEmailController != null)
            sofaEmailController.HideSofaButtonsImmediate();

        if (emailGroupToHide != null) emailGroupToHide.SetActive(false);
        if (hideOnClick != null) hideOnClick.SetActive(false);

        if (sofaEmailController != null)
            sofaEmailController.RequestExit();

        SetClosed();
        busy = false;
    }
}