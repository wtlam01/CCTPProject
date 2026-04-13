// script 控制 send 按鈕嘅 hover 縮放效果：
// same as email button 相同，使用 Lerp 做平滑過渡

// 當滑鼠移上去時：
// 1. 按鈕會平滑放大（hoverScale）

// 當滑鼠移走時：
// 2. 按鈕會平滑縮返原本大小（originalScale）

// 運作方式：
// 3. 每一幀使用 Vector3.Lerp 將當前 scale 慢慢逼近 targetScale
// 4. speed 控制縮放變化嘅速度（越大越快）



// This script controls the hover scale effect of the send button,
// using the same principle as the email button with smooth interpolation.

// On mouse enter:
// 1. The button smoothly scales up (hoverScale)

// On mouse exit:
// 2. The button smoothly returns to its original size (originalScale)

// How it works:
// 3. Each frame uses Vector3.Lerp to gradually move the current scale toward targetScale
// 4. The speed variable controls how fast the transition feels


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