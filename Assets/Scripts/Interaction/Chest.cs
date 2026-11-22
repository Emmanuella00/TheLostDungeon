using System.Collections;
using TheLostDungeon.Audio;
using TheLostDungeon.Items;
using UnityEngine;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// A loot chest. Opening it swings the lid and spawns pickups chosen from a LootTable
    /// (weighted random selection). The pickups pop out in front of the chest.
    /// </summary>
    public class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField, Min(1)] private int rolls = 2;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spreadRadius = 0.5f;

        [Header("Lid")]
        [SerializeField] private Transform lid;
        [Tooltip("Lid rotation added when open. If it opens the wrong way, try (110,0,0) or (0,0,110).")]
        [SerializeField] private Vector3 lidOpenEuler = new Vector3(-110f, 0f, 0f);
        [SerializeField, Min(0.05f)] private float openSeconds = 0.5f;
        [SerializeField] private AudioClip openSound;

        private bool opened;

        public bool Opened => opened;

        public string GetPrompt(PlayerInteractor interactor) => opened ? null : "Open chest";

        public bool Interact(PlayerInteractor interactor)
        {
            if (opened) return false;
            opened = true;

            if (lid != null) StartCoroutine(SwingLid());
            if (openSound != null && AudioManager.HasInstance) AudioManager.Instance.PlaySfx(openSound);
            SpawnLoot();
            return true;
        }

        private void SpawnLoot()
        {
            if (lootTable == null) return;

            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;
            for (int i = 0; i < rolls; i++)
            {
                GameObject prefab = lootTable.Roll(Random.value);
                if (prefab == null) continue;

                float angle = (360f / rolls) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spreadRadius;
                Instantiate(prefab, origin + offset, Quaternion.identity);
            }
        }

        private IEnumerator SwingLid()
        {
            Quaternion closed = lid.localRotation;
            Quaternion open = closed * Quaternion.Euler(lidOpenEuler);

            for (float t = 0f; t < openSeconds; t += Time.deltaTime)
            {
                lid.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t / openSeconds));
                yield return null;
            }
            lid.localRotation = open;
        }
    }
}
