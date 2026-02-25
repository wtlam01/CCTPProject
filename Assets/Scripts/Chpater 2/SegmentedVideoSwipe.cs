using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class SegmentedVideoSwipe : MonoBehaviour
{
    public GameObject sofaImage;
    public GameObject videoRawImage;
    public VideoPlayer videoPlayer;

    public float sofaShowTime = 2f;

    public List<float> stopTimes = new List<float>
    {
        1.8f, 3.8f, 5.8f, 7.8f, 9.8f, 11.8f, 13.4f
    };

    private int stopIndex = 0;
    private bool waitingForSwipe = false;
    private Vector2 startPos;

    IEnumerator Start()
    {
        // 1) 先顯示 sofa
        sofaImage.SetActive(true);
        sofaImage.transform.SetAsLastSibling(); // <— 重要：強制置頂

        // 如果你 SofaImage 有 CanvasGroup，保險起見強制 alpha = 1
        var sofaCg = sofaImage.GetComponent<CanvasGroup>();
        if (sofaCg != null) sofaCg.alpha = 1f;

        videoRawImage.SetActive(false);

        yield return new WaitForSeconds(sofaShowTime);

        // 2) 切去影片
        sofaImage.SetActive(false);
        videoRawImage.SetActive(true);
        videoRawImage.transform.SetAsLastSibling(); // <— 保證影片在最上

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.Play();
    }

    void Update()
    {
        if (!videoPlayer.isPrepared) return;

        if (!waitingForSwipe && stopIndex < stopTimes.Count)
        {
            if (videoPlayer.time >= stopTimes[stopIndex])
            {
                videoPlayer.Pause();
                waitingForSwipe = true;
            }
        }

        if (waitingForSwipe)
        {
            HandleSwipe();
        }
    }

    void HandleSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            float deltaY = Input.mousePosition.y - startPos.y;

            if (deltaY > 100f)
            {
                waitingForSwipe = false;
                stopIndex++;
                videoPlayer.Play();
            }
        }
    }
}