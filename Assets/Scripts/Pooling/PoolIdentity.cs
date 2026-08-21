using UnityEngine;

public class PoolIdentity : MonoBehaviour
{
    // Set when the object came from ObjectPoolManager.GetObject(prefab, ...).
    public GameObject prefab;

    // Set when the object came from ObjectPoolManager.GetPooledObject(key, ...)
    // instead — used by procedurally-built objects that have no source prefab
    // (e.g. RuntimeWeaponProjectile, WeaponVisualEffect).
    public string poolKey;
}
