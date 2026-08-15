using UnityEngine;

/// <summary>
/// The Module 1 FSM context. It deliberately knows only a player Transform and
/// the movement executor, not player input or combat-damage implementations.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    private EnemyState currentState;
    private IEnemyMotor movementExecutor;
    private EnemyScriptableObject enemyData;
    private Transform player;

    private const float FallbackDetectionRadius = 6f;
    private const float FallbackAttackRange = 0.7f;

    private void Awake()
    {
        movementExecutor = GetComponent<IEnemyMotor>();
        enemyData = movementExecutor?.EnemyData ?? GetComponent<EnemyStats>()?.enemyData;

        if (movementExecutor == null)
        {
            Debug.LogError($"{name}: EnemyAI requires a component implementing IEnemyMotor.", this);
        }
    }

    private void OnEnable()
    {
        GameEvents.GlobalAggro += HandleGlobalAggro;
        FindPlayer();
        ChangeState(GameEvents.IsGlobalAggroActive
            ? new ChaseState(this)
            : new IdleState(this));
    }

    private void OnDisable()
    {
        GameEvents.GlobalAggro -= HandleGlobalAggro;
        SetMovementEnabled(false);
    }

    private void Update()
    {
        currentState?.UpdateState();
    }

    public void ChangeState(EnemyState nextState)
    {
        currentState?.ExitState();
        currentState = nextState;
        currentState?.EnterState();
    }

    public void SetMovementEnabled(bool value)
    {
        if (movementExecutor != null)
        {
            movementExecutor.SetMovementEnabled(value);
        }
    }

    public bool IsPlayerWithinDetectionRange()
    {
        return IsPlayerWithinRange(GetDetectionRadius());
    }

    public bool IsPlayerWithinAttackRange()
    {
        return IsPlayerWithinRange(GetAttackRange());
    }

    private bool IsPlayerWithinRange(float range)
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                return false;
            }
        }

        return (player.position - transform.position).sqrMagnitude <= range * range;
    }

    private void HandleGlobalAggro()
    {
        if (isActiveAndEnabled && !(currentState is ChaseState) && !(currentState is AttackState))
        {
            ChangeState(new ChaseState(this));
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    private float GetDetectionRadius()
    {
        return enemyData != null && enemyData.DetectionRadius > 0f
            ? enemyData.DetectionRadius
            : FallbackDetectionRadius;
    }

    private float GetAttackRange()
    {
        return enemyData != null && enemyData.AttackRange > 0f
            ? enemyData.AttackRange
            : FallbackAttackRange;
    }
}
