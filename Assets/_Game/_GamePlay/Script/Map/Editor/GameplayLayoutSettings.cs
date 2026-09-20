using UnityEditor;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    public sealed class GameplayLayoutSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/_GamePlay/Settings/GameplayLayoutSettings.asset";

        [Header("Camera")]
        public Vector3 cameraPosition;
        public Vector3 cameraRotation;
        public float orthographicSize;
        public float nearClipPlane;
        public float farClipPlane;

        [Header("Gameplay Layout (Viewport 0-1)")]
        public Vector2 holeViewport;
        public Vector2 slotCenterViewport;
        public float slotHorizontalSpacing;
        public Vector2 queueCenterViewport;
        public float queueHorizontalSpacing;
        public Vector2 cardViewportMin;
        public Vector2 cardViewportMax;

        public static GameplayLayoutSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameplayLayoutSettings>(AssetPath);
            if (settings != null) return settings;

            const string folder = "Assets/_Game/_GamePlay/Settings";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Game/_GamePlay", "Settings");

            settings = CreateInstance<GameplayLayoutSettings>();
            settings.ResetDefaults();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public void ResetDefaults()
        {
            cameraPosition = new Vector3(0f, 56.56854f, -60.33054f);
            cameraRotation = new Vector3(45f, 0f, 0f);
            orthographicSize = 9f;
            nearClipPlane = .3f;
            farClipPlane = 300f;
            holeViewport = new Vector2(.5f, .445f);
            slotCenterViewport = new Vector2(.5f, .355f);
            slotHorizontalSpacing = .1525f;
            queueCenterViewport = new Vector2(.5f, .263f);
            queueHorizontalSpacing = .14f;
            cardViewportMin = new Vector2(.04f, .515f);
            cardViewportMax = new Vector2(.96f, .885f);
            ValidateValues();
        }

        public void ValidateValues()
        {
            orthographicSize = Mathf.Max(.01f, orthographicSize);
            nearClipPlane = Mathf.Max(.01f, nearClipPlane);
            farClipPlane = Mathf.Max(nearClipPlane + .01f, farClipPlane);
            holeViewport = ClampViewport(holeViewport);
            slotCenterViewport = ClampViewport(slotCenterViewport);
            slotHorizontalSpacing = Mathf.Clamp01(slotHorizontalSpacing);
            queueCenterViewport = ClampViewport(queueCenterViewport);
            queueHorizontalSpacing = Mathf.Clamp01(queueHorizontalSpacing);
            cardViewportMin = ClampViewport(cardViewportMin);
            cardViewportMax = ClampViewport(cardViewportMax);
            cardViewportMax = new Vector2(
                Mathf.Max(cardViewportMin.x, cardViewportMax.x),
                Mathf.Max(cardViewportMin.y, cardViewportMax.y));
        }

        public bool IsCameraProjectionValid(out string cameraWarning)
        {
            var rotation = Quaternion.Euler(cameraRotation);
            var forward = rotation * Vector3.forward;
            var up = rotation * Vector3.up;
            if (forward.y >= -.001f)
            {
                cameraWarning = "Camera must point down toward the gameplay plane.";
                return false;
            }
            if (Mathf.Abs(up.z) < .001f)
            {
                cameraWarning = "Camera angle makes vertical gameplay spacing undefined.";
                return false;
            }
            if (cameraPosition.y <= orthographicSize * Mathf.Abs(up.y) + .01f)
            {
                cameraWarning = "Camera must remain above the complete gameplay viewport.";
                return false;
            }

            cameraWarning = string.Empty;
            return true;
        }

        private static Vector2 ClampViewport(Vector2 value)
        {
            return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
        }
    }
}
