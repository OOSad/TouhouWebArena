using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TouhouWebArena; // PlayerRole etc.
using TouhouWebArena.Spellcards; // Level4SpellcardData, IllusionHealth

/// <summary>
/// **[Server Only MonoBehaviour]** Manages the lifecycle and tracking of Level 4 persistent illusions.
/// Responsible for spawning, tracking duration, handling despawns, and cleaning up on client disconnects.
/// Should be attached to the same GameObject as <see cref="ServerAttackSpawner"/>.
/// </summary>
public class ServerIllusionManager : NetworkBehaviour // Inherit from NetworkBehaviour for IsServer check and coroutines
{
    /// <summary>Singleton instance of the ServerIllusionManager.</summary>
    public static ServerIllusionManager Instance { get; private set; }

    // --- Active Illusion Tracking (Server Only) ---
    private Dictionary<ulong, NetworkObject> _activeIllusionsTargetingPlayer = new Dictionary<ulong, NetworkObject>();
    private Dictionary<ulong, NetworkObject> _activeIllusionsCastByPlayer = new Dictionary<ulong, NetworkObject>();
    // ---------------------------------------------

    // Note: No Singleton pattern here, managed by ServerAttackSpawner

    /// <summary>
    /// Unity Awake method. Implements the singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ServerIllusionManager] Duplicate instance detected. Destroying self.", gameObject);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Consider DontDestroyOnLoad if it needs to persist independently.
        }
    }

    /// <summary>
    /// Unity OnDestroy method. Clears the singleton instance.
    /// </summary>
    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy(); // Call base OnDestroy
    }

    public override void OnNetworkSpawn()
    {
        // We only need this component active on the server
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// **[Server Only]** Cleans up illusions associated with a disconnecting client.
    /// Called by <see cref="ServerAttackSpawner.HandleClientDisconnect"/>.
    /// </summary>
    /// <param name="disconnectedClientId">The ID of the client that disconnected.</param>
    public void HandleClientDisconnect(ulong disconnectedClientId)
    {
        if (!IsServer) return; // Safety check
        // Clean up illusions related to the disconnected client
        if (_activeIllusionsTargetingPlayer.TryGetValue(disconnectedClientId, out NetworkObject illusionTargeting)) 
        {
            ServerForceDespawnIllusion(illusionTargeting);
        }
        if (_activeIllusionsCastByPlayer.TryGetValue(disconnectedClientId, out NetworkObject illusionCastBy)) 
        {
            ServerForceDespawnIllusion(illusionCastBy);
        }
    }

    /// <summary>
    /// **[Server Only]** Notifies the manager that an illusion has been despawned (e.g., by IllusionHealth.Die).
    /// Removes the illusion from tracking dictionaries.
    /// </summary>
    /// <param name="illusionNO">The NetworkObject of the despawned illusion.</param>
    public void ServerNotifyIllusionDespawned(NetworkObject illusionNO)
    {
        if (!IsServer || illusionNO == null) return;
        // Remove from targeting dictionary
        ulong? targetKeyToRemove = null;
        foreach(var kvp in _activeIllusionsTargetingPlayer)
        {
            if (kvp.Value == illusionNO) { targetKeyToRemove = kvp.Key; break; }
        }
        if (targetKeyToRemove.HasValue) _activeIllusionsTargetingPlayer.Remove(targetKeyToRemove.Value);
        // Remove from caster dictionary
        ulong? casterKeyToRemove = null;
        foreach(var kvp in _activeIllusionsCastByPlayer)
        {
             if (kvp.Value == illusionNO) { casterKeyToRemove = kvp.Key; break; }
        }
        if (casterKeyToRemove.HasValue) _activeIllusionsCastByPlayer.Remove(casterKeyToRemove.Value);
    }

    /// <summary>
    /// **[Server Only]** Forces an illusion to despawn, usually by dealing max damage.
    /// Also notifies the manager via <see cref="ServerNotifyIllusionDespawned"/>.
    /// </summary>
    /// <param name="illusionNO">The NetworkObject of the illusion to despawn.</param>
    private void ServerForceDespawnIllusion(NetworkObject illusionNO)
    {
        if (!IsServer || illusionNO == null || !illusionNO.IsSpawned) return;
        IllusionHealth health = illusionNO.GetComponent<IllusionHealth>();
        if (health != null)
        {
            // Let IllusionHealth handle the despawn via TakeDamage, which should call ServerNotifyIllusionDespawned
            health.TakeDamageServerSide(float.MaxValue, PlayerRole.None); 
        }
        else
        {   
            Debug.LogWarning($"[ServerIllusionManager] Illusion {illusionNO.name} missing IllusionHealth, attempting direct despawn.");
            illusionNO.Despawn(true);
            ServerNotifyIllusionDespawned(illusionNO); // Manual notify if no health component
        }
    }

    /// <summary>
    /// **[Server Only]** Spawns the persistent illusion for a Level 4 spellcard.
    /// Called by <see cref="ServerAttackSpawner.ExecuteSpellcard"/>.
    /// </summary>
    /// <param name="opponentPlayerObject">The opponent's player NetworkObject.</param>
    /// <param name="opponentRole">The opponent's PlayerRole.</param>
    public void ServerSpawnLevel4Illusion(Level4SpellcardData spellData, ulong senderClientId, ulong opponentClientId, NetworkObject opponentPlayerObject, PlayerRole opponentRole)
    {
        if (!IsServer) return;

        // --- Cancellation Logic ---
        // Despawn any existing illusion cast by the sender
        if (_activeIllusionsCastByPlayer.TryGetValue(senderClientId, out NetworkObject oldIllusionCastBySender))
        {
            ServerForceDespawnIllusion(oldIllusionCastBySender);
        }
        // Despawn any existing illusion targeting the sender (cast by the opponent)
        if (_activeIllusionsTargetingPlayer.TryGetValue(senderClientId, out NetworkObject oldIllusionTargetingSender))
        {
            ServerForceDespawnIllusion(oldIllusionTargetingSender);
        }
        // -------------------------

        if (spellData.IllusionPrefab == null) { Debug.LogError("[ServerIllusionManager] Level 4 spell data missing IllusionPrefab!"); return; }
        NetworkObject prefabNO = spellData.IllusionPrefab.GetComponent<NetworkObject>();
        if (prefabNO == null) { Debug.LogError("[ServerIllusionManager] Level 4 IllusionPrefab is missing NetworkObject component!"); return; }

        // Determine Spawn Position (Top-center of opponent's bounds)
        Rect opponentBounds = (opponentRole == PlayerRole.Player1) ? ClientAuthMovement.player1Bounds : ClientAuthMovement.player2Bounds;
        float spawnX = opponentBounds.center.x;
        float spawnY = opponentBounds.yMax - 1.0f; 
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0);
        Quaternion spawnRotation = Quaternion.identity;

        GameObject illusionInstance = Instantiate(spellData.IllusionPrefab, spawnPosition, spawnRotation);
        NetworkObject illusionNO = illusionInstance.GetComponent<NetworkObject>();

        // Spawn NetworkObject FIRST
        illusionNO.Spawn(true); // Server owned

        // Update Trackers
        _activeIllusionsTargetingPlayer[opponentClientId] = illusionNO;
        _activeIllusionsCastByPlayer[senderClientId] = illusionNO;

        // Initialize Controller
        Level4IllusionController controller = illusionInstance.GetComponent<Level4IllusionController>();
        if (controller != null) { controller.ServerInitialize(opponentRole, spellData); }
        else { Debug.LogError($"[ServerIllusionManager] Spawned illusion {illusionInstance.name} missing Level4IllusionController!"); }

        // Initialize Health
        IllusionHealth healthComponent = illusionInstance.GetComponent<IllusionHealth>();
        if (healthComponent != null) 
        {
             healthComponent.ServerInitialize(spellData.Health, opponentRole);
             // Optionally, give health component a reference back to this manager for notification?
             // healthComponent.SetManager(this); 
        }
        else { Debug.LogError($"[ServerIllusionManager] Spawned illusion {illusionInstance.name} missing IllusionHealth!"); }

        // Start Lifetime Management
        StartCoroutine(ServerManageIllusionLifetime(illusionNO, spellData.Duration));
    }

    /// <summary>
    /// **[Server Only Coroutine]** Manages the timed duration of a spawned Level 4 illusion.
    /// </summary>
    private IEnumerator ServerManageIllusionLifetime(NetworkObject illusionNO, float duration)
    {
        if (duration <= 0) duration = 0.1f; // Prevent zero/negative wait
        yield return new WaitForSeconds(duration);
        
        // Check if illusion still exists and is tracked before forcing despawn
        // Check both dictionaries just in case
        bool stillTracked = false;
        foreach(var kvp in _activeIllusionsTargetingPlayer) { if(kvp.Value == illusionNO) { stillTracked = true; break; } }
        if(!stillTracked) { foreach(var kvp in _activeIllusionsCastByPlayer) { if(kvp.Value == illusionNO) { stillTracked = true; break; } } }
        
        if(illusionNO != null && illusionNO.IsSpawned && stillTracked)
        {
            // Use force despawn which calls Die() -> Notify for cleanup
            ServerForceDespawnIllusion(illusionNO); 
        }
    }
} 