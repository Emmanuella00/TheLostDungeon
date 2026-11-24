using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>A screen the UIManager can show, hide and update.</summary>
    public interface IUIScreen
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
        void Tick(float unscaledDeltaTime);
    }

    /// <summary>
    /// Base class for every screen (TEMPLATE METHOD: the base handles creation, fading and
    /// gamepad focus; each screen only fills in Build / OnShow / OnTick).
    ///
    /// Screens are plain C# objects that own a GameObject under the UI canvas. They talk to the
    /// game through manager APIs and GameEvents only, so the game code never references the UI.
    /// </summary>
    public abstract class UIScreen : IUIScreen
    {
        private const float FadeSpeed = 8f;

        protected UIManager UI { get; private set; }
        protected RectTransform Root { get; private set; }
        private CanvasGroup group;

        public bool IsVisible => Root != null && Root.gameObject.activeSelf;

        /// <summary>
        /// Builds the screen under the parent, or binds to it if the screen already exists in
        /// the baked GameUI prefab (see UIBuilder: existing elements keep your edits).
        /// </summary>
        public void Create(UIManager ui, Transform parent)
        {
            UI = ui;
            Root = UIBuilder.Stretch(UIBuilder.Rect(GetType().Name, parent));
            group = UIBuilder.GetOrAdd<CanvasGroup>(Root.gameObject);
            Build();
            Root.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (IsVisible) return;
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            group.alpha = 0f;
            OnShow();
            Focus(FirstSelected);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            OnHide();
            Root.gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (!IsVisible) return;
            if (group.alpha < 1f) group.alpha = Mathf.MoveTowards(group.alpha, 1f, dt * FadeSpeed);
            OnTick(dt);
        }

        /// <summary>Selects a control so keyboard / gamepad navigation works immediately.</summary>
        protected static void Focus(Selectable selectable)
        {
            if (selectable == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        protected abstract void Build();
        protected virtual Selectable FirstSelected => null;
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnTick(float dt) { }

        /// <summary>Called once by the UIManager; subscribe to GameEvents here.</summary>
        public virtual void Subscribe() { }
        public virtual void Unsubscribe() { }
    }
}
