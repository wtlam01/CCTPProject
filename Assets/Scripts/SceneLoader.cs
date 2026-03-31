using UnityEngine;
using UnityEngine.SceneManagement;
// need this to use SceneManager.LoadScene

public class SceneLoader : MonoBehaviour
// simple script just for loading scenes, attach this to any button
{
    public void LoadChapter1()
    {
        SceneManager.LoadScene("Landing");
        // loads the Landing scene, called by button onclick in inspector
    }
}