using UnityEngine;

public class WeaponController : MonoBehaviour
{
    //base script for all weapon controllers
    [Header("Weapon Stats")]
    public WeaponScriptableObject weaponData;
    float currentCooldown;


    protected PlayerMovement pm;

    
    protected virtual void Start()
    {
        pm = FindAnyObjectByType<PlayerMovement>();

        currentCooldown = GetEffectiveCooldown();//At the start set the current cooldown to be the cooldown duration
    }

    // Update is called once per frame
    protected virtual void Update()
    {  
        currentCooldown -= Time.deltaTime;
        
        if (currentCooldown <= 0f) //Once the tiem become 0, attack
        {
            Attack();
        }

    }

    protected virtual void Attack()
    {
        currentCooldown = GetEffectiveCooldown();
    }

    protected float GetEffectiveCooldown()
    {
        float cooldownMultiplier = RunManager.Instance != null
            ? RunManager.Instance.Data.cooldownMultiplier
            : 1f;

        return weaponData.CooldownDuration * cooldownMultiplier;
    }
}
