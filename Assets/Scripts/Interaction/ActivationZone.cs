using TheLostDungeon.Player;
using UnityEngine;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// An invisible trigger that switches on IActivatable targets when the player walks in:
    /// ambushes (EnemySpawner), doors that slam open, alarms. The same IActivatable interface the
    /// WallSwitch uses, so anything a switch can control, a zone can control too.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ActivationZone : MonoBehaviour
    {
        [Tooltip("Any components implementing IActivatable (EnemySpawner, Door...).")]
        [SerializeField] private MonoBehaviour[] targets = new MonoBehaviour[0];
        [SerializeField] private bool oneShot = true;

        private bool used;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnValidate()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && !(targets[i] is IActivatable)) targets[i] = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((oneShot && used) || other.GetComponentInParent<PlayerController>() == null) return;
            used = true;

            foreach (MonoBehaviour target in targets)
            {
                if (target is IActivatable activatable) activatable.SetActivated(true);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            if (TryGetComponent(out BoxCollider box))
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
}
