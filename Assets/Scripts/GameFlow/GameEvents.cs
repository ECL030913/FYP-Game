using System;

/// <summary>
/// The event bridge between the existing combat systems and Module 1 game logic.
/// </summary>
public static class GameEvents
{
    public static event Action WaveCleared;
    public static event Action GlobalAggro;

    public static bool IsGlobalAggroActive { get; private set; }

    public static void RaiseWaveCleared()
    {
        WaveCleared?.Invoke();
    }

    public static void RaiseGlobalAggro()
    {
        IsGlobalAggroActive = true;
        GlobalAggro?.Invoke();
    }

    public static void ResetGlobalAggro()
    {
        IsGlobalAggroActive = false;
    }
}
