using UnityEngine;

public class ObstacleMoverBeat : MonoBehaviour
{
    public float speed = 6f;
    public float runnerX = 3f;
    public Transform destroyPoint;
    [System.NonSerialized] public double targetBeatDspTime;
    [System.NonSerialized] public bool avoidedOnBeat;

    public float TimeToRunnerContact
    {
        get
        {
            if (speed <= 0.001f)
                return float.PositiveInfinity;

            return (transform.position.x - runnerX) / speed;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (destroyPoint != null && transform.position.x < destroyPoint.position.x)
            Destroy(gameObject);
    }
}
