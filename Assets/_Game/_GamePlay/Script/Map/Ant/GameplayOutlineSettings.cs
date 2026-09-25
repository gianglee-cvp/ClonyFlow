using UnityEngine;

namespace ColonyFlow.Gameplay
{
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("ColonyFlow/Gameplay Outline Settings")]
    public sealed class GameplayOutlineSettings : MonoBehaviour
    {
        [SerializeField] private bool showOutline = true;
        [SerializeField, Range(1f, 12f)] private float widthPixels = 3f;
        [SerializeField] private Color outlineColor = Color.white;

        internal static GameplayOutlineSettings Active { get; private set; }
        internal bool ShowOutline => showOutline;
        internal float WidthPixels => Mathf.Clamp(widthPixels, 1f, 12f);
        internal Color OutlineColor => outlineColor;

        private void OnEnable() => Active = this;
        private void OnDisable()
        {
            if (Active == this) Active = null;
        }
    }
}
