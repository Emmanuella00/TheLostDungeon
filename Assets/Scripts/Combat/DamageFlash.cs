using UnityEngine;

namespace TheLostDungeon.Combat
{
    /// <summary>
    /// Flashes a model's colour when its Health is hit: red for damage, blue when a shield
    /// blocked the hit. Uses a MaterialPropertyBlock so materials aren't duplicated per object.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Color damageColour = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color blockedColour = new Color(0.3f, 0.7f, 1f);
        [SerializeField, Min(0.01f)] private float flashSeconds = 0.12f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Health health;
        private Renderer[] renderers;
        private Color[] originalColours;
        private MaterialPropertyBlock block;
        private float flashUntil;

        private void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            originalColours = new Color[renderers.Length];
            block = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                Material m = renderers[i].sharedMaterial;
                originalColours[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
            }
        }

        private void OnEnable() => health.Damaged += HandleDamaged;
        private void OnDisable() => health.Damaged -= HandleDamaged;

        private void HandleDamaged(DamageInfo info, DamageResult result)
        {
            SetColour(result.Blocked ? blockedColour : damageColour, useOriginal: false);
            flashUntil = Time.time + flashSeconds;
        }

        private void Update()
        {
            if (flashUntil > 0f && Time.time >= flashUntil)
            {
                flashUntil = 0f;
                SetColour(Color.white, useOriginal: true);
            }
        }

        private void SetColour(Color colour, bool useOriginal)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, useOriginal ? originalColours[i] : colour);
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
