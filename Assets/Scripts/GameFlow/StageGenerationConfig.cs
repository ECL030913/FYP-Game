using UnityEngine;

/// <summary>
/// Designer-editable values for the procedural node generator. The values are
/// stored in a ScriptableObject rather than hard-coded into StageManager.
/// </summary>
[CreateAssetMenu(fileName = "StageGenerationConfig", menuName = "ScriptableObjects/Stage Generation Config")]
public class StageGenerationConfig : ScriptableObject
{
    [Header("Portal Count")]
    [Range(1, 2)] public int portalChoicesPerWave = 2;

    [Header("Node Weights")]
    [Min(0f)] public float combatWeight = 55f;
    [Min(0f)] public float shopWeight = 30f;
    [Min(0f)] public float eliteWeight = 15f;

    [Header("Generation Constraints")]
    [Min(2)] public int firstEliteStageIndex = 2;
    [Min(3)] public int minimumNonEliteStagesBetweenElites = 3;
}
