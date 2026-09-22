using UnityEngine;
using UnityEngine.UI;

namespace ColonyFlow.Gameplay
{
    public class UICanvas : MonoBehaviour
    {
        private bool buttonSfxWired;

        public virtual void Setup()
        {
            if (buttonSfxWired) return;
            foreach (var button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(PlayButtonClick);
            buttonSfxWired = true;
        }

        private static void PlayButtonClick() => AudioManager.Instance.PlaySfx(AudioCue.ButtonClick);
        public virtual void Open() => gameObject.SetActive(true);
        public virtual void Close(float delay = 0) => gameObject.SetActive(false);
        public virtual void CloseDirectly() => gameObject.SetActive(false);
    }
}