using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //Movement
    
    [HideInInspector]
    public float lastHorizontalVector;
    [HideInInspector]
    public float lastVerticalVector;
    [HideInInspector]
    public Vector2 moveDir;
    [HideInInspector]
    public Vector2 lastMovedVector;

    //Reference
    Rigidbody2D rb;
    public CharacterScriptableObject characterData;
    float runtimeSpeedBonus;
    bool movementEnabled = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastMovedVector = new Vector2(1, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        InputManagement();
    }

    void FixedUpdate()
    {
        Move();

        
    }

    void InputManagement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDir = new Vector2(moveX, moveY).normalized;

        if(moveDir.x != 0)
        {
            lastHorizontalVector = moveDir.x;
            lastMovedVector = new Vector2(lastHorizontalVector, 0f);
        }
        
        if(moveDir.y != 0)
        {
            lastVerticalVector = moveDir.y;
            lastMovedVector = new Vector2(0f, lastVerticalVector);
        }
        
        if(moveDir.x != 0 && moveDir.y != 0)
        {
            lastMovedVector = new Vector2(lastHorizontalVector, lastVerticalVector);
        }
    }

    void Move()
    {
        if (!movementEnabled)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            return;
        }

        float effectiveSpeed = characterData.MoveSpeed + runtimeSpeedBonus;
        rb.linearVelocity = new Vector2(moveDir.x * effectiveSpeed, moveDir.y * effectiveSpeed);
    }

    public void SetRuntimeSpeedBonus(float value)
    {
        runtimeSpeedBonus = Mathf.Max(0f, value);
    }

    public void SetMovementEnabled(bool value)
    {
        movementEnabled = value;

        if (!movementEnabled && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
