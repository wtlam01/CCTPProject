using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.08f;
    public float speed = 12f;

    public bool brightenOnHover = true;
    public float hoverBrightness = 1.15f;

    Vector3 baseScale;
    Vector3 targetScale;

    Image img;
    Color baseColor;
    Color targetColor;

    void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;

        img = GetComponent<Image>();
        if (img != null)
        {
            baseColor = img.color;
            targetColor = baseColor;
        }
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);

        if (brightenOnHover && img != null)
            img.color = Color.Lerp(img.color, targetColor, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;

        if (brightenOnHover && img != null)
            targetColor = new Color(
                Mathf.Clamp01(baseColor.r * hoverBrightness),
                Mathf.Clamp01(baseColor.g * hoverBrightness),
                Mathf.Clamp01(baseColor.b * hoverBrightness),
                baseColor.a
            );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;

        if (brightenOnHover && img != null)
            targetColor = baseColor;
    }
}