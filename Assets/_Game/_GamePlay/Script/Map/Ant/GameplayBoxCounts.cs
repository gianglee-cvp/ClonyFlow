using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class GameplayBoxCounts : MonoBehaviour
    {
        [SerializeField] private Text countTemplate;
        [SerializeField] private Vector2 screenOffset;
        private readonly Dictionary<BoxActor, Text> labels = new();
        private readonly List<BoxActor> expired = new();
        private readonly Stack<Text> spare = new();
        private Canvas canvas;

        private void OnEnable()
        {
            canvas = GetComponentInParent<Canvas>();
            if (countTemplate != null) countTemplate.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (countTemplate == null || canvas == null) return;
            expired.Clear();
            foreach (var entry in labels)
                if (entry.Key == null || !BoxActor.CountBoxes.Contains(entry.Key)) expired.Add(entry.Key);
            foreach (var box in expired)
            {
                labels[box].gameObject.SetActive(false);
                spare.Push(labels[box]);
                labels.Remove(box);
            }
            var parent = (RectTransform)countTemplate.transform.parent;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            foreach (var box in BoxActor.CountBoxes)
            {
                if (box == null || box.CountCamera == null) continue;
                if (!labels.TryGetValue(box, out var label))
                {
                    label = spare.Count > 0 ? spare.Pop() : Instantiate(countTemplate, parent);
                    label.name = "Box Count";
                    label.enabled = true;
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.verticalOverflow = VerticalWrapMode.Overflow;
                    label.raycastTarget = false;
                    labels.Add(box, label);
                }
                Vector3 point = box.CountCamera.WorldToScreenPoint(box.CountPosition);
                bool visible = point.z > 0 && (box.CountCamera.cullingMask & (1 << box.gameObject.layer)) != 0;
                label.gameObject.SetActive(visible);
                if (!visible) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, point, uiCamera, out var local);
                label.rectTransform.localPosition = local + screenOffset;
                string value = box.AntCount.ToString();
                if (label.text != value) label.text = value;
            }
        }

        private void OnDisable()
        {
            foreach (var label in labels.Values)
                if (label != null) { label.gameObject.SetActive(false); spare.Push(label); }
            labels.Clear();
        }
    }
}
