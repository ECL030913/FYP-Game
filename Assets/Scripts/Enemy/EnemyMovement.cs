using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public EnemyScriptableObject enemyData;
    Transform Player;
    
    
    void Start()
    {
        Player = FindAnyObjectByType<PlayerMovement>().transform;
    }

    
    void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, Player.transform.position, enemyData.MoveSpeed *  Time.deltaTime);
    }
}
