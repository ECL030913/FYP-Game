/// <summary>
/// Movement contract consumed by the FSM. EnemyAI no longer depends on the
/// concrete EnemyMovement component and can work with future flying/boss motors.
/// </summary>
public interface IEnemyMotor
{
    EnemyScriptableObject EnemyData { get; }
    void SetMovementEnabled(bool enabled);
}
