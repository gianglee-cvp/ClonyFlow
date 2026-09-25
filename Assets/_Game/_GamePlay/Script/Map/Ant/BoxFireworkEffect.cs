using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class BoxFireworkEffect : MonoBehaviour
    {
        public static void Play(GameObject prefab, Vector3 worldPosition, Camera camera, float scale)
        {
            if (camera == null) return;
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0) return;

            // UIParticle needs a Canvas. Keep it independent of the recycled box.
            var root = new GameObject("Box Firework Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(BoxFireworkEffect));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvas.targetDisplay = camera.targetDisplay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            scaler.enabled = true;
            Canvas.ForceUpdateCanvases();

            var effect = Instantiate(prefab, root.transform, false);
            var rect = effect.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)root.transform, screenPoint, null, out var localPoint);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = localPoint;
            rect.localScale = Vector3.one * scale;
            foreach (var graphic in effect.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            effect.SetActive(true);
            var lifetime = root.GetComponent<BoxFireworkEffect>();
            lifetime.StartCoroutine(lifetime.WaitForParticles(effect));
        }

        private IEnumerator WaitForParticles(GameObject effect)
        {
            var particles = effect.GetComponentsInChildren<ParticleSystem>(true);
            // Allow UIParticle to initialize its simulation before checking IsAlive.
            yield return null;
            yield return null;
            float elapsed = 0;
            while (elapsed < 15f)
            {
                bool alive = false;
                foreach (var particle in particles)
                    if (particle != null && particle.IsAlive(false)) { alive = true; break; }
                if (!alive && elapsed > .5f) break;
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
