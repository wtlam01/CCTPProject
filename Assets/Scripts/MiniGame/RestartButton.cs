using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButtonHook : MonoBehaviour
{
    public bool reloadCurrentScene = true;
    public string hubSceneName = "Chapter1two";
    // hub scene name to return to if day limit reached

    public void Restart()
    {
        if (Chapter1TwoGameState.Instance != null)
        {
            Chapter1TwoGameState.Instance.AddRestart();
            // add restart, might also increment playCount and day if hit 3 retries

            if (Chapter1TwoGameState.Instance.day >= 7)
            {
                Chapter1TwoGameState.Instance.MarkReturnedFromMiniGame();
                SceneManager.LoadScene(hubSceneName);
                // day limit hit after retry penalty, go back to hub so exam triggers
                return;
            }
        }

        if (reloadCurrentScene)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // normal restart, just reload mini game
    }
}