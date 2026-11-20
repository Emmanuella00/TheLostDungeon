using UnityEngine;

namespace TheLostDungeon.Pooling
{
    /// <summary>Restarts a particle effect every time it's taken from the pool.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticleEffect : MonoBehaviour, IPoolable
    {
        private ParticleSystem particles;

        private void Awake() => particles = GetComponent<ParticleSystem>();

        public void OnSpawn()
        {
            if (particles == null) particles = GetComponent<ParticleSystem>();
            particles.Clear(true);
            particles.Play(true);
        }

        public void OnDespawn()
        {
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
