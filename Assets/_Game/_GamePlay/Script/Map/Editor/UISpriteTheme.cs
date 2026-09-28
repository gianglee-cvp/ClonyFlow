using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay.Editor
{
    public static class UISpriteTheme
    {
        private const string Root = "Assets/_Game/GUIPackage/UI/";
        private const string Prefabs = "Assets/Resources/UI/";

        [MenuItem("ColonyFlow/UI/Apply Imported Sprite Theme")]
        public static void Apply()
        {
            ApplyGameplay();
            ApplyMenu();
            ApplyWin();
            ApplyLose();
            AssetDatabase.SaveAssets();
        }

        private static void ApplyGameplay()
        {
            var prefab = Open("CanvasGamePlay");
            SetImage(prefab, "Booster Bar", "Common/frameback.png");
            SetImage(prefab, "Add Slot Button", "Common/button_orange.png");
            SetImage(prefab, "Pickup Button", "Common/button_green.png");
            SetImage(prefab, "Blow Button", "Common/button_red.png");
            SetImage(prefab, "Pause Button", "Assets/_Game/_GamePlay/Sprite/ChatGPT Image Sep 25, 2026, 02_25_11 PM-1.png");
            SetImage(prefab, "Speed Button", "Assets/_Game/_GamePlay/Sprite/ChatGPT Image Sep 25, 2026, 02_25_13 PM-2.png");
            Save(prefab, "CanvasGamePlay");
        }

        private static void ApplyMenu()
        {
            var prefab = Open("CanvasMenu");
            SetImage(prefab, "Play Button", "Common/button_green.png");
            Save(prefab, "CanvasMenu");
        }

        private static void ApplyWin()
        {
            var prefab = Open("CanvasWin");
            SetImage(prefab, "Win Card", "Popup_Win/frame_lv complete.png");
            SetImage(prefab, "Win Card/Continue Button", "Common/button_green.png");
            SetImage(prefab, "Win Card/Home Button", "Common/button_orange.png");
            var card = prefab.transform.Find("Win Card");
            var title = card != null ? card.Find("Title") as RectTransform : null;
            if (title != null)
            {
                var ribbon = card.Find("Title Ribbon") as RectTransform;
                if (ribbon == null)
                {
                    var go = new GameObject("Title Ribbon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    ribbon = (RectTransform)go.transform;
                    ribbon.SetParent(card, false);
                }
                ribbon.anchorMin = new Vector2(.05f, .76f);
                ribbon.anchorMax = new Vector2(.95f, .94f);
                ribbon.offsetMin = ribbon.offsetMax = Vector2.zero;
                ribbon.SetSiblingIndex(title.GetSiblingIndex());
                var image = ribbon.GetComponent<Image>();
                image.sprite = Sprite("Popup_Win/tittle.png");
                image.color = Color.white;
                image.raycastTarget = false;
            }
            SetSpriteField(prefab, "lightSprite", "Popup_Win/light_ray.png");
            SetSpriteField(prefab, "progressTrackSprite", "Popup_Win/bar.png");
            SetSpriteField(prefab, "progressFillSprite", "Popup_Win/progress bar_green.png");
            SetSpriteField(prefab, "rewardFrameSprite", "Popup_Win/frame_show.png");
            Save(prefab, "CanvasWin");
        }

        private static void ApplyLose()
        {
            var prefab = Open("CanvasLose");
            SetImage(prefab, "Lose Card", "Popup_Win/frame_lv complete.png");
            SetImage(prefab, "Lose Card/Retry Button", "Common/button_orange.png");
            SetImage(prefab, "Lose Card/Home Button", "Common/button_red.png");
            Save(prefab, "CanvasLose");
        }

        private static GameObject Open(string name) =>
            PrefabUtility.LoadPrefabContents(Prefabs + name + ".prefab");

        private static void Save(GameObject prefab, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(prefab, Prefabs + name + ".prefab");
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        private static Sprite Sprite(string relativePath)
        {
            string path = relativePath.StartsWith("Assets/") ? relativePath : Root + relativePath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new System.InvalidOperationException("Missing UI sprite: " + path);
            return sprite;
        }

        private static void SetImage(GameObject prefab, string objectPath, string spritePath)
        {
            var target = prefab.transform.Find(objectPath);
            if (target == null) return;
            var image = target.GetComponent<Image>();
            if (image == null) return;
            var sprite = Sprite(spritePath);
            image.sprite = sprite;
            image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
        }

        private static void SetSpriteField(GameObject prefab, string field, string spritePath)
        {
            var view = prefab.GetComponent<CanvasWin>();
            if (view == null) return;
            var serialized = new SerializedObject(view);
            var property = serialized.FindProperty(field);
            if (property == null) throw new System.InvalidOperationException("Missing CanvasWin sprite field: " + field);
            property.objectReferenceValue = Sprite(spritePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
