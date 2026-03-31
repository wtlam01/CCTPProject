using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// need event system for the hover detect, same as before

public class ExitDoorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// handles the exit door, hover sprite swap + click to trigger exit sequence
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // same as the other door, two sprites swapping on hover

    [Header("UI References")]
    public Image doorImage;
    public CanvasGroup doorCanvasGroup;
    public GameObject emailGroupToHide;
    public GameObject hideOnClick;
    // canvasgroup lets us check if door is visible before doing anything

    [Header("Next Controller")]
    public SofaEmailController sofaEmailController;
    // reference to sofa controller so we can call exit and hide buttons from here

    [Header("Timings")]
    public float openHoldSeconds = 0.15f;
    // small delay so the open sprite shows briefly before things hide, feels more natural

    bool busy = false;
    // prevent double clicking

    void Reset()
    {
        doorImage = GetComponent<Image>();
        doorCanvasGroup = GetComponent<CanvasGroup>();
        // auto assign in editor when component first added
    }

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        if (doorCanvasGroup == null) doorCanvasGroup = GetComponent<CanvasGroup>();
        SetClosed();
        // grab components if not assigned, then default to closed sprite
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorCanvasGroup != null && doorCanvasGroup.alpha <= 0.001f) return;
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // only swap to open if door is actually visible, skip if alpha is basically 0
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetClosed();
        // mouse leaves = back to closed sprite
    }

    void SetClosed()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
        // reusable helper so i dont repeat the same null check everywhere
    }

    public void OnExitDoorClicked()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(ClickRoutine());
        // if already clicked once, ignore. otherwise start the exit coroutine
    }

    IEnumerator ClickRoutine()
    {
        if (doorCanvasGroup != null && doorCanvasGroup.alpha <= 0.001f)
        {
            busy = false;
            yield break;
        }
        // if door is invisible when clicked somehow, cancel and reset busy

        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // show open sprite on click

        if (openHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(openHoldSeconds);
        // wait a tiny bit so player sees the door open before everything changes

        if (sofaEmailController != null)
            sofaEmailController.HideSofaButtonsImmediate();
        // hide sofa buttons straight away, dont wait for the rest

        if (emailGroupToHide != null) emailGroupToHide.SetActive(false);
        if (hideOnClick != null) hideOnClick.SetActive(false);
        // hide any other UI that shouldnt be visible during exit

        if (sofaEmailController != null)
            sofaEmailController.RequestExit();
        // tell sofa controller to actually do the exit transition

        SetClosed();
        busy = false;
        // reset door sprite and unlock for next time
    }
}