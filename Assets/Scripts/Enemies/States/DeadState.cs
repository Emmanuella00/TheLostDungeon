using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Plays the death animation, then sinks into the floor and is removed (back to its pool if
    /// it was spawned from one). Final state: nothing transitions out of it except a respawn.
    /// </summary>
    public class DeadState : EnemyState
    {
        private const float SinkAfterSeconds = 1.5f;
        private const float RemoveAfterSeconds = 3f;
        private const float SinkSpeed = 0.6f;

        private float startTime;

        public DeadState(EnemyBrain brain) : base(brain) { }

        public override string Name => "Dead";

        public override void Enter()
        {
            startTime = Time.time;
            brain.BeginDeath();
        }

        public override void Tick(float deltaTime)
        {
            float elapsed = Time.time - startTime;
            if (elapsed > SinkAfterSeconds) brain.transform.position += Vector3.down * (SinkSpeed * deltaTime);
            if (elapsed > RemoveAfterSeconds) brain.Remove();
        }
    }
}
