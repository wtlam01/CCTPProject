using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// basic imports, event system for hover detection

public class ExitDoorControllerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// this is a simpler version, only handles the hover sprite swap, no click logic here
{
    [Header("Refs")]
    public Image doorImage;
    // the image component on the door

    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // two sprites to swap between on hover

    void Reset()
    {
        doorImage = GetComponent<Image>();
        // auto grab image when first added in editor
    }

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        SetClosed();
        // make sure it starts as closed sprite
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // mouse hover on = open door sprite
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetClosed();
        // mouse leaves = back to closed
    }

    void SetClosed()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
        // helper function so i dont repeat the null check every time
    }
}