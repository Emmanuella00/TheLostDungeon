using TheLostDungeon.Audio;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// A wall switch. When used it:
    ///   1. switches every linked IActivatable (doors, lights...): it doesn't know what they are
    ///   2. raises GameEvents.SwitchActivated, so objectives ("activate 3 switches") and audio
    ///      can react without the switch referencing them
    /// The indicator light turns from red to green.
    /// </summary>
    public class WallSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private string switchId = "switch_01";

        [Tooltip("Any components implementing IActivatable, e.g. a Door.")]
        [SerializeField] private MonoBehaviour[] targets = new MonoBehaviour[0];

        [Tooltip("One-shot switches stay on. Untick to toggle on and off.")]
        [SerializeField] private bool oneShot = true;

        [Header("Feedback")]
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color offColour = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private Color onColour = new Color(0.2f, 1f, 0.35f);
        [SerializeField] private AudioClip clickSound;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock block;
        private bool isOn;

        public string SwitchId => switchId;
        public bool IsOn => isOn;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            UpdateIndicator();
        }

        private void OnValidate()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && !(targets[i] is IActivatable)) targets[i] = null;
            }
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (oneShot && isOn) return null;
            return isOn ? "Turn off switch" : "Activate switch";
        }

        public bool Interact(PlayerInteractor interactor)
        {
            if (oneShot && isOn) return false;

            isOn = !isOn;
            foreach (MonoBehaviour target in targets)
            {
                if (target is IActivatable activatable) activatable.SetActivated(isOn);
            }

            UpdateIndicator();
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySfx(clickSound);
            GameEvents.RaiseSwitchActivated(switchId, isOn);
            return true;
        }

        private void UpdateIndicator()
        {
            if (indicator == null) return;
            Color c = isOn ? onColour : offColour;
            indicator.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(EmissionId, c * 2f);
            indicator.SetPropertyBlock(block);
        }
    }
}
