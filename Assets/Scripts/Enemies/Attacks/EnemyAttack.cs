using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// STRATEGY PATTERN for enemy attacks. The Attack state just calls Execute(); whether that's a
    /// claw swipe, a laser bolt or a shield bash depends on which strategy component the prefab
    /// has. A new attack style = a new subclass, and the brain and states stay untouched.
    /// </summary>
    public interface IEnemyAttack
    {
        /// <summary>Starts an attack on the target. Called by AttackState when the cooldown allows.</summary>
        void Execute(EnemyBrain attacker, Transform target, Vector3 targetAimPoint);
    }

    /// <summary>Base class so strategies can be MonoBehaviours with Inspector settings.</summary>
    public abstract class EnemyAttack : MonoBehaviour, IEnemyAttack
    {
        public abstract void Execute(EnemyBrain attacker, Transform target, Vector3 targetAimPoint);
    }
}
