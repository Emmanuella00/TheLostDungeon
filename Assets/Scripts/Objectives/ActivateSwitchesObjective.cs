using System.Collections.Generic;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>"Activate the 3 power switches". Counts distinct switches turned on.</summary>
    public class ActivateSwitchesObjective : Objective
    {
        [Tooltip("Switch ids that count. Leave empty to count any switch.")]
        [SerializeField] private string[] switchIds = new string[0];
        [SerializeField, Min(1)] private int count = 3;

        private readonly HashSet<string> activated = new HashSet<string>();

        public override int Current => Mathf.Min(activated.Count, count);
        public override int Target => count;

        protected override void OnBegin() => GameEvents.SwitchActivated += HandleSwitch;
        protected override void OnEnd() => GameEvents.SwitchActivated -= HandleSwitch;

        private void HandleSwitch(string switchId, bool isOn)
        {
            if (switchIds.Length > 0 && System.Array.IndexOf(switchIds, switchId) < 0) return;

            bool changed = isOn ? activated.Add(switchId) : activated.Remove(switchId);
            if (changed) NotifyProgress();
        }
    }
}
