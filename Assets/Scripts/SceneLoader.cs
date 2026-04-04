// t負責場景跳轉，attach係button上面，click就跳去指定scene
// This script handles scene loading, attach to any button and call the function via OnClick in the inspector.


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