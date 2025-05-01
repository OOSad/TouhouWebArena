using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // CharacterStats etc.

/// <summary>
/// **[Server Only]** Handles the logic for spawning character-specific charge attacks.
/// Instantiated and used by <see cref="ServerAttackSpawner"/>.
/// Charge attacks are typically **non-pooled**.
/// </summary>
public class ServerChargeAttackSpawner
{
    /// <summary>
    /// **[Server Only]** Spawns the appropriate (non-pooled) charge attack for the requesting player's character.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the attack.</param>
    public void SpawnChargeAttack(ulong requesterClientId)
    {
        // Assuming server check is done by caller
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ServerChargeAttackSpawner] SpawnChargeAttack called on client? Aborting.");
            return;
        }

        // Get player object and stats
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Could not find player object for client {requesterClientId}");
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {
             Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Player object for client {requesterClientId} missing CharacterStats.");
             return;
        }

        // Fetch the charge attack prefab from stats.
        GameObject chargePrefab = stats.GetChargeAttackPrefab();
        if (chargePrefab == null)
        {
            Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Character {stats.GetCharacterName()} has no charge attack prefab assigned in CharacterStats.");
            return;
        }

        // Determine Character and Execute specific spawn logic (no pooling).
        string characterName = stats.GetCharacterName();
        if (characterName == "HakureiReimu")
        {
            SpawnReimuChargeAttack(playerTransform, requesterClientId, chargePrefab);
        }
        else if (characterName == "KirisameMarisa")
        {
            SpawnMarisaChargeAttack(playerTransform, requesterClientId, chargePrefab);
        }
        else
        {
            Debug.LogWarning($"[ServerChargeAttackSpawner.SpawnChargeAttack] Unknown character '{characterName}' attempting charge attack for client {requesterClientId}. No attack defined.");
        }
    }

    /// <summary>
    /// **[Server Only]** Spawns Reimu's charge attack pattern (**Non-Pooled**).
    /// Instantiates, assigns ownership, and spawns multiple talismans.
    /// </summary>
    private void SpawnReimuChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        // Define spawn offsets relative to the player
        Vector3 forward = playerTransform.up;
        Vector3 right = playerTransform.right;
        float forwardOffset = 0.8f;
        float horizontalSpread = 0.3f;

        Vector3[] relativePositions = new Vector3[4]
        {
            forward * forwardOffset - right * horizontalSpread * 1.5f,
            forward * forwardOffset - right * horizontalSpread * 0.5f,
            forward * forwardOffset + right * horizontalSpread * 0.5f,
            forward * forwardOffset + right * horizontalSpread * 1.5f
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = playerTransform.position + relativePositions[i];
            // Instantiate and spawn directly, DO NOT use the pool helper
            GameObject instance = Object.Instantiate(attackPrefab, spawnPos, playerTransform.rotation);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // Optional: Assign owner role if charge attacks have BulletMovement component
                // BulletMovement bm = instance.GetComponent<BulletMovement>();
                // if (bm != null && PlayerDataManager.Instance != null) { ... assign bm.OwnerRole.Value ... }
                netObj.SpawnWithOwnership(ownerClientId);
            }
            else
            {
                Debug.LogError($"[ServerChargeAttackSpawner] Reimu Charge Attack prefab '{attackPrefab.name}' is missing NetworkObject component!");
                Object.Destroy(instance);
            }
        }
    }

    /// <summary>
    /// **[Server Only]** Spawns Marisa's charge attack (Illusion Laser) (**Non-Pooled**).
    /// Instantiates, assigns ownership, and spawns the laser.
    /// </summary>
    private void SpawnMarisaChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        float forwardOffset = 0.5f;
        Vector3 spawnPos = playerTransform.position + playerTransform.up * forwardOffset;
        Quaternion spawnRot = Quaternion.identity; // Laser rotation usually fixed or handled by prefab/component

        // Instantiate and spawn directly, DO NOT use the pool helper
        GameObject instance = Object.Instantiate(attackPrefab, spawnPos, spawnRot);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Optional: Assign owner role if charge attacks have BulletMovement component
            netObj.SpawnWithOwnership(ownerClientId);
        }
        else
        {
            Debug.LogError($"[ServerChargeAttackSpawner] Marisa Charge Attack prefab '{attackPrefab.name}' is missing NetworkObject component!");
            Object.Destroy(instance);
        }
    }
} 