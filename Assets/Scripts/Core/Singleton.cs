using UnityEngine;

namespace TheLostDungeon.Core
{
    /// <summary>
    /// SINGLETON PATTERN — base class for global services (GameManager, SaveManager, ...).
    ///
    /// Guarantees exactly one instance exists and gives every system a single, well-known
    /// access point (<c>GameManager.Instance</c>). The object survives scene loads so data
    /// such as save progress or the music track isn't lost when a level changes.
    ///
    /// Only use this for genuinely global services. Gameplay objects (enemies, doors,
    /// pickups) should NOT be singletons; they communicate through GameEvents instead.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // A second copy appeared (e.g. a Systems prefab placed in a scene by hand).
                // Destroy only this component, so the original keeps working.
                Destroy(this);
                return;
            }

            Instance = (T)this;
            DontDestroyOnLoad(transform.root.gameObject);
            OnSingletonAwake();
        }

        /// <summary>Use instead of Awake in subclasses; only runs on the surviving instance.</summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
