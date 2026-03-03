using UnityEngine;

public class SpawnObstacles : MonoBehaviour
{
    public GameObject obstaclePrefab;

    public float minY = -4.5f;
    public float maxY =  4.5f;

    public float timeBetweenSpawn = 0.8f;
    private float nextSpawnTime;

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            Spawn();
            nextSpawnTime = Time.time + timeBetweenSpawn;
        }
    }

    void Spawn()
    {
        float y = Random.Range(minY, maxY);
        Vector3 pos = new Vector3(transform.position.x, y, 0f);
        Instantiate(obstaclePrefab, pos, Quaternion.identity);
    }
}