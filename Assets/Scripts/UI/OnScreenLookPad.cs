using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// Touch "look" area: dragging a finger over it turns the camera.
    ///
    /// Like the Input System's own OnScreenStick / OnScreenButton, it feeds a virtual gamepad
    /// control (the right stick by default), so the PlayerController needs no touch-specific code:
    /// it just reads its LookStick action as usual. The drag speed becomes a stick deflection, and
    /// it springs back to zero as soon as the finger stops moving or lifts.
    /// </summary>
    public class OnScreenLookPad : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] private string controlPathValue = "<Gamepad>/rightStick";

        [Tooltip("How much stick deflection one pixel of drag (per frame) produces.")]
        [SerializeField] private float dragToStick = 0.06f;

        private Vector2 frameDelta;
        private bool dragging;

        protected override string controlPathInternal
        {
            get => controlPathValue;
            set => controlPathValue = value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            frameDelta = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            frameDelta += eventData.delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            frameDelta = Vector2.zero;
            SendValueToControl(Vector2.zero);
        }

        private void Update()
        {
            if (!dragging) return;
            Vector2 stick = Vector2.ClampMagnitude(frameDelta * dragToStick, 1f);
            SendValueToControl(stick);
            frameDelta = Vector2.zero;   // no movement next frame = stick back to centre
        }

        protected override void OnDisable()
        {
            dragging = false;
            base.OnDisable();
        }
    }
}
