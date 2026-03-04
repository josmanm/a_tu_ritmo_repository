using UnityEngine;

public class PulseOnBeat : MonoBehaviour
{
    public BeatController beatController;
    public float scaleUp = 1.12f;
    public float speed = 10f;

    Vector3 baseScale;
    float target = 1f;

    void Start()
    {
        baseScale = transform.localScale;
        if (beatController != null)
            beatController.OnBeat += _ => target = scaleUp;
    }

    void Update()
    {
        target = Mathf.Lerp(target, 1f, Time.deltaTime * speed);
        transform.localScale = baseScale * target;
    }
}