// script 控制玩家上下移動：
// 一開始：
// 1. 關閉 Rigidbody2D 重力（gravityScale = 0）
// 2. 鎖定旋轉（避免撞到時旋轉）

// 每一幀（Update）：
// 3. 讀取鍵盤輸入（上 / 下鍵）
// 4. 計算移動方向（moveY）

// 每個物理幀（FixedUpdate）：
// 5. 根據 moveY 設定 Rigidbody2D 嘅速度（上下移動）

// This script handles player movement using up and down arrow keys with no gravity.
// At start:
// 1. Disable Rigidbody2D gravity (gravityScale = 0)
// 2. Freeze rotation to prevent spinning on collision

// Every frame (Update):
// 3. Read keyboard input (up / down arrows)
// 4. Calculate movement direction (moveY)

// Every physics frame (FixedUpdate):
// 5. Apply velocity to the Rigidbody2D based on moveY (vertical movement)


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


//Reference: Mini game (Dani, 2020)