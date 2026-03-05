using UnityEngine;

public class Chapter1TwoGameState : MonoBehaviour
{
    public static Chapter1TwoGameState Instance { get; private set; }

    [Header("Hidden Vars (NO UI)")]
    public int day = 0;
    public int progress = 0;

    [Header("Hidden Cost (NO UI)")]
    public int studyTogetherCount = 0;

    [Header("MiniGame Behaviour Layer (kept global)")]
    public int restartCount = 1;

    [Header("Flags")]
    public bool returnedFromMiniGame = false;
    public bool studyHintAlreadyShown = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- Social system ----------
    public void AddStudyChoice(int dayPerChoice, int progressGain)
    {
        day += dayPerChoice;
        progress += progressGain;

        // ✅ new hidden cost
        studyTogetherCount += 1;
    }

    public void AddPlayChoice(int dayPerChoice)
    {
        day += dayPerChoice;
    }

    // ---------- MiniGame ----------
    public void AddRestart()
    {
        restartCount++;
    }

    public void ResetMiniGameRestartCount()
    {
        restartCount = 1;
    }

    public void MarkReturnedFromMiniGame()
    {
        returnedFromMiniGame = true;
    }

    // Optional reset (if you ever need restart whole chapter)
    public void ResetAll()
    {
        day = 0;
        progress = 0;
        studyTogetherCount = 0;

        restartCount = 1;
        returnedFromMiniGame = false;
        studyHintAlreadyShown = false;
    }
}