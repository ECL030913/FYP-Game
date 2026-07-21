public sealed class AttackState : EnemyState
{
    public AttackState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        // Damage remains owned by the existing collision-based combat system.
        context.SetMovementEnabled(false);
    }

    public override void UpdateState()
    {
        if (!context.IsPlayerWithinAttackRange())
        {
            context.ChangeState(new ChaseState(context));
        }
    }
}
