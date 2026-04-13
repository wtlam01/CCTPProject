// script 控制 TMP 文字嘅 hover 縮放效果：
// 一開始：
// 1. 記錄原本嘅 scale（originalScale）

// 每一幀（Update）：
// 2. 使用 Lerp 平滑過渡到 targetScale

// 當滑鼠移上去時：
// 3. 將 targetScale 設為放大後（originalScale * scaleMultiplier）

// 當滑鼠移走時：
// 4. 將 targetScale 設回原本大小

// This script handles a smooth hover scale effect on TMP text.
// At start:
// 1. Store the original scale

// Every frame (Update):
// 2. Smoothly lerp towards the target scale

// When the mouse enters:
// 3. Set the target scale to a larger size (originalScale * scaleMultiplier)

// When the mouse exits:
// 4. Reset the target scale back to the original size


using UnityEngine;
using UnityEngine.EventSystems;
// event system for hover detection

public class TMPHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// same hover scale as the other buttons but applied to TMP text instead
{
    public float scaleMultiplier = 1.15f;
    public float speed = 10f;
    // 15% bigger on hover, slightly more than other buttons

    private Vector3 originalScale;
    private Vector3 targetScale;
    // store original scale to return to after hover

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        // save scale on first frame
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
        // smoothly lerp to target scale every frame
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * scaleMultiplier;
        // mouse on = scale up
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        // mouse off = back to normal
    }
}