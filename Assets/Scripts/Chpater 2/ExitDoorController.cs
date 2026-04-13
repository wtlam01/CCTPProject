// script 控制 exit door butoom嘅互動流程：

// 一開始：
// 1. 設定門為關閉狀態（doorClosedSprite）
// 2. 自動取得 doorImage 同 doorCanvasGroup

// 當滑鼠移上去（hover）：
// 3. 如果門可見，將 sprite 切換為打開狀態（doorOpenSprite）

// 當滑鼠移走：
// 4. 將 sprite 切換回關閉狀態（doorClosedSprite）

// 當玩家 click exit door：
// 5. 檢查 busy 狀態，避免重複 click
// 6. 如果門目前不可見，取消操作
// 7. 將門顯示為打開狀態（doorOpenSprite）
// 8. 等待一小段時間（openHoldSeconds），player見到開門效果
// 9. 即時隱藏 sofa 上相關按鈕（HideSofaButtonsImmediate）
// 10. 隱藏其他指定 UI（emailGroupToHide / hideOnClick）
// 11. 呼叫 sofa controller 嘅 RequestExit()，開始 exit 流程
// 12. 最後將門重設為關閉狀態，並解除 busy 鎖定


// This script controls the exit door button interaction flow:

// At start:
// 1. Set the door to the closed state (doorClosedSprite)
// 2. Auto-assign doorImage and doorCanvasGroup if not set in the Inspector

// On hover (pointer enter):
// 3. If the door is visible, switch the sprite to the open door state (doorOpenSprite)

// On hover exit:
// 4. Switch the sprite back to the closed state (doorClosedSprite)

// On click:
// 5. Check the busy flag to prevent double clicking
// 6. Cancel if the door is currently invisible
// 7. Show the open door sprite (doorOpenSprite)
// 8. Wait briefly (openHoldSeconds) so the open-door state is visible
// 9. Immediately hide sofa-related buttons (HideSofaButtonsImmediate)
// 10. Hide any additional UI objects (emailGroupToHide / hideOnClick)
// 11. Call sofaEmailController.RequestExit() to begin the exit sequence
// 12. Reset the door back to the closed sprite and release the busy lock

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// need event system for the hover detect, same as before

public class ExitDoorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// handles the exit door, hover sprite swap + click to trigger exit sequence
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // same as the other door, two sprites swapping on hover

    [Header("UI References")]
    public Image doorImage;
    public CanvasGroup doorCanvasGroup;
    public GameObject emailGroupToHide;
    public GameObject hideOnClick;
    // canvasgroup lets us check if door is visible before doing anything

    [Header("Next Controller")]
    public SofaEmailController sofaEmailController;
    // reference to sofa controller so we can call exit and hide buttons from here

    [Header("Timings")]
    public float openHoldSeconds = 0.15f;
    // small delay so the open sprite shows briefly before things hide, feels more natural

    bool busy = false;
    // prevent double clicking

    void Reset()
    {
        doorImage = GetComponent<Image>();
        doorCanvasGroup = GetComponent<CanvasGroup>();
        // auto assign in editor when component first added
    }

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        if (doorCanvasGroup == null) doorCanvasGroup = GetComponent<CanvasGroup>();
        SetClosed();
        // grab components if not assigned, then default to closed sprite
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorCanvasGroup != null && doorCanvasGroup.alpha <= 0.001f) return;
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // only swap to open if door is actually visible, skip if alpha is basically 0
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetClosed();
        // mouse leaves = back to closed sprite
    }

    void SetClosed()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
        // reusable helper so i dont repeat the same null check everywhere
    }

    public void OnExitDoorClicked()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(ClickRoutine());
        // if already clicked once, ignore. otherwise start the exit coroutine
    }

    IEnumerator ClickRoutine()
    {
        if (doorCanvasGroup != null && doorCanvasGroup.alpha <= 0.001f)
        {
            busy = false;
            yield break;
        }
        // if door is invisible when clicked somehow, cancel and reset busy

        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // show open sprite on click

        if (openHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(openHoldSeconds);
        // wait a tiny bit so player sees the door open before everything changes

        if (sofaEmailController != null)
            sofaEmailController.HideSofaButtonsImmediate();
        // hide sofa buttons straight away, dont wait for the rest

        if (emailGroupToHide != null) emailGroupToHide.SetActive(false);
        if (hideOnClick != null) hideOnClick.SetActive(false);
        // hide any other UI that shouldnt be visible during exit

        if (sofaEmailController != null)
            sofaEmailController.RequestExit();
        // tell sofa controller to actually do the exit transition

        SetClosed();
        busy = false;
        // reset door sprite and unlock for next time
    }
}