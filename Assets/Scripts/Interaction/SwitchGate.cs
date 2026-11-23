using System.Collections.Generic;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// An "AND gate" for switches: it opens its targets only when ALL the listed switches are on.
    ///
    /// It listens to GameEvents.SwitchActivated (Observer pattern), so the switches don't need to
    /// know about it. Leave the switches' own Targets lists EMPTY and link the door here instead.
    ///
    /// Example (Level 3): Required Switch Ids = Switch_A, Switch_B, Switch_C; Targets = the door.
    /// </summary>
    public class SwitchGate : MonoBehaviour
    {
        [Tooltip("Every one of these switch ids must be on. Must match each WallSwitch's Switch Id exactly.")]
        [SerializeField] private string[] requiredSwitchIds = { "Switch_A", "Switch_B", "Switch_C" };

        [Tooltip("Any components implementing IActivatable, e.g. a Door.")]
        [SerializeField] private MonoBehaviour[] targets = new MonoBehaviour[0];

        [Tooltip("If ticked, the door stays open once unlocked, even if a switch is turned off again.")]
        [SerializeField] private bool latch = true;

        private readonly HashSet<string> onSwitches = new HashSet<string>();
        private bool isOpen;

        public int OnCount => onSwitches.Count;
        public int RequiredCount => requiredSwitchIds.Length;

        private void OnEnable() => GameEvents.SwitchActivated += HandleSwitch;
        private void OnDisable() => GameEvents.SwitchActivated -= HandleSwitch;

        private void OnValidate()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && !(targets[i] is IActivatable)) targets[i] = null;
            }
        }

        private void HandleSwitch(string switchId, bool isOn)
        {
            if (System.Array.IndexOf(requiredSwitchIds, switchId) < 0) return;

            if (isOn) onSwitches.Add(switchId);
            else onSwitches.Remove(switchId);

            bool allOn = onSwitches.Count >= requiredSwitchIds.Length;
            if (allOn == isOpen) return;
            if (!allOn && latch) return;

            isOpen = allOn;
            foreach (MonoBehaviour target in targets)
            {
                if (target is IActivatable activatable) activatable.SetActivated(isOpen);
            }
        }
    }
}