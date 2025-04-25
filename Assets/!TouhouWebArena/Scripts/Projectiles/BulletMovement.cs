using UnityEngine;
using Unity.Netcode;
using TouhouWebArena.Spellcards; // Added for IllusionHealth

// Require NetworkObject as this script assumes the bullet is networked
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PoolableObjectIdentity))] // Ensure it has the identity component
/// <summary>
/// Base class for handling projectile movement, lifetime, collision, and ownership.
/// Assumes server-authoritative movement and collision detection.
/// Manages automatic despawning based on lifetime and handles returning the object to the <see cref="NetworkObjectPool"/>.
/// </summary>
public class BulletMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f; // Speed of the bullet
    [SerializeField] private float bulletLifetime = 3.0f; // Seconds before the bullet despawns automatically

    // NetworkVariable to identify the owner
    /// <summary>
    /// [Server Write, Client Read] Identifies which player (<see cref="PlayerRole"/>) owns this bullet.
    /// Used for attributing kills/damage and potentially for collision filtering.
    /// </summary>
    public NetworkVariable<PlayerRole> OwnerRole { get; private set; } = 
        new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isDespawning = false; // Flag to prevent double despawn/return

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isDespawning = false; // Reset flag on spawn

        // Only the server should manage the lifetime and despawning
        if (IsServer)
        {
            // Cancel any potentially lingering invokes from previous pooling
            CancelInvoke(nameof(ReturnToPool)); 
            // Start the timer to return to pool
            Invoke(nameof(ReturnToPool), bulletLifetime);
        }
    }

    // Called when the object is despawned (e.g., by server or host)
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Always cancel invokes when despawned, regardless of reason
        CancelInvoke(nameof(ReturnToPool));
        
        // Reset flag immediately
        isDespawning = false; 

        // --- Client-Side Visual Hiding --- 
        if (!IsServer) // Only clients need to explicitly hide visuals on despawn sometimes
        {
            // Use GetComponentInChildren to find renderer on child object
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>(); 
            if (sprite != null) 
            { 
                sprite.enabled = false; 
            }
            // Add similar checks for MeshRenderer if using 3D models
            // MeshRenderer mesh = GetComponentInChildren<MeshRenderer>();
            // if (mesh != null) { mesh.enabled = false; }
        }
        // ---------------------------------

        // Note: We don't return to pool here automatically on Despawn.
        // ReturnToPool is called explicitly by server logic that wants to reuse the object.
    }

    // Called when the GameObject is disabled (e.g., when returned to pool)
    void OnDisable()
    {
        // Cancel invokes if the object is disabled externally
        CancelInvoke(nameof(ReturnToPool));
        isDespawning = false; // Reset flag
    }

    void Update()
    {
        // --- Server-Authoritative Movement ---
        if (IsServer) // Re-added server check
        {
            // Move the bullet forward based on its local 'up' direction
            // Assumes the bullet sprite/model is oriented so 'up' is forward
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.Self);
        }

        // Server still handles despawn timer (via Invoke in OnNetworkSpawn)
        // Clients rely on NetworkTransform primarily for correction/late-join sync now
        // With interpolation enabled, NetworkTransform provides smooth movement on clients.
    }

    // Server-side collision detection
    void OnTriggerEnter2D(Collider2D other)
    {
        // --- VERY FIRST LOG --- 
        Debug.Log($"[BulletMovement] Server: OnTriggerEnter2D called with other: {other.name} (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})", gameObject);
        // ---------------------

        if (!IsServer || isDespawning) return; // Only server handles collisions, ignore if already despawning

        bool shouldDespawn = false; // Flag to indicate if bullet should be despawned

        // --- Check for Illusion Hit FIRST --- 
        if (other.CompareTag("Illusion")) // Assumes Illusion prefab has this tag
        {
            IllusionHealth illusionHealth = other.GetComponent<IllusionHealth>();
            if (illusionHealth != null)
            {
                Debug.Log($"[BulletMovement] Server: Hit Illusion object '{other.name}'. Checking owner role...", gameObject);
                // Check if this bullet belongs to the player the illusion is targeting
                if (OwnerRole.Value == illusionHealth.TargetedPlayerRole)
                {
                    Debug.Log($"[BulletMovement] Server: Owner role ({OwnerRole.Value}) MATCHES target role ({illusionHealth.TargetedPlayerRole}). Proceeding with damage.", gameObject);
                    // Get damage from ProjectileDamager (or default to 1)
                    float damageToDeal = 1f;
                    ProjectileDamager damager = GetComponent<ProjectileDamager>();
                    if (damager != null) 
                    {
                        damageToDeal = damager.damage;
                    }

                    Debug.Log($"[BulletMovement] Server: Attempting to call IllusionHealth directly...", gameObject);
                    // Deal damage via direct server-side method call
                    illusionHealth.TakeDamageServerSide(damageToDeal, OwnerRole.Value);
                    shouldDespawn = true; // Mark bullet for despawn
                }
                else
                {
                    Debug.Log($"[BulletMovement] Server: Owner role ({OwnerRole.Value}) does NOT match target role ({illusionHealth.TargetedPlayerRole}). Ignoring hit.", gameObject);
                }
            }
            else { Debug.LogWarning("[BulletMovement] Hit object with Illusion tag but missing IllusionHealth component!", other.gameObject); }
        }
        // --- Check for Shockwave collision --- 
        else if (other.CompareTag("FairyShockwave")) // Ensure Shockwave prefab has this tag
        {
            shouldDespawn = true;
        }
        // --- Check for Fairy Hit --- 
        else if (other.CompareTag("Fairy")) // Correct check
        {
            Fairy fairy = other.GetComponent<Fairy>();
            if (fairy != null)
            {
                // Ensure damage is only dealt by the opponent
                 PlayerData? ownerData = PlayerDataManager.Instance?.GetPlayerData(OwnerClientId);
                 // Use the public GetOwnerRole() method
                 if (ownerData.HasValue && fairy.GetOwnerRole() != ownerData.Value.Role)
                 {
                    fairy.ApplyLethalDamage(OwnerRole.Value); // Pass PlayerRole
                    shouldDespawn = true; 
                 }
            }
        }
        // Check if we hit a Spirit
        else if (other.CompareTag("Spirit")) // Add check for Spirit tag
        {
            SpiritController spirit = other.GetComponent<SpiritController>();
            if (spirit != null)
            {
                PlayerData? ownerData = PlayerDataManager.Instance?.GetPlayerData(OwnerClientId);
                PlayerRole spiritOwner = spirit.GetOwnerRole(); // Get role once

                // --- DEBUG LOGGING for Spirit Hit ---
                if (ownerData.HasValue)
                {
                     Debug.Log($"[BulletMovement] Server: Hit Spirit. Bullet Owner={ownerData.Value.Role}, Spirit Owner={spiritOwner}", gameObject);
                }
                else { Debug.LogWarning("[BulletMovement] Server: Hit Spirit, but couldn't get PlayerData for bullet owner!", gameObject); }
                // -------------------------------------

                 // Use the public GetOwnerRole() method
                 // REMOVED Condition: if (ownerData.HasValue && spiritOwner != ownerData.Value.Role)
                 // Spirits are always damageable by any player shot.
                Debug.Log("[BulletMovement] Server: Applying damage to spirit.", gameObject); // Updated log
                int damageAmount = 1; 
                // Get damage from ProjectileDamager if attached
                ProjectileDamager damager = GetComponent<ProjectileDamager>();
                if (damager != null) damageAmount = (int)damager.damage;

                spirit.ApplyDamageServer(damageAmount, OwnerRole.Value); // Pass damage, then PlayerRole
                shouldDespawn = true; 
            }
        }
        // ------------------------------------

        // If any collision triggered despawn logic:
        if (shouldDespawn)
        {
            ReturnToPool(); // Use normal pooling
        }
    }

    /// <summary>
    /// Handles despawning the object and returning it to the pool.
    /// Should only be called on the server.
    /// </summary>
    private void ReturnToPool()
    {
        if (!IsServer || isDespawning) return; // Prevent double calls
        
        // Check if the object still exists and is spawned before proceeding
        if (gameObject == null || NetworkObject == null || !NetworkObject.IsSpawned)
        {
            isDespawning = false; // Reset flag if aborting here
            return; // Object already gone or not networked correctly
        }

        isDespawning = true; // Set flag
        CancelInvoke(nameof(ReturnToPool)); // Cancel timer invoke just in case

        // Despawn the object without destroying it
        NetworkObject.Despawn(false); 

        // Return the object to the pool
        if (NetworkObjectPool.Instance != null)
        {
            NetworkObjectPool.Instance.ReturnNetworkObject(NetworkObject);
        }
        else
        {
            Debug.LogWarning("NetworkObjectPool instance not found when trying to return bullet. Destroying instead.", this);
            Destroy(gameObject); 
        }
    }

    /// <summary>
    /// [Server Only] Initiates the process of despawning the bullet and returning it to the pool.
    /// If called on a client, this method does nothing.
    /// </summary>
    public void DespawnBullet()
    {
         if (IsServer)
         {
            ReturnToPool();
         }
         // If called on client, do nothing - server handles despawn
    }
} 