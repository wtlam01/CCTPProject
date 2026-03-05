using UnityEngine;

public class Chapter1TwoGameState : MonoBehaviour
{
    public static Chapter1TwoGameState Instance;

    [Header("Decision layer (global)")]
    public int day = 0;
    public int progress = 0;
    public int playSessionCount = 0;

    [Header("Behaviour layer (mini-game, but kept global)")]
    public int restartCount = 0;

    [Header("Soft lock runtime")]
    public bool exitLockedThisGameOver = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ 給其他 script 用（你 console 報錯就係因為冇呢個）
    public void ResetMiniGameRestartCount()
    {
        restartCount = 0;
        exitLockedThisGameOver = false;
    }
}