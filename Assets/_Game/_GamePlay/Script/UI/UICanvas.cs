using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public class UICanvas : MonoBehaviour
    {
        public virtual void Setup() { }
        public virtual void Open() => gameObject.SetActive(true);
        public virtual void Close(float delay = 0) => gameObject.SetActive(false);
        public virtual void CloseDirectly() => gameObject.SetActive(false);
    }
}
