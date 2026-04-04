//控制按鈕hover時嘅縮放效果，滑鼠移上去會即刻放大5%，移走就即刻縮返
// This script handles a simple instant scale effect on hover, no smooth lerp, just a quick snap up and back.

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