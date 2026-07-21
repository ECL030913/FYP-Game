public sealed class IdleState : EnemyState
{
    public IdleState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        context.SetMovementEnabled(false);
    }

    public override void UpdateState()
    {
        if (context.IsPlayerWithinDetectionRange())
        {
            // The first enemy that sees the player wakes the entire active wave.
            GameEvents.RaiseGlobalAggro();
        }
    }
}
