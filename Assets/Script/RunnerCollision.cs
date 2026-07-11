using UnityEngine;

public class RunnerCollision : MonoBehaviour
{
    public TempoTapGameManager game;
    public float stabilityPenalty = 0.2f;

    RunnerController2D runner;

    void Awake()
    {
        runner = GetComponent<RunnerController2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsObstacle(collision.collider))
        {
            if (runner != null && runner.IsInvulnerable)
            {
                Destroy(collision.gameObject);
                return;
            }

            if (game != null)
                game.RegisterCollision(stabilityPenalty);

            if (runner != null)
                runner.TriggerHitInvulnerability();

            Destroy(collision.gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsObstacle(other))
            return;

        if (runner != null && runner.IsInvulnerable)
        {
            Destroy(other.gameObject);
            return;
        }

        if (game != null)
            game.RegisterCollision(stabilityPenalty);

        if (runner != null)
            runner.TriggerHitInvulnerability();

        Destroy(other.gameObject);
    }

    bool IsObstacle(Collider2D other)
    {
        if (other == null)
            return false;

        return other.CompareTag("Obstacle") || other.GetComponent<ObstacleMoverBeat>() != null;
    }
}
