// 控制send按鈕hover時嘅縮放效果，同email按鈕一樣原理，smoothly放大再縮返
// This script handles a smooth hover scale effect on the send button, lerping up on mouse enter and back on exit.

using UnityEngine;
using UnityEngine.EventSystems;
// event system needed for the hover detection

public class SendButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// same hover scale effect as the email button, just applied to the send button
{
    public float hoverScale = 1.08f;
    public float speed = 10f;
    // 1.08 means 8% bigger on hover, slightly less than email button

    Vector3 originalScale;
    Vector3 targetScale;
    // store original size so we can always return to it

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        // save the starting scale on first frame
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
        // smoothly lerp towards target scale every frame
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        // mouse on = scale up slightly
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        // mouse off = back to normal
    }
}