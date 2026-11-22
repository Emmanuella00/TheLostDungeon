using TheLostDungeon.Player;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>"Reach the exit". Needs a trigger collider on the same object; completes when the player enters it.</summary>
    [RequireComponent(typeof(Collider))]
    public class ReachZoneObjective : Objective
    {
        private bool reached;

        public override int Current => reached ? 1 : 0;
        public override int Target => 1;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive || reached || other.GetComponentInParent<PlayerController>() == null) return;
            reached = true;
            NotifyProgress();
        }
    }
}
