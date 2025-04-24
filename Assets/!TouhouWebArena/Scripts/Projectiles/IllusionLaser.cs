using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components; // Added for NetworkTransform

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))] // Ensure a collider is present
[RequireComponent(typeof(NetworkTransform))] // Ensure position sync
public class IllusionLaser : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float hitInterval = 0.1f; // Time between damage ticks per enemy
    [SerializeField] private float duration = 0.5f;    // How long the laser lasts
    [Tooltip("Visual/origin offset relative to player's position.")]
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 0.5f); // Offset from player center

    [Header("Targeting")]
    [SerializeField] private List<string> targetTags = new List<string>() { "Fairy", "Spirit" };
    [Tooltip("Top Y boundary of the play area in world space. Used for scaling.")]
    [SerializeField] public float PlayAreaMaxY = 5f; // Re-added and made public for spawner access

    // Server-side state
    private Dictionary<Collider2D, float> lastHitTimes = new Dictionary<Collider2D, float>();
    private PlayerRole ownerRole = PlayerRole.None;
    private Transform ownerTransform; // Reference to the player who fired the laser

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Only server needs to track state and handle despawn/following
        if (IsServer)
        {
            // Determine owner role
            if (PlayerDataManager.Instance != null)
            {
                PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
                if (ownerData.HasValue)
                {
                    ownerRole = ownerData.Value.Role;
                }
            }
            if (ownerRole == PlayerRole.None)
            {
                
            }

            // --- Find Owner Transform --- 
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId) != null)
            {
                ownerTransform = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId).transform;
                
            }
            if (ownerTransform == null)
            {
                
            }
            // --------------------------

            // Start lifetime coroutine
            StartCoroutine(DespawnAfterDuration());
        }
    }

    private IEnumerator DespawnAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        if (IsServer && IsSpawned) // Check IsServer as only server should despawn
        {
            
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null) netObj.Despawn(true);
        }
    }

    // --- NEW: Update method to follow player X --- 
    void Update()
    {
        // Only run on server and if we have a valid owner transform
        if (!IsServer || ownerTransform == null)
        {
            return;
        }

        // Calculate the target position based on owner and offset
        Vector3 targetPosition = ownerTransform.position + (Vector3)followOffset; 

        // Update the laser's position to match the owner (with offset)
        // Keep the laser's Z position unchanged
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
    }
    // --------------------------------------------

    // --- Collision Handling (Server Only) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        ProcessCollision(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsServer) return;
        ProcessCollision(other, false);
    }

    private void OnTriggerExit2D(Collider2D other)
    { 
        if (!IsServer) return;
        // Clean up dictionary when object leaves trigger
        if (lastHitTimes.ContainsKey(other))
        {
            lastHitTimes.Remove(other);
        }
    }

    // Common collision processing logic
    private void ProcessCollision(Collider2D other, bool isEnterCollision)
    {
        // 1. Check Tag
        if (!targetTags.Contains(other.gameObject.tag)) return;

        // 2. Check Ownership
        bool ownershipMatches = false;
        if (other.TryGetComponent<SpiritController>(out var spirit) && spirit.GetOwnerRole() == ownerRole)
        {
            ownershipMatches = true;
        }
        else if (other.TryGetComponent<Fairy>(out var fairy) && fairy.GetOwnerRole() == ownerRole)
        {
            ownershipMatches = true;
        }
        if (!ownershipMatches) return;

        // 4. Apply Damage based on interval
        float currentTime = Time.time;
        bool canDamage = false;

        if (isEnterCollision) // Always damage on first contact
        {
            canDamage = true;
        }
        else // For OnTriggerStay, check interval
        {
            if (lastHitTimes.TryGetValue(other, out float lastHitTime))
            {
                if (currentTime >= lastHitTime + hitInterval)
                {
                    canDamage = true;
                }
            }
            else
            {
                // Should technically have been added by OnTriggerEnter, but add as fallback
                canDamage = true; 
            }
        }

        // 5. Apply damage if allowed
        if (canDamage)
        {
            ApplyDamage(other);
            lastHitTimes[other] = currentTime; // Update last hit time
        }
    }

    // Helper to apply damage to the correct component
    private void ApplyDamage(Collider2D target)
    {
        bool applied = false;
        if (target.TryGetComponent<Fairy>(out Fairy fairy))
        {
            
            fairy.ApplyDamageServer(damageAmount, ownerRole); 
            applied = true;
        }
        else if (target.TryGetComponent<SpiritController>(out SpiritController spirit))
        {
            
            spirit.ApplyDamageServer(damageAmount, ownerRole);
            applied = true;
        }
        
        if (!applied) { 
             
        }
    }
} 