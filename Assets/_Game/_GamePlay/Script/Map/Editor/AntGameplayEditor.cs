using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColonyFlow.Gameplay.Editor
{
    [CustomEditor(typeof(AntGameplay))]
    public sealed class AntGameplayEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var gameplay = (AntGameplay)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layout Workflow", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Lần đầu: đặt các object theo ý muốn rồi bấm Capture Current Scene. " +
                "Sau đó sửa Scene Layout Authoring và bấm Apply Layout, hoặc bật Live Layout Preview.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Current Scene")) Capture(gameplay);
                if (GUILayout.Button("Apply Layout")) Apply(gameplay);
            }
        }

        private static void Capture(AntGameplay gameplay)
        {
            Undo.RecordObject(gameplay, "Capture Gameplay Scene Layout");
            if (!gameplay.CaptureSceneLayout())
            {
                EditorUtility.DisplayDialog("Gameplay Layout",
                    "GameplayRoot đang thiếu reference cần thiết để đọc layout.", "OK");
                return;
            }
            EditorUtility.SetDirty(gameplay);
            MarkSceneDirty(gameplay);
        }

        private static void Apply(AntGameplay gameplay)
        {
            Undo.RegisterFullObjectHierarchyUndo(gameplay.gameObject, "Apply Gameplay Scene Layout");
            var cameraProperty = new SerializedObject(gameplay).FindProperty("gameplayCamera");
            var camera = cameraProperty?.objectReferenceValue as Camera;
            if (camera != null)
            {
                Undo.RecordObject(camera, "Apply Gameplay Scene Layout");
                Undo.RecordObject(camera.transform, "Apply Gameplay Scene Layout");
            }
            if (!gameplay.ApplySceneLayout())
            {
                EditorUtility.DisplayDialog("Gameplay Layout",
                    "GameplayRoot đang thiếu reference cần thiết để áp dụng layout.", "OK");
                return;
            }
            EditorUtility.SetDirty(gameplay);
            MarkSceneDirty(gameplay);
            SceneView.RepaintAll();
        }

        private static void MarkSceneDirty(AntGameplay gameplay)
        {
            if (gameplay.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameplay.gameObject.scene);
        }
    }
}
