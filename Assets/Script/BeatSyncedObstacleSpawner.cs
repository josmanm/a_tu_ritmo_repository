using UnityEngine;
using System.Collections.Generic;

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

    [Header("Jump timing")]
    public float obstacleArrivalDelay = 0.22f;
    public float contactPadding = 0.1f;
    public float minimumTravelTime = 0.55f;
    [Range(0f, 1f)] public float adaptiveJumpStrength = 0.8f;
    public float minimumJumpMultiplier = 0.88f;
    public float maximumJumpMultiplier = 1.22f;
    public float earlyContactBias = 0.08f;

    [Header("Fallback obstacle")]
    public Vector2 fallbackObstacleSize = new(0.85f, 1.8f);
    public Color fallbackObstacleColor = new(0.18f, 0.18f, 0.18f, 1f);

    public bool gameStarted = false;

    int beatCount;
    static Sprite fallbackSprite;
    Collider2D runnerCollider;
    RunnerController2D runnerController;
    readonly List<ObstacleMoverBeat> activeObstacles = new();


    void OnEnable()
    {
        if (beat != null) beat.OnBeat += HandleBeat;

        if (runner != null && runnerCollider == null)
            runnerCollider = runner.GetComponent<Collider2D>();

        if (runner != null && runnerController == null)
            runnerController = runner.GetComponent<RunnerController2D>();
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
        if (runner == null || beat == null) return;

        float runnerX = runner.position.x;
        double now = AudioSettings.dspTime;
        double desiredContactTime = targetBeatTime + obstacleArrivalDelay;

        if (desiredContactTime - now < minimumTravelTime)
        {
            targetBeatTime += beat.IntervalSec;
            desiredContactTime = targetBeatTime + obstacleArrivalDelay;
        }

        var pos = new Vector3(spawnX, obstacleY, 0);
        GameObject obs = obstaclePrefab != null
            ? Instantiate(obstaclePrefab, pos, Quaternion.identity)
            : CreateFallbackObstacle(pos);

        var mover = obs.GetComponent<ObstacleMoverBeat>();
        if (mover == null) mover = obs.AddComponent<ObstacleMoverBeat>();

        float obstacleHalfWidth = GetHalfWidth(obs.GetComponent<Collider2D>());
        float obstacleTopY = GetTopY(obs.GetComponent<Collider2D>(), obstacleY);
        float runnerHalfWidth = GetHalfWidth(runnerCollider);
        float physicsBasedDelay = runnerController != null
            ? runnerController.GetRecommendedObstacleContactDelay(obstacleTopY)
            : 0.38f;

        desiredContactTime = targetBeatTime + physicsBasedDelay + obstacleArrivalDelay - earlyContactBias;

        if (desiredContactTime - now < minimumTravelTime)
            desiredContactTime = now + minimumTravelTime;

        // Padding positivo retrasa el contacto real y deja una holgura extra para saltar.
        float contactTargetX = runnerX + obstacleHalfWidth + runnerHalfWidth - contactPadding;
        float distance = spawnX - contactTargetX;
        if (distance <= 0.5f) distance = 5f;

        double timeToTarget = desiredContactTime - now;
        if (timeToTarget < minimumTravelTime)
            timeToTarget = minimumTravelTime;

        float speed = (float)(distance / timeToTarget);

        mover.speed = speed;
        mover.runnerX = contactTargetX;
        mover.destroyPoint = destroyPoint;
        activeObstacles.Add(mover);
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

    public float GetAdaptiveJumpMultiplier(float defaultMultiplier, float desiredTimeToContact)
    {
        ObstacleMoverBeat obstacle = GetNearestActiveObstacle();
        if (obstacle == null)
            return Mathf.Clamp(defaultMultiplier, minimumJumpMultiplier, maximumJumpMultiplier);

        float actualTimeToContact = obstacle.TimeToRunnerContact;
        if (float.IsInfinity(actualTimeToContact) || actualTimeToContact <= 0.02f)
            return Mathf.Clamp(defaultMultiplier, minimumJumpMultiplier, maximumJumpMultiplier);

        float ratio = desiredTimeToContact / actualTimeToContact;
        float correction = (ratio - 1f) * adaptiveJumpStrength;
        float result = defaultMultiplier + correction;
        return Mathf.Clamp(result, minimumJumpMultiplier, maximumJumpMultiplier);
    }

    GameObject CreateFallbackObstacle(Vector3 position)
    {
        var obstacle = new GameObject("TempoObstacle");
        obstacle.tag = "Obstacle";
        obstacle.transform.position = position;

        var renderer = obstacle.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackSprite();
        renderer.color = fallbackObstacleColor;
        renderer.sortingOrder = 9;

        var collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = fallbackObstacleSize;

        obstacle.transform.localScale = new Vector3(fallbackObstacleSize.x, fallbackObstacleSize.y, 1f);
        return obstacle;
    }

    static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        fallbackSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return fallbackSprite;
    }

    float GetHalfWidth(Collider2D col)
    {
        if (col == null)
            return 0.5f;

        return col.bounds.extents.x;
    }

    float GetTopY(Collider2D col, float fallbackY)
    {
        if (col == null)
            return fallbackY + 0.5f;

        return col.bounds.max.y;
    }

    ObstacleMoverBeat GetNearestActiveObstacle()
    {
        float bestTime = float.PositiveInfinity;
        ObstacleMoverBeat best = null;

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            ObstacleMoverBeat obstacle = activeObstacles[i];
            if (obstacle == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            float timeToContact = obstacle.TimeToRunnerContact;
            if (timeToContact < 0f)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            if (timeToContact < bestTime)
            {
                bestTime = timeToContact;
                best = obstacle;
            }
        }

        return best;
    }
}
