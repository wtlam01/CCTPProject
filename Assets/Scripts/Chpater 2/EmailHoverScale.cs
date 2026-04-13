// script 控制 email 按鈕嘅 hover 縮放效果
// 使用 Lerp 做平滑過渡，而唔係即時跳變

// 一開始：
// 1. 記錄按鈕原本嘅大小（originalScale）
// 2. 將 targetScale 設為原本大小

// 每一幀（Update）：
// 3. 使用 Lerp 將目前大小 gradually 移向 targetScale
//    令縮放變得平滑，而唔係突然改變

// 當滑鼠移上去（hover）：
// 4. 將 targetScale 設為放大後嘅大小（originalScale * hoverScale）

// 當滑鼠移走：
// 5. 將 targetScale 設回原本大小（originalScale）

// This script controls the email button hover scaling effect:
// using Lerp to create a smooth transition instead of an instant change

// At start:
// 1. Store the original scale of the button (originalScale)
// 2. Set targetScale to the original scale

// Every frame (Update):
// 3. Smoothly interpolate current scale towards targetScale using Lerp
//    creates a smooth scaling effect instead of a sudden jump

// On hover (pointer enter):
// 4. Set targetScale to a larger value (originalScale * hoverScale)

// On hover exit:
// 5. Reset targetScale back to the original scale

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