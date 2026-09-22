using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasLoading : UICanvas
    {
        [SerializeField] private Text loadingText;
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform spinner;
        private float progressMinAnchorX;
        private float progressMaxAnchorX;
        private bool progressBoundsCached;

        public override void Setup()
        {
            base.Setup();
            CacheProgressBounds();
            SetProgress(0);
        }

        private void Update()
        {
            if (spinner != null) spinner.Rotate(0, 0, -120f * Time.unscaledDeltaTime);
        }

        public void SetProgress(float value)
        {
            float progress = Mathf.Clamp01(value);
            if (progressFill != null)
            {
                CacheProgressBounds();
                progressFill.fillAmount = 1;
                if (progressFill.sprite == null) progressFill.type = Image.Type.Simple;
                var anchors = progressFill.rectTransform.anchorMax;
                anchors.x = Mathf.Lerp(progressMinAnchorX, progressMaxAnchorX, progress);
                progressFill.rectTransform.anchorMax = anchors;
            }
            if (loadingText != null) loadingText.text = "LOADING " + Mathf.RoundToInt(progress * 100) + "%";
        }

        private void CacheProgressBounds()
        {
            if (progressBoundsCached || progressFill == null) return;
            progressMinAnchorX = progressFill.rectTransform.anchorMin.x;
            progressMaxAnchorX = progressFill.rectTransform.anchorMax.x;
            progressBoundsCached = true;
        }
    }
}