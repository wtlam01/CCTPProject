using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class Chapter1TwoController : MonoBehaviour
{
    [Header("Scene Names")]
    public string backSceneName = "Chapter1";

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject; // VideoRawImage GO
    public RawImage videoRawImage;         // RawImage component

    [Header("Chat Video URL")]
    public string chatURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/231Chatwithfriend.mp4";

    [Header("Optional BG while not playing")]
    public GameObject bgImageObject; // optional, can be null

    [Header("Skip")]
    public bool allowSkip = true;

    void Start()
    {
        StartCoroutine(PlayChatThenReturn());
    }

    IEnumerator PlayChatThenReturn()
    {
        if (videoPlayer == null) yield break;

        if (bgImageObject != null) bgImageObject.SetActive(false);
        if (videoRawImageObject != null) videoRawImageObject.SetActive(true);
        if (videoRawImage != null) videoRawImage.color = Color.black;

        videoPlayer.Stop();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = chatURL;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();

        // wait until it actually starts (first frame)
        double t0 = videoPlayer.time;
        float timeout = 2f;
        while (timeout > 0f && videoPlayer.time <= t0 + 0.01)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        while (videoPlayer.isPlaying)
        {
            if (allowSkip && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                break;
            }
            yield return null;
        }

        videoPlayer.Stop();
        SceneManager.LoadScene(backSceneName);
    }
}