using UnityEngine;

public interface IPoolable
{
    void OnGetFromPool();
    void OnReturnToPool();
}