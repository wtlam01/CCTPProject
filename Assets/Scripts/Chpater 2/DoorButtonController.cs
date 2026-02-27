using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoorButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;   // Door.png
    public Sprite doorOpenSprite;     // DoorOpen.png

    [Header("UI References")]
    public Image doorImage;           // DoorButton 自己個 Image
    public GameObject sofaImage;      // SofaImage (3OnSofa png)
    public GameObject emailGroup;     // EmailGroup（可留空）
    public GameObject videoRawImage;  // VideoRawImage（可留空）

    [Header("Optional")]
    public GameObject hideOnClick;    // DoorButton 自己（可留空，會自動用自己）

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        if (hideOnClick == null) hideOnClick = gameObject;

        if (doorClosedSprite != null && doorImage != null)
            doorImage.sprite = doorClosedSprite;
    }

    // ✅ 用 Button OnClick 去 call 呢個
    public void OnDoorClicked()
    {
        // 1) 顯示 Sofa
        if (sofaImage != null) sofaImage.SetActive(true);

        // 2) 隱藏 Email & Video（避免蓋住 sofa）
        if (emailGroup != null) emailGroup.SetActive(false);
        if (videoRawImage != null) videoRawImage.SetActive(false);

        // 3) 隱藏門
        if (hideOnClick != null) hideOnClick.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
    }
}