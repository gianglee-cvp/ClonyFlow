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
            Field("queueCenterViewport", "Queue Row Center");
            Field("queueHorizontalSpacing", "Queue Horizontal Spacing");
            Field("cardViewportMin", "Map Card Min");
            Field("cardViewportMax", "Map Card Max");

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
                if (GUILayout.Button("Apply Layout"))
                {
                    SaveSettings();
                    FixedLayoutSetup.ApplyFromSettings(settings);
                }
            }
            if (GUILayout.Button("Load Camera From Scene")) LoadCameraFromScene();
            if (GUILayout.Button("Reset Defaults")) ResetDefaults();
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
