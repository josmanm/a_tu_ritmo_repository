using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float speed = 6f;
    public Transform destroyPoint;

    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (destroyPoint != null && transform.position.x < destroyPoint.position.x)
        {
            Destroy(gameObject);
        }
    }
}