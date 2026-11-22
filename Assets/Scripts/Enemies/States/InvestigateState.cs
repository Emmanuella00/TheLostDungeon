using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Heard a shot, got hit from behind, or lost sight of the player: run to the spot, look
    /// around for a few seconds, then go back to patrolling. Seeing the player starts a Chase.
    /// </summary>
    public class InvestigateState : EnemyState
    {
        private Vector3 spot;
        private float giveUpTime;
        private bool arrived;

        public InvestigateState(EnemyBrain brain) : base(brain) { }

        public override string Name => "Investigate";

        public void InvestigateAt(Vector3 position)
        {
            spot = position;
            if (brain.Machine.Current == this) Enter();   // new noise while already investigating
            else brain.Machine.ChangeState(this);
        }

        public override void Enter()
        {
            arrived = false;
            giveUpTime = Time.time + Profile.investigateSeconds + 8f;   // also a timeout if the spot is unreachable
            brain.MoveTo(spot, Profile.runSpeed);
        }

        public override void Tick(float deltaTime)
        {
            if (brain.CanSeeTarget())
            {
                brain.PlayAlert();
                brain.Machine.ChangeState(brain.Chase);
                return;
            }

            if (!arrived && brain.ReachedDestination)
            {
                arrived = true;
                brain.StopMoving();
                giveUpTime = Time.time + Profile.investigateSeconds;
            }

            if (arrived)
            {
                // Look around.
                brain.transform.Rotate(0f, 90f * deltaTime, 0f);
            }

            if (Time.time >= giveUpTime) brain.Machine.ChangeState(brain.Patrol);
        }
    }
}
