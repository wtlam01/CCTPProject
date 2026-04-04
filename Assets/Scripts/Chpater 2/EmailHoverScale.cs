// 控制email按鈕hover時嘅縮放效果，滑鼠移上去會smoothly放大，移走就縮返
// This script handles a smooth scale effect on the email button, lerping up on hover and back on exit.

using UnityEngine;
using UnityEngine.EventSystems;
// importing event system so we can detect mouse hover

public class EmailHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// this script handles the scaling effect when hovering over the email button

{
    public float hoverScale = 1.1f;
    // how much it scales up on hover
    public float speed = 10f;
    // how fast the scale transition is, higher = snappier

    Vector3 originalScale;
    Vector3 targetScale;
    // storing the original size so we can go back to it after hover

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        // save the starting scale before anything changes
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * speed
        );
        // lerp smoothly moves current scale towards target scale every frame, makes it feel less snappy
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        // mouse on = scale up
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        // mouse off = back to normal size
    }
}