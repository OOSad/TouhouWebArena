using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Singleton registry to track active spirits per player.
// This runs ONLY on the server.
public class SpiritRegistry : NetworkBehaviour
{
    public static SpiritRegistry Instance { get; private set; }

    // Dictionaries to store active spirits for each player role
    private readonly Dictionary<PlayerRole, List<SpiritController>> activeSpirits = 
        new Dictionary<PlayerRole, List<SpiritController>>
        {
            { PlayerRole.Player1, new List<SpiritController>() },
            { PlayerRole.Player2, new List<SpiritController>() }
        };

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // This component is server-only
            enabled = false;
            return;
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate SpiritRegistry instance found, destroying self.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Don't use DontDestroyOnLoad for scene-specific managers like this usually
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnNetworkDespawn();
    }

    public void Register(SpiritController spirit, PlayerRole ownerRole)
    {
        if (!IsServer || spirit == null || ownerRole == PlayerRole.None)
        {
             if (!IsServer) Debug.LogWarning("SpiritRegistry.Register called on client!");
             else Debug.LogWarning($"SpiritRegistry.Register called with null spirit or None role. Role: {ownerRole}");
             return;
        }

        if (activeSpirits.ContainsKey(ownerRole))
        {
            if (!activeSpirits[ownerRole].Contains(spirit))
            {
                activeSpirits[ownerRole].Add(spirit);
                // Debug.Log($"[Server SpiritRegistry] Registered Spirit {spirit.NetworkObjectId} for {ownerRole}. Count: {activeSpirits[ownerRole].Count}");
            }
        }
        else
        {
            Debug.LogWarning($"[Server SpiritRegistry] Attempted to register spirit for invalid role: {ownerRole}");
        }
    }

    public void Deregister(SpiritController spirit, PlayerRole ownerRole)
    {
        if (!IsServer || spirit == null || ownerRole == PlayerRole.None)
        {
            if (!IsServer) Debug.LogWarning("SpiritRegistry.Deregister called on client!");
            // Don't warn if role is None during potential shutdown scenarios
            return;
        }

        if (activeSpirits.ContainsKey(ownerRole))
        { 
            bool removed = activeSpirits[ownerRole].Remove(spirit);
            // if(removed) Debug.Log($"[Server SpiritRegistry] Deregistered Spirit {spirit.NetworkObjectId} for {ownerRole}. Count: {activeSpirits[ownerRole].Count}");
        }
        // No warning if role invalid here, might happen during shutdown
    }

    // Get the current count of active spirits for a specific player
    public int GetSpiritCount(PlayerRole role)
    {
        if (!IsServer || role == PlayerRole.None)
        {
            return 0; // Or handle error appropriately
        }

        if (activeSpirits.ContainsKey(role))
        {
            return activeSpirits[role].Count;
        }

        Debug.LogWarning($"[Server SpiritRegistry] GetSpiritCount called for invalid role: {role}");
        return 0;
    }
} 