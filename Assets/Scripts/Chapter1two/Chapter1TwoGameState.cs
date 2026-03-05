using UnityEngine;

public class Chapter1TwoGameState : MonoBehaviour
{
    public static Chapter1TwoGameState Instance;

    public int day = 0;
    public int progress = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // keep across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
}