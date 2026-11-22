using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Calm behaviour: walks between patrol waypoints, or wanders near its post if it has none
    /// (so a dragged-in enemy works with zero setup). Seeing the player starts a Chase.
    /// </summary>
    public class PatrolState : EnemyState
    {
        private int waypointIndex;
        private float waitUntil;

        public PatrolState(EnemyBrain brain) : base(brain) { }

        public override string Name => "Patrol";

        public override void Enter()
        {
            waitUntil = Time.time + Random.Range(0.5f, 1.5f);
            brain.StopMoving();
        }

        public override void Tick(float deltaTime)
        {
            if (brain.CanSeeTarget())
            {
                brain.PlayAlert();
                brain.Machine.ChangeState(brain.Chase);
                return;
            }

            if (Time.time < waitUntil || !brain.ReachedDestination) return;

            // Arrived (or just started): pause, then head to the next point.
            if (brain.Agent.hasPath)
            {
                brain.StopMoving();
                waitUntil = Time.time + Random.Range(1f, 2.5f);
                return;
            }

            brain.MoveTo(NextPoint(), Profile.walkSpeed);
        }

        private Vector3 NextPoint()
        {
            var points = brain.PatrolWaypoints;
            if (points.Count > 0)
            {
                waypointIndex = (waypointIndex + 1) % points.Count;
                if (points[waypointIndex] != null) return points[waypointIndex].position;
            }

            brain.TryFindRandomPointNear(brain.Post, Profile.wanderRadius, out Vector3 point);
            return point;
        }
    }
}
