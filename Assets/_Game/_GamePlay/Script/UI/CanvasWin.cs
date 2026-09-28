using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasWin : UICanvas
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button homeButton;
        [Header("Celebration")]
        [SerializeField] private GameObject fireworkPrefab;
        [SerializeField] private GameObject confettiPrefab;
        [SerializeField] private Sprite lightSprite;
        [SerializeField] private Sprite stickerSprite;

        private bool hasNextLevel;
        private bool wired;
        private bool decorationsBuilt;
        private RectTransform lightBack, lightFront, progressFill;
        private Text progressText;
        private RectTransform[] stickerSlots;
        private RectTransform[] rainPieces;
        private float[] rainSpeeds;
        private Coroutine celebrationRoutine, stickerRoutine, actionRoutine;
        private bool firstVolleyDone, stickersDone, introDone;
        private readonly List<BoxFireworkEffect> activeEffects = new List<BoxFireworkEffect>();

        public void Init(int completedLevel, bool hasNext)
        {
            StopEffects();
            hasNextLevel = hasNext;
            firstVolleyDone = stickersDone = introDone = false;
            if (levelText != null) levelText.text = "Level " + completedLevel + " Complete";
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (homeButton != null) homeButton.gameObject.SetActive(false);
            BuildDecorations();
            int progress = (completedLevel - 1) % 5 + 1;
            if (progressText != null) progressText.text = "FEATURE PROGRESS  " + progress + "/5";
            if (progressFill != null) progressFill.anchorMax = new Vector2(0, 1);
            celebrationRoutine = StartCoroutine(Celebrate());
            stickerRoutine = StartCoroutine(AnimateRewards(progress));
            actionRoutine = StartCoroutine(ShowActionAfterIntro());
        }

        public override void Setup()
        {
            base.Setup();
            if (wired) return;
            if (continueButton != null) continueButton.onClick.AddListener(ButtonContinue);
            if (homeButton != null) homeButton.onClick.AddListener(ButtonHome);
            wired = true;
            BuildDecorations();
        }

        public override void Close(float delay = 0) { StopEffects(); base.Close(delay); }
        public override void CloseDirectly() { StopEffects(); base.CloseDirectly(); }
        private void OnDisable() => StopEffects();

        private void StopEffects()
        {
            if (celebrationRoutine != null) StopCoroutine(celebrationRoutine);
            if (stickerRoutine != null) StopCoroutine(stickerRoutine);
            if (actionRoutine != null) StopCoroutine(actionRoutine);
            celebrationRoutine = stickerRoutine = actionRoutine = null;
            foreach (var effect in activeEffects)
                if (effect != null) effect.StopNow();
            activeEffects.Clear();
            introDone = false;
        }

        private void BuildDecorations()
        {
            if (decorationsBuilt || levelText == null) return;
            decorationsBuilt = true;
            var card = (RectTransform)levelText.transform.parent;
            card.anchorMin = new Vector2(.12f, .22f);
            card.anchorMax = new Vector2(.88f, .78f);
            levelText.rectTransform.anchorMin = new Vector2(.05f, .60f);
            levelText.rectTransform.anchorMax = new Vector2(.95f, .72f);
            var title = card.Find("Title") as RectTransform;
            if (title != null) { title.anchorMin = new Vector2(.05f, .76f); title.anchorMax = new Vector2(.95f, .94f); }
            PositionButton(continueButton);
            PositionButton(homeButton);

            lightBack = Image("Light Ray Back", card, lightSprite, new Color(1f, .91f, .55f, .28f),
                new Vector2(.5f, .61f), new Vector2(570, 570));
            lightBack.SetAsFirstSibling();
            lightFront = Image("Light Ray Front", card, lightSprite, new Color(1f, .97f, .75f, .22f),
                new Vector2(.5f, .61f), new Vector2(450, 450));
            lightFront.SetSiblingIndex(1);

            progressText = Text("Feature Progress", card, "FEATURE PROGRESS", 24, new Color(.35f, .22f, .10f));
            progressText.rectTransform.anchorMin = new Vector2(.1f, .51f);
            progressText.rectTransform.anchorMax = new Vector2(.9f, .59f);
            var track = Image("Feature Track", card, null, new Color(.69f, .56f, .40f),
                new Vector2(.5f, .45f), new Vector2(0, 28));
            track.anchorMin = new Vector2(.14f, .45f);
            track.anchorMax = new Vector2(.86f, .45f);
            progressFill = Image("Feature Fill", track, null, new Color(.99f, .71f, .25f),
                new Vector2(.5f, .5f), Vector2.zero);
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = Vector2.one;
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;

            stickerSlots = new RectTransform[3];
            for (int i = 0; i < stickerSlots.Length; i++)
            {
                var slot = Image("Sticker Reward " + (i + 1), card, null, new Color(.89f, .79f, .61f),
                    new Vector2(.29f + i * .21f, .32f), new Vector2(90, 90));
                var icon = Image("Sticker", slot, stickerSprite, Color.white, new Vector2(.5f, .5f), new Vector2(70, 70));
                icon.gameObject.SetActive(false);
                stickerSlots[i] = icon;
            }
            var canvasRect = (RectTransform)transform;
            rainPieces = new RectTransform[24];
            rainSpeeds = new float[rainPieces.Length];
            Color[] colors = { new Color(1f, .75f, .3f), new Color(.5f, .8f, 1f),
                new Color(1f, .48f, .65f), new Color(.75f, 1f, .52f) };
            for (int i = 0; i < rainPieces.Length; i++)
            {
                var piece = Image("Confetti Rain", canvasRect, null, colors[i % colors.Length],
                    new Vector2(.5f, .5f), new Vector2(9, 18));
                piece.SetAsLastSibling();
                piece.anchoredPosition = new Vector2(Random.Range(-500f, 500f), Random.Range(-900f, 900f));
                piece.localRotation = Quaternion.Euler(0, 0, Random.Range(-45f, 45f));
                rainPieces[i] = piece;
                rainSpeeds[i] = Random.Range(130f, 260f);
            }
        }

        private static void PositionButton(Button button)
        {
            if (button == null) return;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(.15f, .08f);
            rect.anchorMax = new Vector2(.85f, .20f);
        }

        private static RectTransform Image(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return rect;
        }

        private static Text Text(string name, Transform parent, string value, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private IEnumerator Celebrate()
        {
            bool firstRound = true;
            while (true)
            {
                yield return new WaitForSecondsRealtime(.45f);
                Fire(confettiPrefab, .5f, .55f, 1f); // left and right cannons in the prefab
                yield return new WaitForSecondsRealtime(.17f);
                for (int i = 0; i < 5; i++)
                {
                    Fire(fireworkPrefab, i % 2 == 0 ? Random.Range(.18f, .42f) : Random.Range(.58f, .82f),
                        Random.Range(.35f, .82f), .7f);
                    yield return new WaitForSecondsRealtime(.17f);
                }
                if (firstRound) { firstVolleyDone = true; firstRound = false; }
                yield return new WaitForSecondsRealtime(2.2f);
            }
        }

        private IEnumerator ShowActionAfterIntro()
        {
            yield return new WaitUntil(() => firstVolleyDone && stickersDone);
            introDone = true;
            if (continueButton != null) continueButton.gameObject.SetActive(hasNextLevel);
            if (homeButton != null) homeButton.gameObject.SetActive(!hasNextLevel);
            actionRoutine = null;
        }

        private void Fire(GameObject prefab, float x, float y, float scale)
        {
            var effect = BoxFireworkEffect.PlayAtScreen(prefab,
                new Vector2(Screen.width * x, Screen.height * y), scale);
            if (effect == null) return;
            activeEffects.RemoveAll(item => item == null);
            activeEffects.Add(effect);
        }

        private IEnumerator AnimateRewards(int progress)
        {
            foreach (var slot in stickerSlots) slot.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(.35f);
            int count = progress >= 5 ? 3 : progress >= 3 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                var slot = stickerSlots[i];
                slot.gameObject.SetActive(true);
                Vector3 end = slot.position;
                Vector3 start = end + Vector3.up * 300f * GetComponentInParent<Canvas>().scaleFactor;
                float elapsed = 0;
                const float duration = .42f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                    slot.position = Vector3.Lerp(start, end, t);
                    slot.localScale = Vector3.one * Mathf.Lerp(1.45f, 1f, t);
                    yield return null;
                }
                slot.position = end;
                slot.localScale = Vector3.one;
                WinFeedback.LightImpact();
                yield return new WaitForSecondsRealtime(.13f);
            }
            float fill = 0, target = progress / 5f;
            while (fill < target)
            {
                fill = Mathf.MoveTowards(fill, target, Time.unscaledDeltaTime * .7f);
                progressFill.anchorMax = new Vector2(fill, 1);
                yield return null;
            }
            stickersDone = true;
            stickerRoutine = null;
        }

        private void Update()
        {
            if (lightBack != null) lightBack.Rotate(0, 0, 18f * Time.unscaledDeltaTime);
            if (lightFront != null) lightFront.Rotate(0, 0, -24f * Time.unscaledDeltaTime);
            if (rainPieces == null) return;
            float width = ((RectTransform)transform).rect.width;
            float height = ((RectTransform)transform).rect.height;
            for (int i = 0; i < rainPieces.Length; i++)
            {
                var piece = rainPieces[i];
                var point = piece.anchoredPosition;
                point.y -= rainSpeeds[i] * Time.unscaledDeltaTime;
                if (point.y < -height * .5f - 30f)
                    point = new Vector2(Random.Range(-width * .5f, width * .5f), height * .5f + 30f);
                piece.anchoredPosition = point;
                piece.Rotate(0, 0, 55f * Time.unscaledDeltaTime);
            }
        }

        public void ButtonContinue() { if (introDone && hasNextLevel) GameManager.Instance.ContinueLevel(); }
        public void ButtonHome() { if (introDone) GameManager.Instance.ShowMenu(); }
    }
}
