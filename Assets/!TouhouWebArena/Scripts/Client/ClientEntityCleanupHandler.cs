using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace TouhouWebArena.Client
{
    /// <summary>
    /// Handles the cleanup of client-side only visuals and pooled objects
    /// when instructed by the server (e.g., during a round reset).
    /// This script should be attached to a NetworkObject that is present on all clients.
    /// </summary>
    public class ClientEntityCleanupHandler : NetworkBehaviour
    {
        public static ClientEntityCleanupHandler Instance { get; private set; }

        // Define known prefab IDs for client-side pooled objects that need clearing.
        // These should match the IDs used when pooling them (e.g., in ClientGameObjectPool).
        private readonly List<string> _clientPooledPrefabIDsToClear = new List<string>
        {
            // Entities confirmed to be clearing already
            "Spirit",                       // From ClientSpiritSpawnHandler
            "ReimuExtraAttackOrb",          // From ClientExtraAttackManager
            "MarisaExtraAttackEarthlightRay", // From ClientExtraAttackManager

            // Corrected Charge Attack IDs
            "ReimuChargeTalisman_Client",   // Corrected from "ReimuHakureiTalisman"
            "MarisaChargeLaser_Client",     // Corrected from "MarisaIllusionLaser"

            // Player basic shots (kept for completeness)
            "ReimuBullet",                // From PlayerShootingController (Matches pool ID)
            "MarisaBullet",               // From PlayerShootingController (Matches pool ID)

            // Fairy IDs
            "NormalFairy",
            "GreatFairy",

            // Stage Bullet IDs
            "StageSmallBullet",
            "StageLargeBullet",

            // Spellcard Bullet IDs (examples, add more if needed)
            "BaseBullet",
            "BlueSmallCircle",
            "BlueSmallStar",
            "GreenSmallStar",
            "RedSmallCircle",
            "RedSmallOval",
            "RedTalisman",
            "WhiteSmallCircle",
            "WhiteSmallOval",
            "WhiteTalisman",
            "YellowSmallStar"
            // "FairyShockwave", // Consider if this needs to be cleared if it's a persistent pooled object

            // Add any other client-side specific prefabs here based on ClientGameObjectPool
        };

        public override void OnNetworkSpawn()
        {
            if (!IsClient) // If this instance is on the server
            {
                enabled = false; // Disable the component's Update(), etc.
                // Debug.Log("[ClientEntityCleanupHandler] Instance on Server. Disabling component."); // Optional log
                return; // Don't proceed with client-specific setup
            }

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ClientEntityCleanupHandler] Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (!IsClient)
            {
                // This script has no server-side logic other than being a NetworkBehaviour
                // so clients can find its RPCs.
                // It could be disabled on the server if it has an Update() or other MonoBehaviour messages later.
            }
            // Debug.Log("[ClientEntityCleanupHandler] Spawned and Instance set.");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsClient) // If this instance is on the server
            {
                return; // Server instance doesn't manage the static Instance or log client messages
            }

            if (Instance == this)
            {
                Instance = null;
            }
            // Debug.Log("[ClientEntityCleanupHandler] Despawned and Instance cleared.");
        }

        [ClientRpc]
        public void ClearAllClientSideVisualsClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (!IsClient) return;

            // Debug.Log($"[ClientEntityCleanupHandler - Client {NetworkManager.Singleton.LocalClientId}] Received ClearAllClientSideVisualsClientRpc. Attempting to clear objects from ClientGameObjectPool.");

            if (ClientGameObjectPool.Instance != null)
            {
                int totalCleared = 0;
                foreach (string prefabId in _clientPooledPrefabIDsToClear)
                {
                    // We need a method in ClientGameObjectPool like:
                    // int ClearAllActiveObjectsByID(string prefabId)
                    // For now, let's assume it exists and does its job.
                    // This method would iterate its active lists and return matching objects to the queue.
                    int clearedForId = ClientGameObjectPool.Instance.ReturnAllActiveObjectsById(prefabId); // ASSUMING THIS METHOD EXISTS
                    if (clearedForId > 0)
                    {
                        // Debug.Log($"[ClientEntityCleanupHandler] Returned {clearedForId} objects of type '{prefabId}' to the ClientGameObjectPool.");
                    }
                    totalCleared += clearedForId;
                }
                // Debug.Log($"[ClientEntityCleanupHandler] Total client-side objects returned to pool: {totalCleared}");
            }
            else
            {
                Debug.LogError("[ClientEntityCleanupHandler] ClientGameObjectPool.Instance is null. Cannot clear client-side visuals.");
            }

            // Stop any ongoing client-side fairy spawning
            if (FairySpawnNetworkHandler.Instance != null)
            {
                // Debug.Log("[ClientEntityCleanupHandler] Attempting to stop active fairy spawning coroutines.");
                FairySpawnNetworkHandler.Instance.StopAllActiveFairySpawningCoroutines();
            }
            else
            {
                // This might be okay if no fairies were ever told to spawn this round, or if the handler isn't set up
                // Debug.LogWarning("[ClientEntityCleanupHandler] FairySpawnNetworkHandler.Instance is null. Cannot stop fairy spawning coroutines.");
            }

            // TODO: Add calls to clear other non-pooled client-side effects if any exist.
        }
    }
} 