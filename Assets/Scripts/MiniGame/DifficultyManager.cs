using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Speed")]
    public float speed = 1f;
    public float maxSpeed = 3f;

    [Header("How fast it ramps")]
    public float speedIncreasePerSecond = 0.05f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        speed = Mathf.Min(maxSpeed, speed + speedIncreasePerSecond * Time.deltaTime);
    }
}