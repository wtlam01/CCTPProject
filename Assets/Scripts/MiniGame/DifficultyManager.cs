// 係全局嘅難度管理器，遊戲速度會隨時間慢慢增加，其他script讀取呢度嘅speed值
// This script is a global difficulty manager that gradually increases game speed over time, read by obstacle and background scripts.

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