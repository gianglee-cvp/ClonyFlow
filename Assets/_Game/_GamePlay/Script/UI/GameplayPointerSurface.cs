using UnityEngine;
using UnityEngine.EventSystems;

namespace ColonyFlow.Gameplay
{
    public sealed class GameplayPointerSurface : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private CanvasGamePlay owner;

        public void Configure(CanvasGamePlay canvas) => owner = canvas;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner != null && eventData != null) owner.HandleWorldPointer(eventData.position);
        }
    }
}
