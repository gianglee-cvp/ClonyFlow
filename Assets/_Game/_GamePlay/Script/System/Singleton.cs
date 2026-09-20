using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                        instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null) instance = this as T;
            else if (instance != this) gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
