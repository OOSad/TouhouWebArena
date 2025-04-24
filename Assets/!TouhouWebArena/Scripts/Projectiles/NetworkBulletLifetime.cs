using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

/// <summary>
/// Manages the lifetime of a networked bullet, handles boundary checks,
/// interacts with the object pool, and implements the IClearable interface
/// to allow removal by bombs and shockwaves based on configuration.
/// Primarily server-side logic.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PoolableObjectIdentity))]
public class NetworkBulletLifetime : NetworkBehaviour, IClearable // Implement IClearable
{
    // ... (Fields: maxLifetime, enforceBounds, boundaryX, keepOnPositiveSide, isNormallyClearable, isReturning) ...

    // ... (OnNetworkSpawn, OnNetworkDespawn, Update, CheckBounds, ReturnToPool methods) ...

    #region IClearable Implementation

    /// <summary>
    /// Handles the bullet being cleared by effects like PlayerDeathBomb or Shockwave.
    /// Checks the forceClear flag and the isNormallyClearable setting to determine
    /// if the bullet should be returned to the pool.
    /// Runs only on the server.
    /// </summary>
    /// <param name="forceClear">If true, the bullet is cleared regardless of isNormallyClearable.</param>
    /// <param name="sourceRole">The role of the player causing the clear (ignored by this implementation).</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {
        // Logic only runs on the server
        if (!IsServer || isReturning) return;

        // If it's a forced clear (bomb) OR this bullet is normally clearable (shockwave)
        if (forceClear || isNormallyClearable)
        {
            ReturnToPool(); // Reuse existing pooling logic
        }
        // Else: Normal clear attempt on a bullet that is not normally clearable - do nothing.
    }

    #endregion

    // ... OnTriggerEnter2D ...
} 