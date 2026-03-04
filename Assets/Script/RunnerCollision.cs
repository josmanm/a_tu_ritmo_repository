using UnityEngine;

public class RunnerCollision : MonoBehaviour
{
    public TempoTapGameManager game;
    public float stabilityPenalty = 0.2f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Obstacle"))
        {
            if (game != null)
            {
                game.stability = Mathf.Clamp01(game.stability - stabilityPenalty);
            }
            Destroy(collision.gameObject);
        }
    }
}