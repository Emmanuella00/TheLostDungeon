using System;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// STATE PATTERN: one behaviour (Patrol, Chase, Attack...) per class, each deciding for
    /// itself when to hand over to another state. This replaces a big switch statement full of
    /// flags like "isChasing && !isAttacking && !isDead".
    /// </summary>
    public interface IEnemyState
    {
        string Name { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>
    /// Runs exactly one state at a time and guarantees Exit is called on the old state before
    /// Enter on the new one. Plain C#, so transitions can be unit tested.
    /// </summary>
    public class StateMachine
    {
        public IEnemyState Current { get; private set; }

        /// <summary>(previous, next) after every change. Useful for debugging and tests.</summary>
        public event Action<IEnemyState, IEnemyState> Changed;

        public void ChangeState(IEnemyState next)
        {
            if (next == null || next == Current) return;

            IEnemyState previous = Current;
            previous?.Exit();
            Current = next;
            next.Enter();
            Changed?.Invoke(previous, next);
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
