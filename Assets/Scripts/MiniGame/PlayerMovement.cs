// 控制玩家上下移動，用上下鍵控制，冇重力，input喺Update讀，physics喺FixedUpdate apply
// This script handles player movement using up and down arrow keys with no gravity, reading input in Update and applying physics in FixedUpdate for stability.

using UnityEngine;
using UnityEngine.InputSystem;
// new input system for keyboard detection

[RequireComponent(typeof(Rigidbody2D))]
// auto adds Rigidbody2D if missing
public class PlayerMovement : MonoBehaviour
// handles player movement, up and down arrow keys only, no gravity
{
    public float playerSpeed = 6f;
    // how fast player moves up and down

    Rigidbody2D rb;
    float moveY;
    // moveY stores direction each frame, applied in FixedUpdate

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        // turn off gravity so player floats, freeze rotation so it doesnt spin on collision
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) { moveY = 0f; return; }

        moveY = 0f;
        if (kb.upArrowKey.isPressed) moveY += 1f;
        if (kb.downArrowKey.isPressed) moveY -= 1f;
        // read input in Update, both keys can cancel each other out if pressed together
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(0f, moveY * playerSpeed);
        // apply movement in FixedUpdate bc its physics based, more stable than Update
    }
}