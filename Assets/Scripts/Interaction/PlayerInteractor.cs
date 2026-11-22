using TheLostDungeon.Combat;
using TheLostDungeon.Events;
using TheLostDungeon.Items;
using TheLostDungeon.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// Looks at whatever is under the crosshair. If it's an IInteractable within reach, it shows
    /// its prompt (through GameEvents, so the HUD stays decoupled) and uses it when Interact is
    /// pressed (E / gamepad Y / the on-screen button on mobile).
    /// Interactables receive this component, so they can check the inventory or health.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform viewOrigin;
        [SerializeField, Min(0.5f)] private float reach = 2.5f;
        [SerializeField] private LayerMask mask = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[8];
        private InputAction interactAction;
        private IInteractable current;
        private string currentPrompt;

        public PlayerInventory Inventory { get; private set; }
        public Health Health { get; private set; }
        public IInteractable Current => current;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            Inventory = GetComponent<PlayerInventory>();
            Health = GetComponent<Health>();
            interactAction = controller.Controls.FindActionMap("Player", true).FindAction("Interact", true);
        }

        private void OnDisable() => SetPrompt(null);

        private void Update()
        {
            if (!controller.ControlEnabled || viewOrigin == null)
            {
                current = null;
                SetPrompt(null);
                return;
            }

            current = FindInteractable();
            SetPrompt(current?.GetPrompt(this));

            if (current != null && interactAction.WasPressedThisFrame())
            {
                current.Interact(this);
                SetPrompt(current.GetPrompt(this));   // e.g. "Open door" disappears once it's open
            }
        }

        private IInteractable FindInteractable()
        {
            int count = Physics.RaycastNonAlloc(viewOrigin.position, viewOrigin.forward, hits, reach, mask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            IInteractable found = null;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;   // ignore the player
                if (hit.distance >= nearest) continue;

                nearest = hit.distance;
                found = hit.collider.GetComponentInParent<IInteractable>();   // walls return null and block the ray
            }
            return found;
        }

        private void SetPrompt(string prompt)
        {
            if (prompt == currentPrompt) return;
            currentPrompt = prompt;
            GameEvents.RaiseInteractionPromptChanged(prompt);
        }
    }
}
