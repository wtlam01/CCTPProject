using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class Chapter1VideoController : MonoBehaviour
{
    [Header("Core")]
    public VideoPlayer videoPlayer;
    public GameObject videoRawImageObject; // VideoRawImage

    [Header("URL")]
    public string videoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/1.mp4";

    void Awake()
    {
        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(false);

        if (videoPlayer != null)
            videoPlayer.playOnAwake = false;
    }

    public void PlayChapter1Video()
    {
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        if (videoPlayer == null) yield break;

        if (videoRawImageObject != null)
            videoRawImageObject.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.isLooping = false;
        videoPlayer.url = videoURL;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();
    }
}