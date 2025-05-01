using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // PlayerRole, CharacterStats etc.

/// <summary>
/// **[Server Only]** Handles the logic for spawning basic shots for players.
/// Instantiated and used by <see cref="ServerAttackSpawner"/>.
/// </summary>
public class ServerBasicShotSpawner
{
    // Constant vertical offset from player center for spawning basic shot pairs.
    private const float firePointVerticalOffset = 0.5f;

    /// <summary>
    /// **[Server Only]** Spawns a pair of basic shot bullets for the requesting player.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the shot.</param>
    public void SpawnBasicShot(ulong requesterClientId)
    {
        // Ensure execution only happens on the server.
        // We assume this check happens in the calling context (ServerAttackSpawner) or that this class is only used server-side.
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ServerBasicShotSpawner] SpawnBasicShot called on client? Aborting."); // Add a warning just in case
            return;
        }

        // Get Sender's Player Object and Character Stats
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerBasicShotSpawner.SpawnBasicShot] Could not find player object for client {requesterClientId}");
            return;
        }
        CharacterStats senderStats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (senderStats == null)
        {   
            // Use string interpolation $ for cleaner logging
            Debug.LogError($"[ServerBasicShotSpawner.SpawnBasicShot] Player object for client {requesterClientId} missing CharacterStats.");
            return;
        }

        // Fetch the correct bullet prefab from the player's stats.
        GameObject bulletToSpawn = senderStats.GetBulletPrefab();
        if (bulletToSpawn == null)
        {   
             // Use string interpolation
            Debug.LogError($"[ServerBasicShotSpawner.SpawnBasicShot] Character {senderStats.GetCharacterName()} has no bullet prefab assigned in CharacterStats.");
            return;
        }

        // Calculate spawn points and rotation based on player transform and stats.
        Transform playerTransform = networkClient.PlayerObject.transform;
        Vector3 centerSpawnPoint = playerTransform.position + playerTransform.up * firePointVerticalOffset;
        Quaternion spawnRotation = playerTransform.rotation;
        float spread = senderStats.GetBulletSpread();
        Vector3 rightOffset = playerTransform.right * (spread / 2f);

        // Spawn the pair using the static pooling helper method.
        ServerPooledSpawner.SpawnSinglePooledBullet(bulletToSpawn, centerSpawnPoint - rightOffset, spawnRotation, requesterClientId);
        ServerPooledSpawner.SpawnSinglePooledBullet(bulletToSpawn, centerSpawnPoint + rightOffset, spawnRotation, requesterClientId);
    }
} 