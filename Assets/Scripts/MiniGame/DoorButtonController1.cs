using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DoorButtonController1 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image doorImage;
    public Button doorButton; // ✅ drag Button component (same GO)

    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;

    [Header("Scene")]
    public string targetScene = "Chapter1two";

    [Header("State")]
    public bool isLocked = false;

    void Awake()
    {
        if (doorButton == null) doorButton = GetComponent<Button>();
    }

    void Start()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;

        ApplyLockVisual();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        ApplyLockVisual();
    }

    void ApplyLockVisual()
    {
        // ✅ 你想「隱藏 door」就用 SetActive
        gameObject.SetActive(!isLocked);

        // 如果你唔想 SetActive（只想 disable），改用下面：
        // if (doorButton != null) doorButton.interactable = !isLocked;
    }

    public void GoToScene()
    {
        if (isLocked) return;
        SceneManager.LoadScene(targetScene);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked) return;
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
    }
}