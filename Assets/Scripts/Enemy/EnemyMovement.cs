using UnityEngine;

public class EnemyMovement : MonoBehaviour, IEnemyMotor
{
    public EnemyScriptableObject enemyData;
    private Transform player;
    private EnemyStats enemyStats;

    public EnemyScriptableObject EnemyData => enemyData;

    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
    }

    private void OnEnable()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                return;
            }
        }

        float moveSpeed = enemyStats != null
            ? enemyStats.CurrentMoveSpeed
            : enemyData != null ? enemyData.MoveSpeed : 0f;
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime);
    }

    public void SetMovementEnabled(bool movementEnabled)
    {
        enabled = movementEnabled;
    }

    private void FindPlayer()
    {
        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
        player = playerMovement != null ? playerMovement.transform : null;
    }
}
