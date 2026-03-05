using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DoorButtonController1 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image doorImage;

    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;

    [Header("Scene")]
    public string targetScene = "Chapter1two";

    void Start()
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
    }

    // click door
    public void GoToScene()
    {
        SceneManager.LoadScene(targetScene);
    }

    // hover open
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorImage != null && doorOpenSprite != null)
            doorImage.sprite = doorOpenSprite;
    }

    // hover leave
    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorImage != null && doorClosedSprite != null)
            doorImage.sprite = doorClosedSprite;
    }
}