using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadChapter1()
    {
        SceneManager.LoadScene("Chapter1");
    }
}