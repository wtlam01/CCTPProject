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