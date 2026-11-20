using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.Algorithms
{
    /// <summary>Anything the aim assist may lock onto (enemies, training dummies).</summary>
    public interface ITargetable
    {
        Vector3 AimPoint { get; }
        bool IsTargetable { get; }
    }

    /// <summary>
    /// Live list of targets. Enemies add themselves in OnEnable and remove themselves in
    /// OnDisable, so nothing has to call FindObjectsOfType every frame (which is slow).
    /// </summary>
    public static class TargetRegistry
    {
        private static readonly List<ITargetable> targets = new List<ITargetable>();
        private static readonly List<Vector3> positionBuffer = new List<Vector3>();
        private static readonly List<ITargetable> validBuffer = new List<ITargetable>();

        public static int Count => targets.Count;

        public static void Register(ITargetable target)
        {
            if (target != null && !targets.Contains(target)) targets.Add(target);
        }

        public static void Unregister(ITargetable target) => targets.Remove(target);

        /// <summary>Nearest targetable thing in the cone, or null.</summary>
        public static ITargetable FindNearest(Vector3 origin, Vector3 forward, float maxDistance, float maxAngleDegrees)
        {
            positionBuffer.Clear();
            validBuffer.Clear();

            foreach (ITargetable t in targets)
            {
                if (t == null || !t.IsTargetable) continue;
                validBuffer.Add(t);
                positionBuffer.Add(t.AimPoint);
            }

            int index = TargetFinder.FindNearestInCone(origin, forward, positionBuffer, maxDistance, maxAngleDegrees);
            return index >= 0 ? validBuffer[index] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => targets.Clear();
    }
}
