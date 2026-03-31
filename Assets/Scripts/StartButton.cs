using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// simple hover scale, no lerp this time, just instant snap
{
    public float hoverScale = 1.05f;
    private Vector3 originalScale;
    // only 5% bigger on hover, quite subtle

    void Start()
    {
        originalScale = transform.localScale;
        // save original scale on first frame
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScale;
        // instantly scale up on hover, no smooth lerp like the other scripts
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        // snap back to normal on mouse exit
    }
}