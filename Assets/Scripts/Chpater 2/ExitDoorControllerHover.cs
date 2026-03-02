using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ExitDoorControllerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    public Image doorImage;

    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;

    void Reset()
    {
        doorImage = GetComponent<Image>();
    }

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        SetClosed();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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
}