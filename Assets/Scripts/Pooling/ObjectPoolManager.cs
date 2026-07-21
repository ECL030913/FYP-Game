using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new();

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

    public void ReleaseObject(GameObject obj)
    {
        PoolIdentity identity = obj.GetComponent<PoolIdentity>();

        if (identity == null || identity.prefab == null)
        {
            Destroy(obj);
            return;
        }

        if (!pools.ContainsKey(identity.prefab))
        {
            Destroy(obj);
            return;
        }

        pools[identity.prefab].Release(obj);
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
}
