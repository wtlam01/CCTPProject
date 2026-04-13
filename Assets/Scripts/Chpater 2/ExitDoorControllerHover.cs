// script 控制簡化版門按鈕嘅 hover 效果：
// 只負責滑鼠移入／移出時切換 sprite，冇任何 click 或流程控制

// 一開始：
// 1. 自動取得 doorImage
// 2. 設定門為關閉狀態（doorClosedSprite）

// 當滑鼠移上去（hover）：
// 3. 將門圖片切換為打開狀態（doorOpenSprite）

// 當滑鼠移走：
// 4. 將門圖片切換回關閉狀態（doorClosedSprite）

// This script controls a simplified door hover interaction:
// it only swaps sprites on hover with no click or transition logic

// At start:
// 1. Auto-assign doorImage
// 2. Set the door to the closed state (doorClosedSprite)

// On hover (pointer enter):
// 3. Switch the sprite to the open door (doorOpenSprite)

// On hover exit:
// 4. Switch back to the closed door (doorClosedSprite)



using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// basic imports, event system for hover detection

public class ExitDoorControllerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// this is a simpler version, only handles the hover sprite swap, no click logic here
{
    [Header("Refs")]
    public Image doorImage;
    // the image component on the door

    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // two sprites to swap between on hover

    void Reset()
    {
        doorImage = GetComponent<Image>();
        // auto grab image when first added in editor
    }

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        SetClosed();
        // make sure it starts as closed sprite
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // mouse hover on = open door sprite
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetClosed();
        // mouse leaves = back to closed
    }

    void SetClosed()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
        // helper function so i dont repeat the null check every time
    }
}