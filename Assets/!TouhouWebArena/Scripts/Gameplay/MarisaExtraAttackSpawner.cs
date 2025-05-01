using UnityEngine;
using Unity.Netcode;

/// <summary>
/// [Server Only] Handles spawning Marisa's extra attack projectiles (Master Sparks).
/// Listens for events triggered by <see cref="FairyController"/> instances marked as extra attack triggers.
/// Uses the <see cref="NetworkObjectPool"/> to spawn the projectiles.
/// </summary>
public class MarisaExtraAttackSpawner : NetworkBehaviour
{
    [Header("Marisa Spawn Settings")]
    [Tooltip("Assign the Transform defining the bottom-center area for attacks targeting Player 1.")]
    [SerializeField] private Transform player1TargetExtraAttackSpawnArea;

    [Tooltip("Assign the Transform defining the bottom-center area for attacks targeting Player 2.")]
    [SerializeField] private Transform player2TargetExtraAttackSpawnArea;

    [Tooltip("The horizontal width around the spawn area's center within which the laser can appear.")]
    [SerializeField] private float extraAttackSpawnWidth = 5f;

    /// <summary>
    /// Gets the Transform defining the target spawn area for Player 1's extra attack.
    /// </summary>
    /// <returns>The target area Transform for Player 1.</returns>
    public Transform GetPlayer1TargetArea() => player1TargetExtraAttackSpawnArea;

    /// <summary>
    /// Gets the Transform defining the target spawn area for Player 2's extra attack.
    /// </summary>
    /// <returns>The target area Transform for Player 2.</returns>
    public Transform GetPlayer2TargetArea() => player2TargetExtraAttackSpawnArea;

    /// <summary>
    /// Gets the configured horizontal spawn width for the extra attack.
    /// </summary>
    /// <returns>The horizontal spawn width.</returns>
    public float GetSpawnWidth() => extraAttackSpawnWidth;

    void Start()
    {
        // Basic validation
        if (player1TargetExtraAttackSpawnArea == null)
        {
            Debug.LogError("Player 1 Target Extra Attack Spawn Area not assigned in MarisaExtraAttackSpawner!", this);
        }
        if (player2TargetExtraAttackSpawnArea == null)
        {
            Debug.LogError("Player 2 Target Extra Attack Spawn Area not assigned in MarisaExtraAttackSpawner!", this);
        }
    }

    // Draw visual aids in the editor to see the spawn areas
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta; // Use a distinct color for Marisa's spawner
        float gizmoHeight = 0.2f; // Small height for the gizmo line/box

        // Draw Player 1 Target Area
        if (player1TargetExtraAttackSpawnArea != null)
        {
            Vector3 center1 = player1TargetExtraAttackSpawnArea.position;
            Vector3 size1 = new Vector3(extraAttackSpawnWidth, gizmoHeight, 0f);
            Gizmos.DrawWireCube(center1, size1);
        }

        // Draw Player 2 Target Area
        if (player2TargetExtraAttackSpawnArea != null)
        {
            Vector3 center2 = player2TargetExtraAttackSpawnArea.position;
            Vector3 size2 = new Vector3(extraAttackSpawnWidth, gizmoHeight, 0f);
            Gizmos.DrawWireCube(center2, size2);
        }
    }
} 