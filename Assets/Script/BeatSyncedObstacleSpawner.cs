using UnityEngine;
using System.Collections.Generic;

public class BeatSyncedObstacleSpawner : MonoBehaviour
{
    public enum ObstacleAction { Tap, Jump, Duck }

    private struct ScheduledObstacle
    {
        public ObstacleMoverBeat mover;
        public double beatTime;
        public ObstacleAction action;
    }

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
    readonly List<ScheduledObstacle> scheduledObstacles = new();
    int obstacleCount;
    int difficultyStage;


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

        int spacing = difficultyStage >= 3 ? 1 : difficultyStage >= 1 ? 2 : 3;
        if (beatCount % spacing != 0) return;

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

        ObstacleAction action = obstacleCount > 0 && obstacleCount % 3 == 2 ? ObstacleAction.Duck : ObstacleAction.Jump;
        bool wideGround = action == ObstacleAction.Jump && obstacleCount % 3 == 1;
        obstacleCount++;

        float runnerTop = runnerCollider != null ? runnerCollider.bounds.max.y : runner.position.y + 0.8f;
        var pos = new Vector3(spawnX, action == ObstacleAction.Duck ? runnerTop + 0.03f : obstacleY, 0f);
        GameObject obs = action == ObstacleAction.Duck || obstaclePrefab == null
            ? CreateFallbackObstacle(pos) : Instantiate(obstaclePrefab, pos, Quaternion.identity);

        if (action == ObstacleAction.Duck)
        {
            obs.name = "DuckObstacle";
            BoxCollider2D collider = obs.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1.05f, 0.52f);
            collider.offset = Vector2.zero;
            collider.isTrigger = true;
            obs.transform.localScale = Vector3.one;
            obs.transform.Find("Visual").localScale = new Vector3(1.05f, 0.52f, 1f);
            obs.GetComponentInChildren<SpriteRenderer>().color = new Color(0.5f, 0.34f, 0.84f);
        }
        else if (wideGround)
        {
            obs.name = "WideJumpObstacle";
            BoxCollider2D collider = obs.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.68f);
            collider.isTrigger = true;
            obs.GetComponentInChildren<SpriteRenderer>().color = new Color(0.92f, 0.43f, 0.29f);
        }

        SpriteRenderer obstacleRenderer = obs.GetComponentInChildren<SpriteRenderer>();
        if (obstacleRenderer != null)
        {
            obstacleRenderer.sortingOrder = 9;
            if (action == ObstacleAction.Jump && !wideGround)
                obstacleRenderer.color = new Color(0.12f, 0.25f, 0.44f);
        }

        var mover = obs.GetComponent<ObstacleMoverBeat>();
        if (mover == null) mover = obs.AddComponent<ObstacleMoverBeat>();

        float obstacleHalfWidth = GetHalfWidth(obs.GetComponent<Collider2D>());
        float obstacleTopY = GetTopY(obs.GetComponent<Collider2D>(), obstacleY);
        float runnerHalfWidth = GetHalfWidth(runnerCollider);
        float physicsBasedDelay = action == ObstacleAction.Duck ? 0.16f : runnerController != null
            ? runnerController.GetRecommendedObstacleContactDelay(obstacleTopY)
            : 0.38f;

        // Contact follows the beat: the player must first have time to jump or duck.
        desiredContactTime = targetBeatTime + physicsBasedDelay + 0.12f + Mathf.Max(0f, obstacleArrivalDelay);

        if (desiredContactTime - now < minimumTravelTime)
            desiredContactTime = now + minimumTravelTime;

        // Match the actual collider edges rather than the obstacle centre; the old negative
        // padding caused physical contact before the indicated beat.
        float contactTargetX = runnerX + obstacleHalfWidth + runnerHalfWidth;
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
        scheduledObstacles.Add(new ScheduledObstacle { mover = mover, beatTime = targetBeatTime, action = action });
    }

    public ObstacleAction GetActionForBeat(double beatTime)
    {
        for (int i = scheduledObstacles.Count - 1; i >= 0; i--)
        {
            ScheduledObstacle obstacle = scheduledObstacles[i];
            if (obstacle.mover == null)
            {
                scheduledObstacles.RemoveAt(i);
                continue;
            }

            if (System.Math.Abs(obstacle.beatTime - beatTime) < 0.03)
                return obstacle.action;
        }

        return ObstacleAction.Tap;
    }

    public void SetDifficulty(int stage)
    {
        difficultyStage = Mathf.Clamp(stage, 0, 3);
    }

    public void StartSpawner()
    {
        gameStarted = true;
        beatCount = 0;
        obstacleCount = 0;
        difficultyStage = 0;
    }

    public void StopSpawner()
    {
        gameStarted = false;
        foreach (ObstacleMoverBeat obstacle in activeObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle.gameObject);
        }
        activeObstacles.Clear();
        scheduledObstacles.Clear();
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

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(obstacle.transform, false);
        visual.transform.localScale = new Vector3(fallbackObstacleSize.x, fallbackObstacleSize.y, 1f);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFallbackSprite();
        renderer.color = fallbackObstacleColor;
        renderer.sortingOrder = 9;

        var collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = fallbackObstacleSize;

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
            1f);
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
