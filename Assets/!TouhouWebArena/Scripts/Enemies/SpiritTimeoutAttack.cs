using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // For PlayerRole if needed for bullet config

/// <summary>
/// [Server Only] Handles the specific attack pattern executed when a Spirit times out in its activated state.
/// Spawns configured bullets in a spread pattern.
/// Should be attached to the same GameObject as the SpiritController.
/// Called by <see cref="SpiritController.FixedUpdate"/>.
/// </summary>
public class SpiritTimeoutAttack : MonoBehaviour
{
    [Header("Timeout Attack Config")]
    [Tooltip("Prefab for the bullet spawned during the timeout attack. Must have NetworkObject and StageSmallBulletMoverScript components.")]
    [SerializeField] private GameObject spiritLargeBulletPrefab;

    [Tooltip("Spread angle (degrees) for the side bullets fired during timeout.")]
    [SerializeField] private float bulletSpreadAngle = 15f;

    /// <summary>
    /// [Server Only] Executes the timeout attack, spawning three bullets.
    /// Called by SpiritController when its activated timer expires.
    /// </summary>
    /// <param name="spawnPosition">The world position where the bullets should originate.</param>
    public void ExecuteAttack(Vector3 spawnPosition)
    {
        // Check if we are on the server, as this logic should only run there.
        // Note: SpiritController already checks IsServer before calling this via FixedUpdate.
        // Adding a redundant check here for safety/clarity.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            // This case should ideally not be reached if called correctly from SpiritController.
            Debug.LogWarning("[SpiritTimeoutAttack] ExecuteAttack called on a client. Aborting.", this);
            return; 
        }

        // Spawn 3 bullets: one down, two angled
        
        // 1. Straight down
        SpawnBullet(Vector3.down, spawnPosition);

        // 2. Angled left
        Quaternion leftRotation = Quaternion.Euler(0, 0, bulletSpreadAngle);
        Vector3 leftDirection = leftRotation * Vector3.down;
        SpawnBullet(leftDirection, spawnPosition);

        // 3. Angled right
        Quaternion rightRotation = Quaternion.Euler(0, 0, -bulletSpreadAngle);
        Vector3 rightDirection = rightRotation * Vector3.down;
        SpawnBullet(rightDirection, spawnPosition);
    }

    /// <summary>
    /// [Server Only] Instantiates, configures (via <see cref="StageSmallBulletMoverScript"/>), 
    /// and spawns (via <see cref="NetworkObject"/>) a single timeout bullet.
    /// </summary>
    /// <param name="direction">The direction the bullet should travel.</param>
    /// <param name="spawnPosition">The world position where the bullet should spawn.</param>
    private void SpawnBullet(Vector3 direction, Vector3 spawnPosition)
    {
        if (spiritLargeBulletPrefab == null)
        {
            Debug.LogError("[SpiritTimeoutAttack] spiritLargeBulletPrefab is not assigned!", this);
            return;
        }

        // Calculate rotation to face the direction
        Quaternion bulletRotation = Quaternion.LookRotation(Vector3.forward, direction); // Use LookRotation for 2D up direction

        // Instantiate the bullet prefab
        GameObject bulletInstance = Instantiate(spiritLargeBulletPrefab, spawnPosition, bulletRotation);

        // Get the NetworkObject
        NetworkObject bulletNetworkObject = bulletInstance.GetComponent<NetworkObject>();
        if (bulletNetworkObject == null)
        {
            Debug.LogError($"[SpiritTimeoutAttack] Timeout bullet prefab '{spiritLargeBulletPrefab.name}' is missing a NetworkObject component.", this);
            Destroy(bulletInstance); // Clean up the non-networked instance
            return;
        }

        // --- Configure Bullet (Requires StageSmallBulletMoverScript) ---
        StageSmallBulletMoverScript bulletMover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        if (bulletMover != null)
        {   
            // Spawn the bullet on the network FIRST
            bulletNetworkObject.Spawn(true); // Spawn server-owned

            // THEN set NetworkVariables *after* spawning
            bulletMover.UseInitialVelocity.Value = true;
            // Consider adding a specific speed field to this component if needed, otherwise use mover's default min.
            bulletMover.InitialVelocity.Value = direction.normalized * bulletMover.GetMinSpeed(); 
            // Timeout bullets aren't targeted at a specific player.
            bulletMover.TargetPlayerRole.Value = PlayerRole.None; 
        }
        else
        {
             Debug.LogWarning($"[SpiritTimeoutAttack] Timeout bullet prefab '{spiritLargeBulletPrefab.name}' is missing StageSmallBulletMoverScript. Spawning without configuration.", this);
            // Still spawn the object if it has a NetworkObject, but warn about missing script.
            bulletNetworkObject.Spawn(true);
        }
        // --------------------------------------
    }
} 