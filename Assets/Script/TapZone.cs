using UnityEngine;
using UnityEngine.EventSystems;

public class TapZone : MonoBehaviour, IPointerDownHandler
{
    public TempoTapGameManager game;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (game) game.RegisterTap();
    }
}