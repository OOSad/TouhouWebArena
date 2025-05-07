using UnityEngine;
// Removed: using Unity.Netcode;
// Removed: using TouhouWebArena; // PlayerRole no longer used here

/// <summary>
/// [Client-Side] Handles the specific attack pattern executed when a Spirit times out.
/// Spawns configured bullets in a spread pattern using ClientGameObjectPool.
/// Should be attached to the same GameObject as the (client-side) SpiritController.
/// Called by the client-side SpiritController.
/// </summary>
public class SpiritTimeoutAttack : MonoBehaviour // Changed from server-only script
{
    [Header("Timeout Attack Config")]
    [Tooltip("The Prefab ID (from PooledObjectInfo) for the bullet spawned during the timeout attack. Should be 'LargeStageBullet'.")]
    [SerializeField] private string bulletPrefabIDToSpawn = "LargeStageBullet"; // Changed field name and default

    [Tooltip("Spread angle (degrees) for the side bullets fired during timeout.")]
    [SerializeField] private float bulletSpreadAngle = 15f;

    private ClientGameObjectPool _clientObjectPool;

    void Awake()
    {
        // Get the pool instance. This script will be on a client-side spirit.
        _clientObjectPool = ClientGameObjectPool.Instance;
        if (_clientObjectPool == null)
        {
            Debug.LogError("[SpiritTimeoutAttack] ClientGameObjectPool.Instance is null! Cannot spawn bullets.", this);
            enabled = false; // Disable if pool is not available
        }
    }

    /// <summary>
    /// [Client-Side] Executes the timeout attack, spawning three bullets.
    /// </summary>
    /// <param name="spawnPosition">The world position where the bullets should originate.</param>
    public void ExecuteAttack(Vector3 spawnPosition)
    {
        if (!_clientObjectPool) // Check if pool was found in Awake
        {
            Debug.LogWarning("[SpiritTimeoutAttack] ExecuteAttack called, but pool is not available. Aborting.", this);
            return; 
        }
        
        SpawnBullet(Vector3.down, spawnPosition);

        Quaternion leftRotation = Quaternion.Euler(0, 0, bulletSpreadAngle);
        Vector3 leftDirection = leftRotation * Vector3.down;
        SpawnBullet(leftDirection, spawnPosition);

        Quaternion rightRotation = Quaternion.Euler(0, 0, -bulletSpreadAngle);
        Vector3 rightDirection = rightRotation * Vector3.down;
        SpawnBullet(rightDirection, spawnPosition);
    }

    /// <summary>
    /// [Client-Side] Gets a bullet from the pool, positions it, and initializes its movement.
    /// </summary>
    private void SpawnBullet(Vector3 direction, Vector3 spawnPosition)
    {
        if (string.IsNullOrEmpty(bulletPrefabIDToSpawn))
        {
            Debug.LogError("[SpiritTimeoutAttack] bulletPrefabIDToSpawn is not assigned!", this);
            return;
        }

        GameObject bulletInstance = _clientObjectPool.GetObject(bulletPrefabIDToSpawn);

        if (bulletInstance == null)
        {
            Debug.LogWarning($"[SpiritTimeoutAttack] Failed to get bullet '{bulletPrefabIDToSpawn}' from pool.", this);
            return;
        }

        bulletInstance.transform.position = spawnPosition;
        // Calculate rotation to face the direction of travel
        // For 2D, if bullets visually rotate to face direction:
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f; // +90 if sprite faces upwards
        bulletInstance.transform.rotation = Quaternion.Euler(0, 0, angle);
        // If bullets always face one direction (e.g., down), set Quaternion.identity or a fixed rotation.
        // bulletInstance.transform.rotation = Quaternion.identity;

        StageSmallBulletMoverScript bulletMover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        if (bulletMover != null)
        {   
            // Use the bullet's own default speed and max lifetime
            bulletMover.Initialize(direction, bulletMover.DefaultSpeed, bulletMover.MaxLifetime);
        }
        else
        {
             Debug.LogWarning($"[SpiritTimeoutAttack] Bullet prefab '{bulletPrefabIDToSpawn}' is missing StageSmallBulletMoverScript. Cannot initialize movement.", bulletInstance);
        }
        bulletInstance.SetActive(true);
    }
} 