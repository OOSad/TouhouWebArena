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
             return;
        }

        if (activeSpirits.ContainsKey(ownerRole))
        {
            if (!activeSpirits[ownerRole].Contains(spirit))
            {
                activeSpirits[ownerRole].Add(spirit);
            }
        }
    }

    public void Deregister(SpiritController spirit, PlayerRole ownerRole)
    {
        if (!IsServer || spirit == null || ownerRole == PlayerRole.None)
        {
            return;
        }

        if (activeSpirits.ContainsKey(ownerRole))
        { 
            bool removed = activeSpirits[ownerRole].Remove(spirit);
        }
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

        return 0;
    }
} 