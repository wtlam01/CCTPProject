using UnityEngine;

public class SideScrollRunner : MonoBehaviour
{
    public float cameraSpeed = 4f;

    void Update()
    {
        transform.position += new Vector3(cameraSpeed * Time.deltaTime, 0f, 0f);
    }
}