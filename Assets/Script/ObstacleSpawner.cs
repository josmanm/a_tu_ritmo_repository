using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public Transform destroyPoint;

    [Header("Spawn")]
    public float spawnInterval = 2.0f;
    public float yPosition = -2.5f;

    [Header("Obstacle Speed")]
    public float obstacleSpeed = 6f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            Spawn();
        }
    }

    void Spawn()
    {
        if (obstaclePrefab == null) return;

        Vector3 pos = new Vector3(transform.position.x, yPosition, 0f);
        GameObject obs = Instantiate(obstaclePrefab, pos, Quaternion.identity);

        var mover = obs.GetComponent<ObstacleMover>();
        if (mover == null) mover = obs.AddComponent<ObstacleMover>();

        mover.speed = obstacleSpeed;
        mover.destroyPoint = destroyPoint;
    }
}