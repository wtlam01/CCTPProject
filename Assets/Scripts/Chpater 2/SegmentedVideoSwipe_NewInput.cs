using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class SegmentedVideoSwipe_NewInput : MonoBehaviour
{
    [Header("UI")]
    public GameObject sofaImage;        // SofalImage (UI Image object)
    public GameObject videoRawImage;    // VideoRawImage (RawImage object)
    public GameObject swipeHint;        // SwipeHint (TMP or any GameObject)

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public float sofaShowTime = 2f;

    [Header("Stop Frames (25fps)")]
    public List<long> stopFrames = new List<long>
    {
        45, 95, 145, 195, 245, 295, 335
    };

    [Header("Swipe")]
    public float swipeThreshold = 120f; // drag up pixels

    private int stopIndex = 0;
    private bool waitingForSwipe = false;

    private bool dragging = false;
    private Vector2 startPos;

    IEnumerator Start()
    {
        // Initial UI
        if (swipeHint) swipeHint.SetActive(false);

        sofaImage.SetActive(true);
        videoRawImage.SetActive(false);

        yield return new WaitForSeconds(sofaShowTime);

        sofaImage.SetActive(false);
        videoRawImage.SetActive(true);

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.Play();
    }

    void Update()
    {
        if (!videoPlayer.isPrepared) return;

        // Stop logic
        if (!waitingForSwipe && stopIndex < stopFrames.Count)
        {
           if (videoPlayer.frame >= stopFrames[stopIndex] && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                waitingForSwipe = true;
                if (swipeHint) swipeHint.SetActive(true);
            }
        }

        // Swipe detect (New Input System friendly: use mouse position + button)
        if (waitingForSwipe)
        {
            HandleMouseDragUp();
        }
    }

    void HandleMouseDragUp()
    {
        // Works in both old/new input handling because we're using UnityEngine.Input in a minimal way.
        // If your project is strictly New Input System only and still errors, tell me and we switch to Pointer callbacks.

        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            startPos = Input.mousePosition;
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            float deltaY = ((Vector2)Input.mousePosition).y - startPos.y;

            if (deltaY > swipeThreshold)
            {
                // Continue
                if (swipeHint) swipeHint.SetActive(false);

                waitingForSwipe = false;
                stopIndex++;

                // If still has segments, continue playing
                videoPlayer.Play();
            }
        }
    }
}