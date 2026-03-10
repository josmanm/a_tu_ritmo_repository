using UnityEngine;

public class BeatSyncedObstacleSpawner : MonoBehaviour
{
    public BeatController beat;

    [Header("Prefabs/Refs")]
    public GameObject obstaclePrefab;
    public Transform runner;          // Runner transform (posición x fija)
    public Transform destroyPoint;    // Punto a la izquierda

    [Header("Spawn points")]
    public float spawnX = 12f;
    public float obstacleY = -2.5f;

    [Header("Beat design")]
    public int beatsAhead = 4;        // cuántos beats antes spawnear
    public int beatSpacing = 2;       // 2 = cada 2 beats aparece uno

    public bool gameStarted = false;

    int beatCount;


    void OnEnable()
    {
        if (beat != null) beat.OnBeat += HandleBeat;
    }

    void OnDisable()
    {
        if (beat != null) beat.OnBeat -= HandleBeat;
    }

    void HandleBeat(double beatDspTime)
    {
        if (!gameStarted) return;

        beatCount++;

        if (beatCount % beatSpacing != 0) return;

        double targetBeatTime = beatDspTime + beat.IntervalSec * beatsAhead;
        SpawnForTargetBeat(targetBeatTime);
    }

    void SpawnForTargetBeat(double targetBeatTime)
    {
        if (obstaclePrefab == null || runner == null || beat == null) return;

        // Distancia desde spawn hasta runner
        float runnerX = runner.position.x;
        float distance = spawnX - runnerX;
        if (distance <= 0.5f) distance = 5f;

        // Tiempo hasta el beat objetivo (desde ahora)
        double now = AudioSettings.dspTime;
        double timeToTarget = targetBeatTime - now;

        // Si queda muy poco tiempo, empuja al próximo beat para evitar injusticias
        if (timeToTarget < beat.IntervalSec * 0.8)
        {
            targetBeatTime += beat.IntervalSec;
            timeToTarget = targetBeatTime - now;
        }

        // Velocidad necesaria para llegar exacto al runner en ese tiempo
        float speed = (float)(distance / timeToTarget);

        var pos = new Vector3(spawnX, obstacleY, 0);
        GameObject obs = Instantiate(obstaclePrefab, pos, Quaternion.identity);

        var mover = obs.GetComponent<ObstacleMoverBeat>();
        if (mover == null) mover = obs.AddComponent<ObstacleMoverBeat>();

        mover.speed = speed;
        mover.runnerX = runnerX;
        mover.destroyPoint = destroyPoint;
    }

    public void StartSpawner()
    {
        gameStarted = true;
        beatCount = 0;
    }

    public void StopSpawner()
    {
        gameStarted = false;
    }
}