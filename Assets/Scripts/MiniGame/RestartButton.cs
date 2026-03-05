using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtonHook : MonoBehaviour
{
    public bool reloadCurrentScene = true;

    public void Restart()
    {
        if (Chapter1TwoGameState.Instance != null)
            Chapter1TwoGameState.Instance.AddRestart();

        if (reloadCurrentScene)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}