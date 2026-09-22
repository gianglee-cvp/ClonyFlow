using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasMenu : UICanvas
    {
        private static readonly string[] TabNames =
            { "Shop Tab", "Collection Tab", "Home", "Event Tab", "Setting Tab" };

        [SerializeField] private Text levelText;
        [SerializeField] private Button playButton;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private RectTransform selectionCard;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private RectTransform[] tabRects;
        [SerializeField, Min(1f)] private float selectedScale = 1.18f;
        [SerializeField] private float selectedYOffset = 26f;
        [SerializeField, Min(0f)] private float tabTweenDuration = .2f;
        private Vector3[] baseLocalPositions;
        private int selectedTabIndex = 2;
        private bool wired;

        public override void Setup()
        {
            ResolveBottomTabs();
            base.Setup();
            WireButtons();
            if (levelText != null)
                levelText.text = "LEVEL\n" + LevelManager.Instance.CurrentLevel;
        }

        public override void Open()
        {
            base.Open();
            ResolveBottomTabs();
            Canvas.ForceUpdateCanvases();
            if (bottomBar != null) LayoutRebuilder.ForceRebuildLayoutImmediate(bottomBar);
            CacheBasePositions();
            ApplySelection(selectedTabIndex, false);
        }

        private void ResolveBottomTabs()
        {
            if (bottomBar == null) bottomBar = transform.Find("Bottom Bar") as RectTransform;
            if (bottomBar == null) return;
            var layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childScaleWidth = false;
                layout.childScaleHeight = false;
            }
            EnsureSelectionCard();
            if (tabRects == null || tabRects.Length != TabNames.Length)
                tabRects = new RectTransform[TabNames.Length];
            if (tabButtons == null || tabButtons.Length != TabNames.Length)
                tabButtons = new Button[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                if (tabRects[i] == null)
                {
                    var tab = bottomBar.Find(TabNames[i]);
                    if (tab == null && i == 2) tab = bottomBar.Find("Home Tab");
                    tabRects[i] = tab as RectTransform;
                }
                if (tabRects[i] == null) continue;
                var graphic = tabRects[i].GetComponent<Graphic>();
                if (graphic != null) graphic.raycastTarget = true;
                if (tabButtons[i] == null) tabButtons[i] = tabRects[i].GetComponent<Button>();
                if (tabButtons[i] == null) tabButtons[i] = tabRects[i].gameObject.AddComponent<Button>();
                if (tabButtons[i].targetGraphic == null) tabButtons[i].targetGraphic = graphic;
            }
        }

        private void EnsureSelectionCard()
        {
            if (selectionCard == null) selectionCard = bottomBar.Find("Selection Card") as RectTransform;
            if (selectionCard == null)
            {
                var owner = new GameObject("Selection Card", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(LayoutElement));
                selectionCard = owner.GetComponent<RectTransform>();
                selectionCard.SetParent(bottomBar, false);
                selectionCard.anchorMin = selectionCard.anchorMax = new Vector2(.5f, 0);
                selectionCard.pivot = new Vector2(.5f, 0);
                selectionCard.sizeDelta = new Vector2(216, 250);
            }
            var image = selectionCard.GetComponent<Image>();
            if (image == null) image = selectionCard.gameObject.AddComponent<Image>();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(.28f, .48f, .85f, 1);
            image.raycastTarget = false;
            var layoutElement = selectionCard.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = selectionCard.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            selectionCard.SetAsFirstSibling();
        }

        private void WireButtons()
        {
            if (wired) return;
            if (playButton != null && playButton.onClick.GetPersistentEventCount() == 0)
                playButton.onClick.AddListener(ButtonPlay);
            if (tabButtons != null)
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    if (tabButtons[i] == null) continue;
                    int index = i;
                    tabButtons[i].onClick.AddListener(() => SelectTab(index));
                }
            wired = true;
        }

        private void CacheBasePositions()
        {
            if (tabRects == null) return;
            baseLocalPositions = new Vector3[tabRects.Length];
            for (int i = 0; i < tabRects.Length; i++)
            {
                if (tabRects[i] == null) continue;
                tabRects[i].DOKill();
                tabRects[i].localScale = Vector3.one;
            }
            if (bottomBar != null) LayoutRebuilder.ForceRebuildLayoutImmediate(bottomBar);
            for (int i = 0; i < tabRects.Length; i++)
                if (tabRects[i] != null) baseLocalPositions[i] = tabRects[i].localPosition;
        }

        private void SelectTab(int index)
        {
            if (index < 0 || tabRects == null || index >= tabRects.Length || tabRects[index] == null) return;
            selectedTabIndex = index;
            ApplySelection(index, true);
        }

        private void ApplySelection(int index, bool animated)
        {
            if (tabRects == null || baseLocalPositions == null || index < 0 || index >= tabRects.Length) return;
            float duration = animated ? tabTweenDuration : 0;
            for (int i = 0; i < tabRects.Length; i++)
            {
                var tab = tabRects[i];
                if (tab == null) continue;
                tab.DOKill();
                var targetScale = i == index ? Vector3.one * selectedScale : Vector3.one;
                float targetY = baseLocalPositions[i].y + (i == index ? selectedYOffset : 0);
                if (duration > 0)
                {
                    tab.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
                    tab.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad);
                }
                else
                {
                    tab.localScale = targetScale;
                    var position = baseLocalPositions[i]; position.y = targetY; tab.localPosition = position;
                }
            }
            MoveSelectionCard(index, duration);
        }

        private void MoveSelectionCard(int index, float duration)
        {
            if (selectionCard == null || bottomBar == null || tabRects[index] == null) return;
            selectionCard.DOKill();
            var target = bottomBar.InverseTransformPoint(tabRects[index].TransformPoint(tabRects[index].rect.center));
            selectionCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tabRects[index].rect.width + 12);
            if (duration > 0) selectionCard.DOLocalMoveX(target.x, duration).SetEase(Ease.OutQuad);
            else
            {
                var position = selectionCard.localPosition; position.x = target.x; selectionCard.localPosition = position;
            }
        }

        public void ButtonPlay() => GameManager.Instance.PlayCurrentLevel();

        private void OnDestroy()
        {
            selectionCard?.DOKill();
            if (tabRects == null) return;
            foreach (var tab in tabRects) tab?.DOKill();
        }
    }
}
