using UnityEngine;
// DanmakU is only needed for DanmakuConfig now
using DanmakU;
// Remove Controllers namespace
// using DanmakU.Controllers; // This namespace does not exist in 0.7.0a
using System.Collections; // Required for Coroutines
using Unity.Netcode; // Add Netcode namespace

// Inherit from NetworkBehaviour instead of DanmakuBehaviour
public class SmallStageBulletSpawner : NetworkBehaviour
{
    [Header("References")]
    // Remove direct prefab reference, get it from the controller
    // [SerializeField]
    // private DanmakuPrefab bulletPrefab; 

    [Tooltip("Controller responsible for managing the DanmakuSet for bullets.")]
    [SerializeField]
    private DanmakuSetController danmakuSetController; // Assign the controller

    [SerializeField]
    private Transform spawnAreaCenter1; // Assign the first spawn area center transform

    [SerializeField]
    private Transform spawnAreaCenter2; // Assign the second spawn area center transform

    [Header("Spawning Configuration")]
    [SerializeField]
    private float minSpeed = 2f; // Minimum bullet speed

    [SerializeField]
    private float maxSpeed = 4f; // Maximum bullet speed

    [SerializeField]
    [Range(0f, 180f)]
    private float angleVariation = 30f; // Total angle variation (e.g., 30 means +/- 15 degrees from down)

    [SerializeField]
    [Min(0.1f)]
    private float spawnRate = 10f; // Bullets per second (total across both areas)

    [Header("Timing")]
    [SerializeField]
    [Min(0f)]
    private float initialSpawnDelay = 2.0f; // Delay before spawning starts

    // Fixed dimensions for the spawn rectangles
    private const float SpawnAreaWidth = 7f;
    private const float SpawnAreaHeight = 2f;

    // Remove local DanmakuSet management
    // private DanmakuSet bulletSet;
    private Coroutine spawnCoroutine;

    // --- Netcode Lifecycle --- 

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Check if controller is assigned
        if (danmakuSetController == null)
        {
            Debug.LogError("DanmakuSetController is not assigned!", this);
            enabled = false;
            return;
        }
        // The controller handles set creation, just need to wait for it potentially
        // Might need a check later if controller hasn't initialized its set yet.

        if (IsServer)
        {
            // Initial validation moved to StartSpawning
            StartSpawning();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        StopSpawning(); // Stop coroutine if server
        // No need to destroy set here, controller handles it
    }

    // Remove DanmakuSet Management methods
    // private bool CreateLocalDanmakuSet() { ... }
    // private void DestroyLocalDanmakuSet() { ... }

    // --- Spawning Logic --- 

    private void StartSpawning()
    {
        // Ensure only server runs this
        if (!IsServer) return;

        // Stop existing coroutine if any (safety check)
        StopSpawning(); 

        // Validate essential references before starting
        // Now checks controller instead of local set
        if (danmakuSetController == null || spawnAreaCenter1 == null || spawnAreaCenter2 == null)
        {
            Debug.LogError("Cannot start spawning, required references are missing!", this);
            return;
        }

        if (minSpeed > maxSpeed)
        {
            Debug.LogWarning("Min Speed is greater than Max Speed. Clamping Max Speed.", this);
            maxSpeed = minSpeed;
        }

        Debug.Log("Server starting spawn routine.");
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void StopSpawning()
    {
         if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            Debug.Log("Server stopped spawn routine.");
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // This coroutine now only runs on the server
        Debug.Log("SpawnRoutine started on server. Waiting for DanmakuSetController...");

        // Wait until the controller has initialized its ManagedSet
        while (danmakuSetController == null || danmakuSetController.ManagedSet == null)
        {
            if (danmakuSetController == null)
            {
                Debug.LogError("SpawnRoutine waiting, but DanmakuSetController reference is missing! Stopping.", this);
                yield break; // Exit the coroutine
            }
            // Add a small delay to prevent a tight loop if initialization is slow
            yield return null; // Wait for the next frame
        }

        Debug.Log("DanmakuSetController ready. Starting main spawn loop.");

        // --- Add Initial Delay ---
        if (initialSpawnDelay > 0f)
        {
            Debug.Log($"Waiting for initial spawn delay: {initialSpawnDelay} seconds.");
            yield return new WaitForSeconds(initialSpawnDelay);
            Debug.Log("Initial spawn delay finished.");
        }
        // -------------------------

        float baseAngleRad = -Mathf.PI / 2f;
        float halfAngleVariationRad = (angleVariation * Mathf.Deg2Rad) / 2f;
        float halfWidth = SpawnAreaWidth / 2f;
        float halfHeight = SpawnAreaHeight / 2f;

        while (true)
        {
            // Calculate delay for the next spawn
            yield return new WaitForSeconds(1f / spawnRate);

            // Choose a spawn center randomly
            Transform chosenCenter = (Random.value < 0.5f) ? spawnAreaCenter1 : spawnAreaCenter2;
            Vector3 centerPosition = chosenCenter.position;

            // Calculate random offset within the spawn area
            float offsetX = Random.Range(-halfWidth, halfWidth);
            float offsetY = Random.Range(-halfHeight, halfHeight);
            Vector3 spawnPosition = centerPosition + new Vector3(offsetX, offsetY, 0);

            // Randomize parameters on Server
            float randomSpeed = Random.Range(minSpeed, maxSpeed);
            float randomAngleRad = baseAngleRad + Random.Range(-halfAngleVariationRad, halfAngleVariationRad);

            // Call ClientRpc to spawn the bullet on all clients
            SpawnBulletClientRpc(spawnPosition, randomSpeed, randomAngleRad);

            // Server only sends RPC, clients handle the firing.
        }
    }

    // --- Netcode RPC --- 

    [ClientRpc]
    private void SpawnBulletClientRpc(Vector3 spawnPosition, float speed, float angleRad)
    {
        // This code executes on ALL clients (including the host)
        // Get the set from the controller - Added null check for controller itself
        if (danmakuSetController == null)
        {
             Debug.LogError("ClientRpc called but DanmakuSetController reference is missing! Cannot fire bullet.", this);
             return;
        }
        // Check if ManagedSet is ready on the client
        if (danmakuSetController.ManagedSet == null) 
        {   
            // It's possible a client receives the RPC before its local controller is ready.
            // In a more complex scenario, we might queue this or request state, but for now, just log.
            Debug.LogWarning("ClientRpc received, but local DanmakuSetController ManagedSet is not ready yet. Skipping this bullet.", this);
            return;
        }

        // Create the config using parameters received from the server
        var config = new DanmakuConfig
        {
            Position = spawnPosition,
            Rotation = angleRad, // Use radians
            Speed = speed,
            AngularSpeed = 0, // No angular speed for simple linear movement
            Color = Color.white // Default color, can be customized if needed
        };

        // Fire the bullet locally on this client using the controller's set
        danmakuSetController.ManagedSet.Fire(config);
    }
}
