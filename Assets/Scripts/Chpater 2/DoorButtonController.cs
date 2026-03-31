using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoorButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // two sprites for the door, one closed one open, swap them on hover

    [Header("UI References")]
    public Image doorImage;
    public GameObject emailGroupToHide;
    // doorImage is the actual UI image component, emailGroupToHide i can leave empty if dont need

    [Header("Next Controller")]
    public SofaEmailController sofaEmailController;
    // drag the sofa controller here so the door knows what to trigger next

    [Header("Optional")]
    public GameObject hideOnClick; // if null, will hide this gameObject
    // this lets me choose which object to hide when clicked, more flexible

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        // if i forgot to drag doorImage in inspector, it tries grab it itself
        if (doorImage != null && doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
        // set default sprite to closed door at the start
    }

    public void OnDoorClicked()
    {
        if (emailGroupToHide != null)
            emailGroupToHide.SetActive(false);
        // hide the old email overlay first before switching scene

        if (sofaEmailController != null)
            sofaEmailController.StartSofaMode();
        // tell sofa controller to start, this is where the next part begins

        if (hideOnClick != null) hideOnClick.SetActive(false);
        else gameObject.SetActive(false);
        // hide the door after clicking, either specific object or just itself
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null) doorImage.sprite = doorOpenSprite;
        // mouse hover on = show open door sprite
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
        // mouse hover off = back to closed, simple swap
    }
}