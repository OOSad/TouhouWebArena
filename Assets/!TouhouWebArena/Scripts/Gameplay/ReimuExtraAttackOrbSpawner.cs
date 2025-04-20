using UnityEngine;
using System.Collections;
using Unity.Netcode;

// Spawner specifically for Reimu's Extra Attack Orbs
// This script now primarily holds the configuration for spawn zones used by PlayerDataManager.
public class ReimuExtraAttackOrbSpawner : NetworkBehaviour
{
    [Header("Spawn Zones")]
    [SerializeField] private Transform spawnZone1; // Assign the same Transform as StageSmallBulletSpawner
    [SerializeField] private Transform spawnZone2; // Assign the same Transform as StageSmallBulletSpawner
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(2f, 1f); // Should match StageSmallBulletSpawner if using same zones

    // Note: The actual spawning is triggered by PlayerDataManager based on kill count.
    // The timer-based spawning logic has been removed.

    private void Start()
    {
        // Basic validation for assigned zones
        if (spawnZone1 == null || spawnZone2 == null)
        {
            Debug.LogError("Spawn zones not assigned in ReimuExtraAttackOrbSpawner.", this);
            enabled = false;
            return;
        }
    }

    // --- Public Getters for Spawn Zone Info ---
    public Transform GetSpawnZone1() => spawnZone1;
    public Transform GetSpawnZone2() => spawnZone2;
    public Vector2 GetSpawnZoneSize() => spawnZoneSize;
    // --- End Public Getters ---

    // Draw visual aids in the editor to see the spawn zones
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // Use a different color to distinguish from bullet spawner
        if (spawnZone1 != null)
        {
            Gizmos.DrawWireCube(spawnZone1.position, new Vector3(spawnZoneSize.x, spawnZoneSize.y, 0f));
        }
        if (spawnZone2 != null)
        {
            Gizmos.DrawWireCube(spawnZone2.position, new Vector3(spawnZoneSize.x, spawnZoneSize.y, 0f));
        }
    }
} 