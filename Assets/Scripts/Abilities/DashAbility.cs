using UnityEngine;

namespace TheLostDungeon.Abilities
{
    /// <summary>
    /// A quick burst of movement in the direction you're moving (or forward if standing still),
    /// with brief invulnerability: dodge a Guardian's bash, an Archer's bolt or a barrel blast.
    /// </summary>
    public class DashAbility : Ability
    {
        [SerializeField] private float distance = 6f;
        [SerializeField, Min(0.05f)] private float duration = 0.18f;
        [SerializeField, Min(0f)] private float invulnerableSeconds = 0.3f;

        private CharacterController motor;
        private Vector3 direction;
        private float remaining;

        public override bool IsActive => remaining > 0f;

        protected override bool CanActivate(in AbilityContext context) => context.Player != null && context.Player.Motor != null;

        protected override void OnActivate(in AbilityContext context)
        {
            motor = context.Player.Motor;
            Transform body = context.Player.transform;
            Vector2 input = context.Player.MoveInput;

            direction = input.sqrMagnitude > 0.01f
                ? (body.right * input.x + body.forward * input.y).normalized
                : body.forward;

            remaining = duration;
            context.Health?.SetInvulnerable(invulnerableSeconds);
        }

        protected override void OnTick(float deltaTime)
        {
            if (remaining <= 0f || motor == null) return;

            float step = Mathf.Min(deltaTime, remaining);
            motor.Move(direction * (distance / duration * step));
            remaining -= step;
            if (remaining <= 0f) NotifyStatusChanged();
        }
    }
}
