using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoorButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;

    [Header("UI References")]
    public Image doorImage;
    public GameObject emailGroupToHide; // optional: hide your old EmailGroup overlay

    [Header("Next Controller")]
    public SofaEmailController sofaEmailController; // drag VideoController (with SofaEmailController) here

    [Header("Optional")]
    public GameObject hideOnClick; // if null, will hide this gameObject

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        if (doorImage != null && doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
    }

    public void OnDoorClicked()
    {
        // hide old email overlay group if you want
        if (emailGroupToHide != null)
            emailGroupToHide.SetActive(false);

        // start sofa + email button
        if (sofaEmailController != null)
            sofaEmailController.StartSofaMode();

        // hide door
        if (hideOnClick != null) hideOnClick.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null) doorImage.sprite = doorOpenSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
    }
}