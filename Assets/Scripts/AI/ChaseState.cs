public sealed class ChaseState : EnemyState
{
    public ChaseState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        context.SetMovementEnabled(true);
    }

    public override void UpdateState()
    {
        if (context.IsPlayerWithinAttackRange())
        {
            context.ChangeState(new AttackState(context));
        }
    }

    public override void ExitState()
    {
        context.SetMovementEnabled(false);
    }
}
