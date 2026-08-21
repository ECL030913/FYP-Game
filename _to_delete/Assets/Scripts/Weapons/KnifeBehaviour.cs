using UnityEngine;

public class KnifeBehavior : ProjectileWeaponController
{

    
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        transform.position += direction * currentSpeed * Time.deltaTime;// Set the movement of Knife
    }
}
