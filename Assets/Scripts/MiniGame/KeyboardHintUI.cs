using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeyboardHintUI : MonoBehaviour
{
    [Header("UI Images")]
    public Image upImage;
    public Image downImage;

    [Header("Sprites")]
    public Sprite upNormal;
    public Sprite upPressed;
    public Sprite downNormal;
    public Sprite downPressed;

    [Header("Press Feel")]
    [Range(0.8f, 1.0f)] public float pressedScale = 0.92f;
    public float lerpSpeed = 14f;

    Vector3 upBaseScale;
    Vector3 downBaseScale;

    void Awake()
    {
        if (upImage) upBaseScale = upImage.rectTransform.localScale;
        if (downImage) downBaseScale = downImage.rectTransform.localScale;

        // set initial sprites
        if (upImage && upNormal) upImage.sprite = upNormal;
        if (downImage && downNormal) downImage.sprite = downNormal;
    }

    void Update()
    {
        // If no keyboard (e.g., mobile), keep normal state
        if (Keyboard.current == null)
            return;

        bool upHeld = Keyboard.current.upArrowKey.isPressed;
        bool downHeld = Keyboard.current.downArrowKey.isPressed;

        // swap sprites
        if (upImage)
            upImage.sprite = upHeld ? (upPressed ? upPressed : upNormal) : upNormal;

        if (downImage)
            downImage.sprite = downHeld ? (downPressed ? downPressed : downNormal) : downNormal;

        // scale feel
        if (upImage)
        {
            Vector3 target = upHeld ? upBaseScale * pressedScale : upBaseScale;
            upImage.rectTransform.localScale = Vector3.Lerp(upImage.rectTransform.localScale, target, Time.deltaTime * lerpSpeed);
        }

        if (downImage)
        {
            Vector3 target = downHeld ? downBaseScale * pressedScale : downBaseScale;
            downImage.rectTransform.localScale = Vector3.Lerp(downImage.rectTransform.localScale, target, Time.deltaTime * lerpSpeed);
        }
    }
}