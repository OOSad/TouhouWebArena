using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using TouhouWebArena.Managers; // For PoolableObjectIdentity, Fairy, SpiritController etc.
using TouhouWebArena.Spellcards; // For IllusionHealth
using TouhouWebArena.Spellcards.Behaviors; // For NetworkBulletLifetime

namespace TouhouWebArena.Helpers
{
    /// <summary>
    /// [Server Only] Provides static helper methods for cleaning up networked entities during round resets.
    /// </summary>
    public static class ServerEntityCleanupHelper
    {
        /// <summary>
        /// Finds and despawns or returns to pool various networked entities present in the scene.
        /// This includes projectiles, fairies, spirits, illusions, and extra attacks.
        /// Should only be called on the server.
        /// </summary>
        public static void CleanupAllEntitiesServer()
        {
            // Ensure NetworkManager is available before proceeding
            if (NetworkManager.Singleton == null)
            {
                 Debug.LogError("[ServerEntityCleanupHelper] NetworkManager is not available. Cannot perform cleanup.");
                 return;
            }

             // Ensure this runs only on the server
            if (!NetworkManager.Singleton.IsServer) {
                Debug.LogWarning("[ServerEntityCleanupHelper] CleanupAllEntitiesServer called on a client. Aborting.");
                return;
            }

            Debug.Log("[ServerEntityCleanupHelper] Starting entity cleanup...");

            // --- Projectiles ---
            List<NetworkObject> projectilesToClear = new List<NetworkObject>();

            // Find Spellcard Bullets
            NetworkBulletLifetime[] spellcardBullets = Object.FindObjectsByType<NetworkBulletLifetime>(FindObjectsSortMode.None);
            foreach (var bullet in spellcardBullets)
            {
                if (bullet.TryGetComponent<NetworkObject>(out var netObj))
                {
                    projectilesToClear.Add(netObj);
                }
            }

            // Find Stage Bullets (Example - Add other types as needed)
            StageSmallBulletMoverScript[] stageBullets = Object.FindObjectsByType<StageSmallBulletMoverScript>(FindObjectsSortMode.None);
            foreach (var bullet in stageBullets)
            {
                 if (bullet.TryGetComponent<NetworkObject>(out var netObj))
                {
                    projectilesToClear.Add(netObj);
                }
            }

            // TODO: Find Player Shots? (Need the specific script component)
            // TODO: Find other projectile types?

            Debug.Log($"[ServerEntityCleanupHelper] Clearing {projectilesToClear.Count} projectiles.");
            foreach (var netObj in projectilesToClear)
            {
                TryReturnOrDespawn(netObj);
            }

            // --- Fairies ---
            FairyController[] activeFairies = Object.FindObjectsByType<FairyController>(FindObjectsSortMode.None);
            Debug.Log($"[ServerEntityCleanupHelper] Clearing {activeFairies.Length} fairies.");
            foreach(var fairy in activeFairies)
            {
                TryReturnOrDespawn(fairy.GetComponent<NetworkObject>());
            }

            // --- Spirits ---
            SpiritController[] activeSpirits = Object.FindObjectsByType<SpiritController>(FindObjectsSortMode.None);
            Debug.Log($"[ServerEntityCleanupHelper] Clearing {activeSpirits.Length} spirits.");
            foreach(var spirit in activeSpirits)
            {
                TryReturnOrDespawn(spirit.GetComponent<NetworkObject>());
            }

            // --- Illusions ---
            IllusionHealth[] activeIllusions = Object.FindObjectsByType<IllusionHealth>(FindObjectsSortMode.None);
            Debug.Log($"[ServerEntityCleanupHelper] Clearing {activeIllusions.Length} illusions.");
            foreach(var illusion in activeIllusions)
            {
                 TryReturnOrDespawn(illusion.GetComponent<NetworkObject>());
            }

            // --- Extra Attacks ---
            GameObject[] extraAttacks = GameObject.FindGameObjectsWithTag("ExtraAttack");
            Debug.Log($"[ServerEntityCleanupHelper] Clearing {extraAttacks.Length} Extra Attack objects.");
            foreach (GameObject extraAttack in extraAttacks)
            {
                if (extraAttack.TryGetComponent<NetworkObject>(out var netObj))
                {
                    TryReturnOrDespawn(netObj); // Use helper now
                }
                else
                {
                    Debug.LogWarning($"[ServerEntityCleanupHelper] Found ExtraAttack tagged object '{extraAttack.name}' without a NetworkObject. Destroying directly.");
                    Object.Destroy(extraAttack);
                }
            }

             Debug.Log("[ServerEntityCleanupHelper] Entity cleanup finished.");
        }


        /// <summary>
        /// Helper method to attempt returning a NetworkObject to the pool, otherwise despawn it.
        /// Server only.
        /// </summary>
        private static void TryReturnOrDespawn(NetworkObject netObj)
        {
            if (netObj == null || !netObj.IsSpawned) return;

            // Check if pooled first
            if (NetworkObjectPool.Instance != null && netObj.TryGetComponent<PoolableObjectIdentity>(out _))
            {
                NetworkObjectPool.Instance.ReturnNetworkObject(netObj);
            }
            else // Fallback to despawn if not pooled or pool unavailable
            {
                netObj.Despawn(true); // true = destroy object after despawn
            }
        }
    }
} 