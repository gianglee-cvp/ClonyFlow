using UnityEditor;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    public sealed class GameplayLayoutSettingsWindow : EditorWindow
    {
        private GameplayLayoutSettings settings;
        private SerializedObject serializedSettings;

        [MenuItem("ColonyFlow/Map/Layout Settings")]
        public static void Open()
        {
            GetWindow<GameplayLayoutSettingsWindow>("Gameplay Layout");
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnGUI()
        {
            if (settings == null) LoadSettings();
            if (settings == null) return;

            serializedSettings.Update();
            EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
            Field("cameraPosition", "Position");
            Field("cameraRotation", "Rotation");
            Field("orthographicSize", "Orthographic Size");
            Field("nearClipPlane", "Near Clip Plane");
            Field("farClipPlane", "Far Clip Plane");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gameplay Layout (Viewport 0-1)", EditorStyles.boldLabel);
            Field("holeViewport", "Ant Hole Position");
            Field("slotCenterViewport", "Slot Row Center");
            Field("slotHorizontalSpacing", "Slot Horizontal Spacing");
            Field("queueOffsetFromSlots", "Queue Offset From Slots");
            Field("queueHorizontalSpacing", "Queue Horizontal Spacing");
            Field("cardViewportMin", "Map Card Min");
            Field("cardViewportMax", "Map Card Max");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gameplay Sizing", EditorStyles.boldLabel);
            Field("mapPadding", "Pixel Art Padding");
            Field("cardInsetPixels", "Card Inner Inset (px)");
            Field("boxViewportWidth", "Slot / Box Width");
            Field("queueRowViewportSpacing", "Queue Row Spacing");
            Field("holeViewportWidth", "Hole Width");
            Field("holeViewportDepth", "Hole Depth");

            if (serializedSettings.ApplyModifiedProperties())
            {
                settings.ValidateValues();
                EditorUtility.SetDirty(settings);
            }

            EditorGUILayout.Space();
            bool cameraIsValid = settings.IsCameraProjectionValid(out string cameraWarning);
            if (!cameraIsValid) EditorGUILayout.HelpBox(cameraWarning, MessageType.Warning);
            using (new EditorGUI.DisabledScope(!cameraIsValid))
            {
                if (GUILayout.Button("Capture Scene Into Legacy Settings"))
                {
                    if (TryCaptureSceneLayout(out string captureError))
                    {
                        SaveSettings();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Gameplay Layout", captureError, "OK");
                    }
                }
                if (GUILayout.Button("Rebuild Scene From Legacy Settings (Destructive)") &&
                    EditorUtility.DisplayDialog("Rebuild Gameplay Scene?",
                        "Thao tác này sẽ xóa và tạo lại GameplayRoot. Các chỉnh sửa thủ công trong layout sẽ bị ghi đè.",
                        "Rebuild", "Cancel"))
                {
                    SaveSettings();
                    FixedLayoutSetup.ApplyFromSettings(settings);
                }
            }
            EditorGUILayout.HelpBox(
                "Workflow mới: chọn GameplayRoot trong Hierarchy, chỉnh Scene Layout Authoring trên AntGameplay, " +
                "sau đó bấm Apply Layout. Queue luôn offset theo hàng slot.", MessageType.Info);
            if (GUILayout.Button("Select GameplayRoot In Scene"))
            {
                var gameplay = Object.FindFirstObjectByType<AntGameplay>();
                if (gameplay != null)
                {
                    Selection.activeGameObject = gameplay.gameObject;
                    EditorGUIUtility.PingObject(gameplay.gameObject);
                }
            }
            if (GUILayout.Button("Load Camera From Scene")) LoadCameraFromScene();
            if (GUILayout.Button("Reset Defaults")) ResetDefaults();
        }

        private bool TryCaptureSceneLayout(out string error)
        {
            var camera = Camera.main;
            if (camera == null) camera = Object.FindFirstObjectByType<Camera>();
            var gameplay = Object.FindFirstObjectByType<AntGameplay>();
            if (camera == null || gameplay == null)
            {
                error = "Open MapDemo and make sure its camera and AntGameplay objects are available.";
                return false;
            }

            var gameplayData = new SerializedObject(gameplay);
            var anchors = gameplayData.FindProperty("boxAnchors");
            var slotSurfaces = gameplayData.FindProperty("slotSurfaces");
            var holeReturn = gameplayData.FindProperty("holeReturn").objectReferenceValue as Transform;
            var cardSurface = gameplayData.FindProperty("mapCardSurface").objectReferenceValue as Renderer;
            if (anchors == null || anchors.arraySize == 0 || holeReturn == null || cardSurface == null)
            {
                error = "The scene layout references are incomplete. Apply Settings To Scene once, then try again.";
                return false;
            }

            Undo.RecordObject(settings, "Capture Gameplay Layout From Scene");
            CaptureSlotRow(camera, anchors);
            settings.holeViewport = Viewport(camera, holeReturn.position);
            CaptureCard(camera, cardSurface);
            CaptureVisualSizes(camera, slotSurfaces, holeReturn);
            settings.ValidateValues();
            EditorUtility.SetDirty(settings);
            serializedSettings.Update();
            error = string.Empty;
            return true;
        }

        private void CaptureSlotRow(Camera camera, SerializedProperty anchors)
        {
            var xValues = new float[anchors.arraySize];
            Vector2 center = Vector2.zero;
            int count = 0;
            for (int i = 0; i < anchors.arraySize; i++)
            {
                var anchor = anchors.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (anchor == null) continue;
                Vector2 viewport = Viewport(camera, anchor.position);
                center += viewport;
                xValues[count++] = viewport.x;
            }
            if (count == 0) return;

            center /= count;
            settings.slotCenterViewport = center;
            if (count < 2) return;

            System.Array.Sort(xValues, 0, count);
            float totalSpacing = 0f;
            for (int i = 1; i < count; i++) totalSpacing += xValues[i] - xValues[i - 1];
            settings.slotHorizontalSpacing = totalSpacing / (count - 1);
        }

        private void CaptureCard(Camera camera, Renderer surface)
        {
            var frame = surface.transform.parent?.Find("Frame")?.GetComponent<Renderer>();
            if (frame == null) return;
            ViewportBounds(camera, frame.bounds, out settings.cardViewportMin, out settings.cardViewportMax);

            float worldUnitsPerPixel = 2f * camera.orthographicSize / 1920f;
            settings.cardInsetPixels = Mathf.Max(0f,
                (frame.bounds.size.x - surface.bounds.size.x) / worldUnitsPerPixel);
        }

        private void CaptureVisualSizes(Camera camera, SerializedProperty slotSurfaces, Transform holeReturn)
        {
            float portraitWorldWidth = 2f * camera.orthographicSize * 1080f / 1920f;
            if (slotSurfaces != null && slotSurfaces.arraySize > 0)
            {
                var slot = slotSurfaces.GetArrayElementAtIndex(0).objectReferenceValue as Renderer;
                if (slot != null) settings.boxViewportWidth = slot.bounds.size.x / portraitWorldWidth;
            }

            var rim = holeReturn.parent?.Find("Hole Rim")?.GetComponent<Renderer>();
            if (rim == null) return;
            settings.holeViewportWidth = rim.bounds.size.x / portraitWorldWidth;
            settings.holeViewportDepth = rim.bounds.size.z / portraitWorldWidth;
        }

        private static void ViewportBounds(Camera camera, Bounds bounds, out Vector2 min, out Vector2 max)
        {
            min = Vector2.one;
            max = Vector2.zero;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var point = new Vector3(bounds.center.x + x * bounds.extents.x, 0f,
                    bounds.center.z + z * bounds.extents.z);
                Vector2 viewport = Viewport(camera, point);
                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }
        }

        private static Vector2 Viewport(Camera camera, Vector3 world)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return new Vector2(viewport.x, viewport.y);
        }

        private void Field(string propertyName, string label)
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty(propertyName), new GUIContent(label));
        }

        private void LoadSettings()
        {
            settings = GameplayLayoutSettings.LoadOrCreate();
            if (settings != null) serializedSettings = new SerializedObject(settings);
        }

        private void LoadCameraFromScene()
        {
            var camera = Camera.main;
            if (camera == null) camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Gameplay Layout", "No camera was found in the open scene.", "OK");
                return;
            }

            Undo.RecordObject(settings, "Load Gameplay Camera Settings");
            settings.cameraPosition = camera.transform.position;
            settings.cameraRotation = camera.transform.eulerAngles;
            settings.orthographicSize = camera.orthographicSize;
            settings.nearClipPlane = camera.nearClipPlane;
            settings.farClipPlane = camera.farClipPlane;
            SaveSettings();
            serializedSettings.Update();
            Repaint();
        }

        private void ResetDefaults()
        {
            Undo.RecordObject(settings, "Reset Gameplay Layout Settings");
            settings.ResetDefaults();
            SaveSettings();
            serializedSettings.Update();
            Repaint();
        }

        private void SaveSettings()
        {
            settings.ValidateValues();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
