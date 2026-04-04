//控制mini game入面嘅出口門按鈕，hover會換sprite，click會通知hub玩家返嚟
// 然後跳去指定scene，有lock state可以控制門係咪可以click
// This script handles the exit door button in the mini game, swapping sprites on hover and notifying
// the hub that the player is returning before loading the target scene, with a lock state to control visibility.

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