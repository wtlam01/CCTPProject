// script 控制 Chapter 1 Two 嘅全局遊戲狀態（GameState）：
// 使用 DontDestroyOnLoad 保持資料喺 scene 切換之間，讓 hub 同 mini game 可以共享進度

// 主要功能：
// 1. 記錄玩家嘅隱藏數值（day、progress、studyTogetherCount、playCount）
// 2. 管理 mini game 行為（restart 次數會影響時間同選擇）
// 3. 提供統一入口俾其他 script 存取數據（Singleton pattern）

// 系統邏輯：
// 4. Study Together：
//    +1 day
//    +1 progress
//    +1 studyTogetherCount

// 5. Play：
//    +1 day
//    +1 playCount

// 6. Mini game retry 機制：
//    每 3 次 retry → +1 day +1 playCount（隱藏懲罰）

// 7. 狀態控制：
//    returnedFromMiniGame：通知 hub 要繼續流程
//    studyHintAlreadyShown：避免 hint 重複出現

// 額外：
// 提供 ResetAll() 可以重置整個 chapter 狀態
// 使用 Singleton + DontDestroyOnLoad 確保只有一個 instance 並跨 scene 保留


// This script manages the global game state for Chapter 1 Two:
// It uses DontDestroyOnLoad to persist data across scene transitions,
// allowing the hub and mini game to share progression data.

// Main responsibilities:
// 1. Track hidden variables (day, progress, studyTogetherCount, playCount)
// 2. Manage mini game behaviour (restart count affects time and choices)
// 3. Provide a central access point for other scripts (Singleton pattern)

// System logic:
// 4. Study Together:
//    +1 day
//    +1 progress
//    +1 studyTogetherCount

// 5. Play:
//    +1 day
//    +1 playCount

// 6. Mini game retry system:
//    Every 3 retries → +1 day +1 playCount (hidden penalty)

// 7. State control:
//    returnedFromMiniGame: tells hub to resume flow
//    studyHintAlreadyShown: prevents hint from appearing multiple times

// Additional:
// ResetAll() allows full reset of chapter state
// Uses Singleton + DontDestroyOnLoad to ensure a single persistent instance

using UnityEngine;

public class Chapter1TwoGameState : MonoBehaviour
// persistent game state for chapter 1 two, survives scene loads so data carries between mini game and hub
{
    public static Chapter1TwoGameState Instance { get; private set; }
    // singleton so any script can access it with Chapter1TwoGameState.Instance

    [Header("Hidden Vars (NO UI)")]
    public int day = 0;
    public int progress = 0;
    // day tracks how many days passed, progress tracks how well player is doing

    [Header("Hidden Cost (NO UI)")]
    public int studyTogetherCount = 0;
    // hidden counter, too many study together sessions actually causes failure

    [Header("MiniGame Behaviour Layer (kept global)")]
    public int restartCount = 1;
    public int playCount = 0;
    // restartCount tracks retries within one game session, playCount tracks total times player chose play from hub

    [Header("Flags")]
    public bool returnedFromMiniGame = false;
    public bool studyHintAlreadyShown = false;
    // returnedFromMiniGame tells hub to resume flow, studyHintAlreadyShown stops hint appearing twice

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // standard singleton pattern, destroy duplicate and persist across scenes
    }

    // ---------- Social system ----------
    public void AddStudyChoice(int dayPerChoice, int progressGain)
{
    day += 1;
    progress += progressGain;
    studyTogetherCount += 1;
    // study = +1 day
}

public void AddPlayChoice(int dayPerChoice)
{
    day += 1;
    playCount += 1;
    // play = +1 day
}

    // ---------- MiniGame ----------
    public void AddRestart()
{
    restartCount++;

    if (restartCount > 1 && (restartCount - 1) % 3 == 0)
    {
        day += 1;
        playCount += 1;
        // every 3 retries = +1 day +1 playCount
    }
}

    public void ResetMiniGameRestartCount()
    {
        restartCount = 1;
        // reset to 1 when starting a fresh mini game run from hub
    }

    public void MarkReturnedFromMiniGame()
    {
        returnedFromMiniGame = true;
        // called before loading back to hub so hub knows to resume choice logic
    }

    public void ResetAll()
    {
        day = 0;
        progress = 0;
        studyTogetherCount = 0;
        playCount = 0;
        restartCount = 1;
        returnedFromMiniGame = false;
        studyHintAlreadyShown = false;
        // full reset if chapter needs to restart from scratch
    }
}