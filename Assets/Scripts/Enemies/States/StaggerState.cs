using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Stunned after a melee hit breaks its guard (Guardian). It can't move or attack, and its
    /// shield is lowered so bolts do full damage: the reward for getting in close.
    /// </summary>
    public class StaggerState : EnemyState
    {
        private float endTime;
        private Combat.DefenceProfile savedDefence;
        private Combat.Health health;

        public StaggerState(EnemyBrain brain) : base(brain) { }

        public override string Name => "Stagger";

        public override void Enter()
        {
            endTime = Time.time + Profile.staggerSeconds;
            brain.StopMoving();

            health = brain.GetComponent<Combat.Health>();
            savedDefence = health.Defence;
            Combat.DefenceProfile lowered = savedDefence;
            lowered.hasFrontShield = false;
            health.Defence = lowered;
        }

        public override void Tick(float deltaTime)
        {
            if (Time.time >= endTime) brain.Machine.ChangeState(brain.Chase);
        }

        public override void Exit()
        {
            if (health != null) health.Defence = savedDefence;
        }
    }
}
