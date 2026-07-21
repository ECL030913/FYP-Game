public abstract class EnemyState
{
    protected readonly EnemyAI context;

    protected EnemyState(EnemyAI context)
    {
        this.context = context;
    }

    public virtual void EnterState() { }
    public abstract void UpdateState();
    public virtual void ExitState() { }
}
