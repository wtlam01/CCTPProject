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