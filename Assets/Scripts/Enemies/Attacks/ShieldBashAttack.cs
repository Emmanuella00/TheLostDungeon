using System.Collections;
using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Guardian attack: a short forward charge with the shield, then a heavy, slow melee hit.
    /// Reuses MeleeAttack's hit logic and only adds the lunge (inheritance + strategy).
    /// </summary>
    public class ShieldBashAttack : MeleeAttack
    {
        [SerializeField] private float lungeDistance = 1.5f;
        [SerializeField, Min(0.05f)] private float lungeSeconds = 0.25f;

        private void Reset()
        {
            damage = 30f;
            reach = 2.6f;
            windUpSeconds = 0.5f;
            knockback = 8f;
        }

        protected override IEnumerator StrikeAfterWindUp(EnemyBrain attacker, Transform target)
        {
            // Lunge forward during the wind-up, then let the base class resolve the hit.
            Vector3 direction = attacker.transform.forward;
            float perSecond = lungeDistance / lungeSeconds;
            for (float t = 0f; t < lungeSeconds && attacker.IsAlive; t += Time.deltaTime)
            {
                attacker.Nudge(direction * (perSecond * Time.deltaTime));
                yield return null;
            }

            yield return base.StrikeAfterWindUp(attacker, target);
        }
    }
}
