using UnityEngine;
using Unity.Netcode;
// using System.Collections.Generic; // No longer needed for FindObjectsOfType
// using System.Linq; // No longer needed for OfType<T>
using TouhouWebArena; // Add namespace for IClearable and PlayerRole
using System.Collections.Generic; // Required for List<GameObject>

/// <summary>
/// Handles the server-side logic for triggering the player's "death bomb" effect.
/// When executed, it sends a ClientRpc to all clients to clear bullets in a radius.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class PlayerDeathBomb : NetworkBehaviour
{
    private CharacterStats characterStats;
    private const int BOMB_DAMAGE_TO_ENEMIES = 100; // Damage high enough to kill any enemy
    // We might need a reference to PlayerAttackRelay to call ClientRPCs if this script itself isn't the best place.
    // However, since PlayerDeathBomb is on the player prefab which has NetworkObject, it can send ClientRPCs directly.

    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null) Debug.LogError("PlayerDeathBomb requires CharacterStats component!", this);
    }

    public void ExecuteBomb()
    {
        if (!IsServer) return;

        PlayerRole bombingPlayerRole = PlayerRole.None;
        ulong bombingPlayerClientId = OwnerClientId; // Get the ClientId of the player who is bombing

        if (PlayerDataManager.Instance != null)
        {
            PlayerData? playerData = PlayerDataManager.Instance.GetPlayerData(bombingPlayerClientId);
            if (playerData.HasValue) bombingPlayerRole = playerData.Value.Role;
            else { Debug.LogError($"[PlayerDeathBomb] Could not get PlayerData for bombing client {bombingPlayerClientId}. Aborting bomb.", gameObject); return; }
        }
        else { Debug.LogError("[PlayerDeathBomb] PlayerDataManager instance not found. Aborting bomb.", gameObject); return; }

        float currentBombRadius = characterStats.GetDeathBombRadius();
        Vector3 bombCenter = transform.position;

        ClearObjectsInRadiusClientRpc(bombCenter, currentBombRadius, bombingPlayerRole, bombingPlayerClientId);
        Debug.Log($"[Server DeathBomb] Sent ClearObjectsInRadiusClientRpc. Center: {bombCenter}, Radius: {currentBombRadius}, Bomber: {bombingPlayerRole} (Client {bombingPlayerClientId}).");

        // --- Old server-side clearing logic for other IClearable types (fairies, spirits) can remain for now ---
        // This part is NOT for StageSmallBulletMoverScript anymore.
        Collider2D[] hits = Physics2D.OverlapCircleAll(bombCenter, currentBombRadius);
        int otherClearedCount = 0;
        foreach (Collider2D hit in hits)
        {
            IClearable clearable = hit.GetComponentInParent<IClearable>(); // Check GetComponentInParent as well
            if (clearable != null && !(clearable is StageSmallBulletMoverScript)) // Explicitly EXCLUDE StageSmallBulletMoverScript here
            {
                // Existing role check and clear logic for non-stage bullets
                PlayerRole objectRole = PlayerRole.None;
                bool roleFound = false;
                if (clearable is SpiritController spirit) { objectRole = spirit.GetOwnerRole(); roleFound = true; }
                else if (clearable is FairyController fairy) { objectRole = fairy.GetOwnerRole(); roleFound = true; }
                // else if (clearable is TouhouWebArena.Spellcards.Behaviors.NetworkBulletLifetime spellBullet) 
                // {
                //      objectRole = spellBullet.TargetPlayerRole.Value;
                //      roleFound = true;
                // }

                if (roleFound && objectRole == bombingPlayerRole)
                {
                    clearable.Clear(true, bombingPlayerRole); 
                    otherClearedCount++;
                }
            }
        }
        if (otherClearedCount > 0) Debug.Log($"[Server DeathBomb] Cleared {otherClearedCount} other IClearable objects locally on server.");
        // --- End of old server-side clearing logic ---
    }

    [ClientRpc]
    private void ClearObjectsInRadiusClientRpc(Vector3 bombCenter, float bombRadius, PlayerRole bombingPlayerRole, ulong bombingPlayerClientId)
    {
        if (ClientGameObjectPool.Instance == null) 
        {
            Debug.LogWarning("[Client DeathBomb] ClientGameObjectPool.Instance is null. Cannot clear objects.");
            return;
        }

        int objectsClearedCount = 0;
        List<GameObject> allActiveObjects = ClientGameObjectPool.Instance.GetAllActiveObjects();

        foreach (GameObject activeGO in allActiveObjects)
        {
            if (Vector3.Distance(activeGO.transform.position, bombCenter) <= bombRadius)
            {
                // Try to clear as a bullet first
                StageSmallBulletMoverScript bulletMover = activeGO.GetComponent<StageSmallBulletMoverScript>();
                if (bulletMover != null)
                {
                    bulletMover.ForceReturnToPoolByBomb();
                    objectsClearedCount++;
                    continue; // Move to next object if it was a bullet and cleared
                }

                // If not a clearable bullet, try to clear as a Fairy
                ClientFairyHealth fairyHealth = activeGO.GetComponent<ClientFairyHealth>();
                if (fairyHealth != null)
                {
                    // Debug.Log($"[Client DeathBomb] Damaging Fairy {activeGO.name} with bomb from client {bombingPlayerClientId}.");
                    fairyHealth.TakeDamage(BOMB_DAMAGE_TO_ENEMIES, bombingPlayerClientId); // Pass bomber's ID
                    objectsClearedCount++; // Counting cleared/damaged enemies too
                    continue;
                }

                // ADDED: Logic for ClientSpiritHealth
                ClientSpiritHealth spiritHealth = activeGO.GetComponent<ClientSpiritHealth>();
                if (spiritHealth != null)
                {
                    // Debug.Log($"[Client DeathBomb] Damaging Spirit {activeGO.name} with bomb from client {bombingPlayerClientId}.");
                    spiritHealth.TakeDamage(BOMB_DAMAGE_TO_ENEMIES, bombingPlayerClientId);
                    objectsClearedCount++; // Counting cleared/damaged enemies too
                    continue;
                }

                // ADDED: Generic projectile clearing using ClientProjectileLifetime
                // This should catch BaseBullet prefabs if they have this component.
                ClientProjectileLifetime projectileLifetime = activeGO.GetComponent<ClientProjectileLifetime>();
                if (projectileLifetime != null)
                {
                    // Debug.Log($"[Client DeathBomb] Clearing generic projectile {activeGO.name} with bomb.");
                    projectileLifetime.ForceReturnToPool();
                    objectsClearedCount++;
                    continue; 
                }
            }
        }
        // Log total objects affected by the bomb
        // if (objectsClearedCount > 0) Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId} DeathBomb] Processed {objectsClearedCount} objects (bullets/enemies) in bomb radius.");
    }
} 