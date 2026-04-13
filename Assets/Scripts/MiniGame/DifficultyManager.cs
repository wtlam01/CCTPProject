// script 控制全局難度變化：
// 一開始：
// 1. 設定初始速度（speed）
// 2. 遊戲進行中每一幀慢慢增加速度（speedIncreasePerSecond）
// 3. 當速度達到上限（maxSpeed）後停止增加
// 4. 其他 script 可以讀取呢個 speed 作為統一遊戲速度

// This script manages global game difficulty by increasing speed over time.
// At runtime:
// 1. Starts with an initial speed value (speed)
// 2. Gradually increases speed every frame (speedIncreasePerSecond)
// 3. Stops increasing once reaching the maximum speed (maxSpeed)
// 4. Other scripts read this speed value for consistent gameplay scaling


using UnityEngine;

public class DifficultyManager : MonoBehaviour
// global manager that slowly increases game speed over time, other scripts read from this
{
    public static DifficultyManager Instance;
    // singleton so any script can access it with DifficultyManager.Instance.speed

    [Header("Speed")]
    public float speed = 1f;
    public float maxSpeed = 3f;
    // starts at 1, caps at 3 so it doesnt get too insane

    [Header("How fast it ramps")]
    public float speedIncreasePerSecond = 0.05f;
    // gradually increases speed every second, small value so player barely notices at first

    void Awake()
    {
        Instance = this;
        // set singleton reference on awake
    }

    void Update()
    {
        speed = Mathf.Min(maxSpeed, speed + speedIncreasePerSecond * Time.deltaTime);
        // increment speed each frame but clamp it so it never goes above maxSpeed
    }
}

// References: Singleton pattern - common Unity design pattern 
//Reference: Mini game (Dani, 2020)