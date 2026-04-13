// script 控制 mini game 出口門按鈕：
// 一開始：
// 1. 設定門嘅預設 sprite（關門）
// 2. 根據 isLocked 決定門是否顯示

// 當滑鼠移上去時：
// 3. 門會變成開門 sprite（hover）

// 當滑鼠移走時：
// 4. 門會變返關門 sprite

// 當玩家 click 門：
// 5. 如果未 lock：通知 hub 玩家由 mini game 返回
// 6. 載入指定 scene（targetScene）

// This script handles the exit door in the mini game.
// At start:
// 1. Sets the default closed door sprite
// 2. Shows or hides the door based on lock state (isLocked)

// On mouse enter:
// 3. Switches to the open door sprite (hover)

// On mouse exit:
// 4. Switches back to the closed door sprite

// On click:
// 5. If not locked, notifies the hub that the player is returning
// 6. Loads the target scene (targetScene)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
// scene management to load next scene when door clicked

public class DoorButtonController1 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// door button that loads a scene on click, with hover sprite swap and a lock state
{
    [Header("UI")]
    public Image doorImage;
    public Button doorButton;
    // image for sprite swapping, button for click handling

    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;
    // same hover sprite swap as other door scripts

    [Header("Scene")]
    public string targetScene = "Chapter1two";
    // scene to load when door is clicked

    [Header("State")]
    public bool isLocked = false;
    // if locked, door hides itself and ignores clicks

    void Awake()
    {
        if (doorButton == null) doorButton = GetComponent<Button>();
        // auto grab button if not assigned
    }

    void Start()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;

        ApplyLockVisual();
        // set default sprite and apply lock state on start
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        ApplyLockVisual();
        // called externally to lock or unlock the door
    }

    void ApplyLockVisual()
    {
        gameObject.SetActive(!isLocked);
        // hide the whole door if locked, show if unlocked
    }

    public void GoToScene()
    {
        if (isLocked) return;

        if (Chapter1TwoGameState.Instance != null)
            Chapter1TwoGameState.Instance.MarkReturnedFromMiniGame();
        // tell hub that player is coming back from mini game
        // without this, hub wouldnt know to check if exam should trigger

        SceneManager.LoadScene(targetScene);
        // load target scene, skip if locked
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked) return;
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
        // hover on = open sprite, but only if not locked
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
        // hover off = back to closed sprite
    }
}

//Reference: Unity Technologies (2023a)