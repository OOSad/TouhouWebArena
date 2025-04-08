using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Likely needed if NetworkTransform used on bullets
using System.Collections; // Required for Coroutines
// using System.Collections; // No longer needed

[RequireComponent(typeof(NetworkObject))] // Ensure player has NetworkObject
public class PlayerShooting : NetworkBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab; // Assign your Bullet Prefab in the Inspector
    // [SerializeField] private Transform firePoint;     // Removed - will calculate dynamically
    [SerializeField] private KeyCode fireKey = KeyCode.Z; // Configurable fire key
    [SerializeField] private float bulletSpread = 0.2f; // Horizontal distance between the pair of bullets

    [Header("Burst Fire Settings")]
    [SerializeField] private int burstCount = 3; // Number of pairs in a burst
    [SerializeField] private float timeBetweenBurstShots = 0.08f; // Delay between pairs in a burst
    [SerializeField] private float burstCooldown = 0.3f; // Delay after a burst finishes before next can start

    // [Header("Stream Fire Settings")] // Removed stream settings
    // [SerializeField] private float streamFireRate = 8f; // Bullets per second for stream fire

    // Private variables
    private float nextFireTime = 0f; // Time when next burst can start
    private const float firePointVerticalOffset = 0.5f; // How far above the player center bullets spawn
    private Coroutine burstCoroutine; // To track if a burst is active
    // private int shotsFiredSinceKeyDown = 0; // Removed counter

    void Start()
    {
        // Initialize to allow firing immediately
        // shotsFiredSinceKeyDown = burstCount;
    }

    void Update()
    {
        // Only the owner of this object should process input and request firing
        if (!IsOwner) return;

        // --- Burst Fire on KeyDown Logic ---
        if (Input.GetKeyDown(fireKey))
        {
            // Check if enough time has passed and no burst is currently active
            if (Time.time >= nextFireTime && burstCoroutine == null)
            {
                // Start the burst sequence
                burstCoroutine = StartCoroutine(BurstFireSequence());

                // Calculate cooldown: total burst duration + burst cooldown delay
                // Note: Burst duration calculation depends on *number of intervals*, not number of shots.
                float burstDuration = burstCount > 1 ? (burstCount - 1) * timeBetweenBurstShots : 0f;
                nextFireTime = Time.time + burstDuration + burstCooldown;
            }
        }
    }

    private IEnumerator BurstFireSequence()
    {
        Debug.Log("Starting Burst Fire Sequence");
        for (int i = 0; i < burstCount; i++)
        {
            // Tell the server to fire a bullet pair
            RequestFireServerRpc();
            Debug.Log($"Requested burst shot {i + 1}");

            // Wait before the next shot in the burst (unless it's the last one)
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(timeBetweenBurstShots);
            }
        }
        Debug.Log("Finished Burst Fire Sequence");
        burstCoroutine = null; // Allow next burst after cooldown
    }

    [ServerRpc]
    private void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab is not assigned on the server!");
            return;
        }
        // if (firePoint == null) // Removed null check for firePoint
        // {
        //     Debug.LogError("Fire Point is not assigned on the server!");
        //     return;
        // }

        // Calculate the center spawn point above the player
        // Vector3 spawnPosition = transform.position + Vector3.up * firePointVerticalOffset; // Old single point
        Vector3 centerSpawnPoint = transform.position + transform.up * firePointVerticalOffset; // Use transform.up for player's local up
        Quaternion spawnRotation = transform.rotation; // Use player's rotation

        // Calculate left and right offset vectors based on player's right direction
        Vector3 rightOffset = transform.right * (bulletSpread / 2f);
        Vector3 leftOffset = -rightOffset;

        // Calculate final positions for the bullet pair
        Vector3 spawnPositionLeft = centerSpawnPoint + leftOffset;
        Vector3 spawnPositionRight = centerSpawnPoint + rightOffset;

        // Instantiate the bullet on the server at the calculated position
        // GameObject bulletInstance = Instantiate(bulletPrefab, spawnPosition, spawnRotation); // Old single spawn

        // Spawn the left bullet
        SpawnSingleBullet(spawnPositionLeft, spawnRotation);

        // Spawn the right bullet
        SpawnSingleBullet(spawnPositionRight, spawnRotation);
    }

    // Helper method to spawn one bullet
    private void SpawnSingleBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bulletInstance = Instantiate(bulletPrefab, position, rotation);
        NetworkObject bulletNetworkObject = bulletInstance.GetComponent<NetworkObject>();

        if (bulletNetworkObject == null)
        {
            Debug.LogError("Bullet Prefab is missing a NetworkObject component!");
            Destroy(bulletInstance);
            return;
        }

        // Spawn the bullet so it syncs to clients
        bulletNetworkObject.Spawn(true);
        Debug.Log($"Server spawned bullet {bulletInstance.name} at {position}");
    }
} 