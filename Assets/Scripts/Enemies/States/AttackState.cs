using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Stands still, turns to face the player and runs the attack STRATEGY on a cooldown.
    /// It doesn't know whether that's a claw, a bolt or a shield bash. Goes back to Chase when
    /// the player is out of range or out of sight.
    /// </summary>
    public class AttackState : EnemyState
    {
        private float nextAttackTime;

        public AttackState(EnemyBrain brain) : base(brain) { }

        public override string Name => "Attack";

        public override void Enter()
        {
            brain.StopMoving();
            // Small random delay so a group of enemies doesn't attack in perfect sync.
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + Random.Range(0.1f, 0.4f));
        }

        public override void Tick(float deltaTime)
        {
            if (brain.Target == null)
            {
                brain.Machine.ChangeState(brain.Patrol);
                return;
            }

            brain.FaceTarget(deltaTime);

            RangeDecision decision = EnemyMath.DecideRange(brain.DistanceToTarget, Profile.preferredMinDistance, Profile.attackRange * 1.15f);
            if (decision != RangeDecision.Hold || !brain.CanSeeTarget())
            {
                brain.Machine.ChangeState(brain.Chase);
                return;
            }

            if (Time.time >= nextAttackTime && brain.AttackStrategy != null)
            {
                nextAttackTime = Time.time + Profile.attackCooldown;
                brain.TriggerAttackAnimation();
                brain.AttackStrategy.Execute(brain, brain.Target, brain.TargetAimPoint ?? brain.Target.position);
            }
        }
    }
}
