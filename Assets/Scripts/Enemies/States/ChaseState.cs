using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Gets into attack position using EnemyMath.DecideRange: close in (Warrior, Guardian) or keep
    /// a safe distance (Archer backs away if you rush it). Hands over to Attack when in range,
    /// to Investigate if it loses sight of you, and back to its post if leashed (Guardian).
    /// </summary>
    public class ChaseState : EnemyState
    {
        private const float LoseSightSeconds = 2.5f;
        private const float RepathSeconds = 0.25f;

        private Vector3 lastKnownPosition;
        private float lastSeenTime;
        private float nextRepath;

        public ChaseState(EnemyBrain brain) : base(brain) { }

        public override string Name => "Chase";

        public override void Enter()
        {
            lastSeenTime = Time.time;
            nextRepath = 0f;
            if (brain.Target != null) lastKnownPosition = brain.Target.position;
        }

        public override void Tick(float deltaTime)
        {
            if (brain.Target == null)
            {
                brain.Machine.ChangeState(brain.Patrol);
                return;
            }

            bool sees = brain.CanSeeTarget();
            if (sees)
            {
                lastSeenTime = Time.time;
                lastKnownPosition = brain.Target.position;
            }
            else if (Time.time - lastSeenTime > LoseSightSeconds)
            {
                brain.Investigate.InvestigateAt(lastKnownPosition);
                return;
            }

            // Leashed enemies (Guardians) don't follow you across the map.
            if (Profile.leashRadius > 0f && Vector3.Distance(brain.transform.position, brain.Post) > Profile.leashRadius)
            {
                brain.Investigate.InvestigateAt(brain.Post);
                return;
            }

            float distance = brain.DistanceToTarget;
            RangeDecision decision = EnemyMath.DecideRange(distance, Profile.preferredMinDistance, Profile.attackRange);

            if (decision == RangeDecision.Hold && sees)
            {
                brain.Machine.ChangeState(brain.Attack);
                return;
            }

            if (Time.time < nextRepath) return;
            nextRepath = Time.time + RepathSeconds;

            if (decision == RangeDecision.Retreat)
            {
                Vector3 away = (brain.transform.position - brain.Target.position).normalized;
                Vector3 goal = brain.transform.position + away * (Profile.preferredMinDistance - distance + 2f);
                brain.TryFindRandomPointNear(goal, 1.5f, out Vector3 point);
                brain.MoveTo(point, Profile.runSpeed);
            }
            else
            {
                brain.MoveTo(sees ? brain.Target.position : lastKnownPosition, Profile.runSpeed);
            }
        }
    }
}
