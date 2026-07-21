using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public CharacterScriptableObject characterData;

    public float BaseMaxHealth { get; private set; }

    [Header("Health")]
    public float currentHealth;
    public float maxHealth;
    public bool IsDead { get; private set; }


    float currentRecovery;
    float currentMoveSpeed;
    float currentMight;
    float currentProjectileSpeed;
    
    //i-Frames
        [Header("I-Frames")]
        public float invincibilityDuration = 0.5f;
        float invincibilityTimer;
        bool isInvincible;

    [Header("Health Regen")]
    public float regenDelay = 3f;
    public float regenAmount = 2f;
    public float regenInterval = 1f;

    float timeSinceLastDamage;
    float regenTimer;
    SpriteRenderer playerSprite;
    readonly List<WeaponController> suspendedWeaponControllers = new List<WeaponController>();

    [Header("UI")]
    public Slider healthBar;

    private void Awake()
    {
        //Assign the variables
        BaseMaxHealth = characterData.MaxHealth;
        maxHealth = BaseMaxHealth;
        currentHealth = maxHealth;

        playerSprite = GetComponent<SpriteRenderer>();

        UpdateHealthBar();

        currentRecovery = characterData.Recovery;
        currentMoveSpeed = characterData.MoveSpeed;
        currentMight = characterData.Might;
        currentProjectileSpeed = characterData.ProjectileSpeed;

    }

    void Update()
    {
        HandleInvincibility();
        HandleHealthRegen();
    }

    void HandleInvincibility()
    {
        if (IsDead)
        {
            if (playerSprite != null)
            {
                playerSprite.enabled = true;
            }

            return;
        }

        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;

            if (playerSprite != null)
            {
                // Alternate the visible sprite during the existing 0.5-second i-frame.
                playerSprite.enabled = Mathf.FloorToInt(invincibilityTimer * 16f) % 2 == 0;
            }
        }
        else
        {
            isInvincible = false;

            if (playerSprite != null)
            {
                playerSprite.enabled = true;
            }
        }
    }

    void HandleHealthRegen()
    {
        if (IsDead || currentHealth >= maxHealth)
        {
            return;
        }

        timeSinceLastDamage += Time.deltaTime;

        if (timeSinceLastDamage < regenDelay)
        {
            return;
        }

        regenTimer += Time.deltaTime;

        if (regenTimer >= regenInterval)
        {
            regenTimer = 0f;
            Heal(regenAmount);
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || isInvincible)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        timeSinceLastDamage = 0f;
        regenTimer = 0f;

        invincibilityTimer = invincibilityDuration;
        isInvincible = true;

        UpdateHealthBar();
        RunManager.Instance?.SavePlayerState(this);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        RunManager.Instance?.SavePlayerState(this);
    }

    public void ApplyRunData(RunData runData)
    {
        if (runData == null)
        {
            return;
        }

        maxHealth = BaseMaxHealth + runData.maxHealthBonus;
        currentHealth = Mathf.Clamp(runData.savedPlayerHealth, 0f, maxHealth);
        UpdateHealthBar();
    }

    /// <summary>
    /// Restores the player for a completely new run after choosing Retry.
    /// StageManager immediately applies the fresh RunData afterwards, but doing
    /// the local reset here also prevents a dead player from receiving input
    /// during that transition.
    /// </summary>
    public void ReviveAt(Vector2 position)
    {
        IsDead = false;
        isInvincible = false;
        invincibilityTimer = 0f;
        timeSinceLastDamage = 0f;
        regenTimer = 0f;
        maxHealth = BaseMaxHealth;
        currentHealth = maxHealth;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = position;
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        movement?.SetMovementEnabled(true);
        SetWeaponControllersEnabled(true);

        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

    void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        invincibilityTimer = 0f;
        isInvincible = false;
        GetComponent<PlayerMovement>()?.SetMovementEnabled(false);
        SetWeaponControllersEnabled(false);

        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        Debug.Log("PLAYER IS DEAD");

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager != null)
        {
            stageManager.HandlePlayerDeath(this);
            return;
        }

        Module1Ui.EnsureForScene().ShowDeathMenu();
    }

    private void SetWeaponControllersEnabled(bool value)
    {
        if (value)
        {
            foreach (WeaponController weaponController in suspendedWeaponControllers)
            {
                if (weaponController != null)
                {
                    weaponController.enabled = true;
                }
            }

            suspendedWeaponControllers.Clear();
            return;
        }

        suspendedWeaponControllers.Clear();
        foreach (WeaponController weaponController in GetComponentsInChildren<WeaponController>(true))
        {
            if (weaponController != null && weaponController.enabled)
            {
                weaponController.enabled = false;
                suspendedWeaponControllers.Add(weaponController);
            }
        }
    }


}
