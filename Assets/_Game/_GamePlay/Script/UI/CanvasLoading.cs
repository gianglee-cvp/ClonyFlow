using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public sealed class CanvasLoading : UICanvas
    {
        [SerializeField] private Text loadingText;
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform spinner;

        public override void Setup()
        {
            base.Setup();
            SetProgress(0);
        }

        private void Update()
        {
            if (spinner != null) spinner.Rotate(0, 0, -120f * Time.unscaledDeltaTime);
        }

        public void SetProgress(float value)
        {
            float progress = Mathf.Clamp01(value);
            if (progressFill != null) progressFill.fillAmount = progress;
            if (loadingText != null) loadingText.text = "LOADING " + Mathf.RoundToInt(progress * 100) + "%";
        }
    }
}