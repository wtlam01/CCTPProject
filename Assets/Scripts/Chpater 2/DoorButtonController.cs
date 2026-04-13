// script 控制門按鈕（door button）嘅互動：
// 包括 hover 換圖、click 觸發下一步流程同隱藏當前 UI

// 一開始：
// 1. 將門設為關閉狀態（doorClosedSprite）

// 當滑鼠移上去（hover）：
// 2. 將門圖片切換為打開狀態（doorOpenSprite）

// 當滑鼠移開：
// 3. 將門圖片切換回關閉狀態（doorClosedSprite）

// 當玩家 click 門：
// 4. 隱藏 email overlay（emailGroupToHide）
// 5. 觸發下一個系統（sofaEmailController.StartSofaMode）
// 6. 隱藏門本身（或者指定 UI object）

// This script controls the door button interaction:
// including hover sprite swapping, click transition, and UI hiding

// At start:
// 1. Set the door to closed state (doorClosedSprite)

// On hover (pointer enter):
// 2. Switch the sprite to the open door (doorOpenSprite)

// On hover exit:
// 3. Switch back to the closed door (doorClosedSprite)

// On click:
// 4. Hide the email overlay (emailGroupToHide)
// 5. Trigger the next system (sofaEmailController.StartSofaMode)
// 6. Hide the door itself (or a specified object)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoorButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // two sprites for the door, one closed one open, swap them on hover

    [Header("UI References")]
    public Image doorImage;
    public GameObject emailGroupToHide;
    // doorImage is the actual UI image component, emailGroupToHide i can leave empty if dont need

    [Header("Next Controller")]
    public SofaEmailController sofaEmailController;
    // drag the sofa controller here so the door knows what to trigger next

    [Header("Optional")]
    public GameObject hideOnClick; // if null, will hide this gameObject
    // this lets me choose which object to hide when clicked, more flexible

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        // if i forgot to drag doorImage in inspector, it tries grab it itself
        if (doorImage != null && doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
        // set default sprite to closed door at the start
    }

    public void OnDoorClicked()
    {
        if (emailGroupToHide != null)
            emailGroupToHide.SetActive(false);
        // hide the old email overlay first before switching scene

        if (sofaEmailController != null)
            sofaEmailController.StartSofaMode();
        // tell sofa controller to start, this is where the next part begins

        if (hideOnClick != null) hideOnClick.SetActive(false);
        else gameObject.SetActive(false);
        // hide the door after clicking, either specific object or just itself
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null) doorImage.sprite = doorOpenSprite;
        // mouse hover on = show open door sprite
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
        // mouse hover off = back to closed, simple swap
    }
}