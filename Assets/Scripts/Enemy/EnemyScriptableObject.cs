using UnityEngine;

[CreateAssetMenu(fileName = "EnemyScriptableObject", menuName = "ScriptableObjects/Enemy")]
public class EnemyScriptableObject : ScriptableObject
{
    //Base stat for enemies
    [SerializeField]
    float moveSpeed;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }

    [SerializeField]
    float maxHealth;
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }

    [SerializeField]
    float damage;
    public float Damage { get => damage; set => damage = value; }

    [Header("Rewards")]
    [SerializeField]
    int experienceReward = 5;
    public int ExperienceReward { get => experienceReward; set => experienceReward = value; }

    [SerializeField]
    int coinReward = 2;
    public int CoinReward { get => coinReward; set => coinReward = value; }

    [Header("FSM Settings")]
    [SerializeField]
    float detectionRadius = 6f;
    public float DetectionRadius { get => detectionRadius; set => detectionRadius = value; }

    [SerializeField]
    float attackRange = 0.7f;
    public float AttackRange { get => attackRange; set => attackRange = value; }
}
