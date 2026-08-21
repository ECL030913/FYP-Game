using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new();

    // Parallel registry for objects that are built procedurally at runtime
    // (no prefab asset to key off), e.g. RuntimeWeaponProjectile / WeaponVisualEffect.
    readonly Dictionary<string, ObjectPool<GameObject>> keyedPools = new();

    [Header("Pool Settings")]
    public int defaultCapacity = 30;
    public int maxSize = 300;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        GameObject obj = pools[prefab].Get();

        obj.transform.SetPositionAndRotation(position, rotation);

        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnGetFromPool();

        return obj;
    }

    /// <summary>
    /// Gets (or lazily creates) a pooled instance for objects that have no
    /// source prefab, such as RuntimeWeaponProjectile / WeaponVisualEffect,
    /// which are built with `new GameObject(...)` + AddComponent instead of
    /// Instantiate(prefab). `createFunc` is only invoked when the pool needs
    /// to grow; every other Get() reuses a previously-released instance.
    /// </summary>
    public GameObject GetPooledObject(string key, System.Func<GameObject> createFunc, Vector3 position, Quaternion rotation)
    {
        if (!keyedPools.ContainsKey(key))
        {
            CreateKeyedPool(key, createFunc);
        }

        GameObject obj = keyedPools[key].Get();

        obj.transform.SetPositionAndRotation(position, rotation);

        IPoolable poolable = obj.GetComponent<IPoolable>();
        poolable?.OnGetFromPool();

        return obj;
    }

    public void ReleaseObject(GameObject obj)
    {
        PoolIdentity identity = obj.GetComponent<PoolIdentity>();

        if (identity == null)
        {
            Destroy(obj);
            return;
        }

        if (!string.IsNullOrEmpty(identity.poolKey) && keyedPools.ContainsKey(identity.poolKey))
        {
            keyedPools[identity.poolKey].Release(obj);
            return;
        }

        if (identity.prefab != null && pools.ContainsKey(identity.prefab))
        {
            pools[identity.prefab].Release(obj);
            return;
        }

        Destroy(obj);
    }

    void CreatePool(GameObject prefab)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);

                PoolIdentity identity = obj.GetComponent<PoolIdentity>();
                if (identity == null)
                {
                    identity = obj.AddComponent<PoolIdentity>();
                }

                identity.prefab = prefab;

                return obj;
            },
            actionOnGet: obj =>
            {
                obj.SetActive(true);
            },
            actionOnRelease: obj =>
            {
                IPoolable poolable = obj.GetComponent<IPoolable>();
                poolable?.OnReturnToPool();

                obj.SetActive(false);
            },
            actionOnDestroy: obj =>
            {
                Destroy(obj);
            },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        pools.Add(prefab, pool);
    }

    void CreateKeyedPool(string key, System.Func<GameObject> createFunc)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = createFunc();
                obj.SetActive(false);

                PoolIdentity identity = obj.GetComponent<PoolIdentity>();
                if (identity == null)
                {
                    identity = obj.AddComponent<PoolIdentity>();
                }

                identity.poolKey = key;

                return obj;
            },
            actionOnGet: obj =>
            {
                obj.SetActive(true);
            },
            actionOnRelease: obj =>
            {
                IPoolable poolable = obj.GetComponent<IPoolable>();
                poolable?.OnReturnToPool();

                obj.SetActive(false);
            },
            actionOnDestroy: obj =>
            {
                Destroy(obj);
            },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        keyedPools.Add(key, pool);
    }
}
