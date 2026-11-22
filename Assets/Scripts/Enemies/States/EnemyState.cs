namespace TheLostDungeon.Enemies
{
    /// <summary>Shared base for the enemy states: gives each one access to its brain.</summary>
    public abstract class EnemyState : IEnemyState
    {
        protected readonly EnemyBrain brain;

        protected EnemyState(EnemyBrain brain) => this.brain = brain;

        public abstract string Name { get; }
        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }

        protected EnemyProfile Profile => brain.Profile;
    }
}
