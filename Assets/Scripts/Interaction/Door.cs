using TheLostDungeon.Audio;
using TheLostDungeon.Items;
using UnityEngine;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// A sliding sci-fi door. It can be opened in two ways:
    ///   - directly, by the player pressing Interact (IInteractable), optionally needing a keycard
    ///   - remotely, by a WallSwitch or anything else (IActivatable)
    /// Untick "Player Can Open" for doors that only a switch should control.
    /// </summary>
    public class Door : MonoBehaviour, IInteractable, IActivatable
    {
        [SerializeField] private Transform panel;
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.7f, 0f);
        [SerializeField, Min(0.1f)] private float speed = 3f;
        [SerializeField] private bool playerCanOpen = true;
        [SerializeField] private bool startsOpen;

        [Header("Lock")]
        [SerializeField] private ItemDefinition requiredKey;
        [SerializeField] private bool consumeKey;

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;

        private Vector3 closedPosition;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public bool IsLocked => requiredKey != null;

        private void Awake()
        {
            if (panel == null) panel = transform;
            closedPosition = panel.localPosition;
            isOpen = startsOpen;
            if (startsOpen) panel.localPosition = closedPosition + openOffset;
        }

        private void Update()
        {
            Vector3 target = isOpen ? closedPosition + openOffset : closedPosition;
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, speed * Time.deltaTime);
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (!playerCanOpen || isOpen) return null;
            if (requiredKey != null && (interactor.Inventory == null || !interactor.Inventory.Has(requiredKey)))
                return $"Locked - needs {requiredKey.DisplayName}";
            return "Open door";
        }

        public bool Interact(PlayerInteractor interactor)
        {
            if (!playerCanOpen || isOpen) return false;

            if (requiredKey != null)
            {
                PlayerInventory inventory = interactor.Inventory;
                if (inventory == null || !inventory.Has(requiredKey))
                {
                    if (AudioManager.HasInstance) AudioManager.Instance.PlayUILocked();
                    return false;
                }
                if (consumeKey) inventory.Remove(requiredKey);
            }

            Open();
            return true;
        }

        public void SetActivated(bool activated)
        {
            if (activated) Open();
            else Close();
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            if (openSound != null && AudioManager.HasInstance) AudioManager.Instance.PlaySfx(openSound);
        }

        public void Close() => isOpen = false;
    }
}
