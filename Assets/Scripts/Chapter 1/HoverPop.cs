// this script 用嚟控制按鈕嘅 hover 效果：
// 當滑鼠移上去時：
// 1. 按鈕會平滑放大
// 2. 按鈕會稍微變光

// 當滑鼠移走時：
// 3. 按鈕會平滑縮返原本大小
// 4. 亮度會回復正常

// This script controls the button hover effect:
// When the mouse enters:
// 1. The button smoothly scales up
// 2. The button becomes slightly brighter (optional)

// When the mouse exits:
// 3. The button smoothly returns to its original scale
// 4. The brightness returns to its original value

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// UI needed to access Image component for color change

public class HoverPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// hover effect with both scale and brightness, fancier version of the other hover scripts
{
    public float hoverScale = 1.08f;
    public float speed = 12f;
    // 8% bigger on hover, speed controls how snappy the lerp feels

    public bool brightenOnHover = true;
    public float hoverBrightness = 1.15f;
    // optional brightness boost on hover, multiplies rgb values by 1.15

    Vector3 baseScale;
    Vector3 targetScale;
    // store original scale to return to

    Image img;
    Color baseColor;
    Color targetColor;
    // store original color so we can lerp back to it on exit

    void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
        // save starting scale

        img = GetComponent<Image>();
        if (img != null)
        {
            baseColor = img.color;
            targetColor = baseColor;
        }
        // grab image and save its original color, null check bc not every object has image
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);
        // smoothly lerp scale toward target every frame

        if (brightenOnHover && img != null)
            img.color = Color.Lerp(img.color, targetColor, Time.unscaledDeltaTime * speed);
        // also lerp color if brighten is enabled, same speed as scale
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;
        // set target scale bigger on hover

        if (brightenOnHover && img != null)
            targetColor = new Color(
                Mathf.Clamp01(baseColor.r * hoverBrightness),
                Mathf.Clamp01(baseColor.g * hoverBrightness),
                Mathf.Clamp01(baseColor.b * hoverBrightness),
                baseColor.a
            );
        // multiply each rgb channel by brightness value, clamp so it doesnt go over 1, keep original alpha
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;

        if (brightenOnHover && img != null)
            targetColor = baseColor;
        // reset both scale and color targets back to original on mouse exit
    }
}