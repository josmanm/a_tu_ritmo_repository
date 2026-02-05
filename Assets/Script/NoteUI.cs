using UnityEngine;
using UnityEngine.UI;

public class NoteUI : MonoBehaviour
{
    public int laneIndex;
    public bool wasHit;
    public bool isResolving;

    [HideInInspector] public Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
    }
}