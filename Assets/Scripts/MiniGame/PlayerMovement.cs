using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float playerSpeed = 6f;

    Rigidbody2D rb;
    float moveY;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) { moveY = 0f; return; }

        moveY = 0f;
        if (kb.upArrowKey.isPressed) moveY += 1f;
        if (kb.downArrowKey.isPressed) moveY -= 1f;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(0f, moveY * playerSpeed);
    }
}