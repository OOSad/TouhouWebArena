using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Singleton registry to track active spirits per player.
// This runs ONLY on the server.
/// <summary>
/// [Server Only] Singleton registry to track active <see cref="SpiritController"/> instances for each player.
/// Provides methods for registering, deregistering, and counting active spirits based on their owner's <see cref="PlayerRole"/>.
/// </summary>
public class SpiritRegistry : NetworkBehaviour
{
    /// <summary>
    /// Gets the singleton instance of the SpiritRegistry. Server only.
    /// </summary>
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

    /// <summary>
    /// [Server Only] Registers a <see cref="SpiritController"/> with the specified owner.
    /// </summary>
    /// <param name="spirit">The spirit instance to register. Ignored if null.</param>
    /// <param name="ownerRole">The <see cref="PlayerRole"/> of the spirit's owner. Ignored if None.</param>
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

    /// <summary>
    /// [Server Only] Deregisters a <see cref="SpiritController"/> from its owner's list.
    /// </summary>
    /// <param name="spirit">The spirit instance to deregister. Ignored if null.</param>
    /// <param name="ownerRole">The <see cref="PlayerRole"/> of the spirit's owner. Ignored if None.</param>
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

    /// <summary>
    /// [Server Only] Gets the current count of active spirits for a specific player.
    /// </summary>
    /// <param name="role">The <see cref="PlayerRole"/> to query.</param>
    /// <returns>The number of active spirits for the given player, or 0 if the role is invalid or not found.</returns>
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