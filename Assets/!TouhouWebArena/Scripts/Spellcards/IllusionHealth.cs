using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the health of a Level 4 spellcard's illusion.
/// Health is primarily managed client-side by the player who is targeted by the illusion.
/// When the illusion's health reaches zero on the responsible client, that client notifies the server via an RPC.
/// On the server, this component caches its ServerIllusionOrchestrator to pass on the death report.
/// </summary>
public class IllusionHealth : NetworkBehaviour
{
    // Client-side state, authoritative on the `isResponsibleClient`.
    private float currentHealth;
    private float maxHealth;
    private ulong targetedPlayerId; // The NetworkObjectId of the player this illusion is targeting.
    private bool isResponsibleClient; // True if this client instance is the one targeted by the illusion and thus responsible for its health updates.
    private bool isDead = false; // Client-side flag to prevent further processing after death is registered.

    // Server-side cache.
    private ServerIllusionOrchestrator _serverOrchestrator; // Cached on server to forward death reports.

    // ADDED: Client-side cache for visuals
    private ClientIllusionView _clientView;

    /// <summary>
    /// Called when the network object is spawned.
    /// On the server, it caches the ServerIllusionOrchestrator component.
    /// On the client, it caches the ClientIllusionView component.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            _serverOrchestrator = GetComponent<ServerIllusionOrchestrator>();
            if (_serverOrchestrator == null)
            {
                Debug.LogError($"[IllusionHealth] ServerIllusionOrchestrator component not found on {gameObject.name} on the server!");
            }
        }
        if (IsClient)
        {
             _clientView = GetComponent<ClientIllusionView>();
             if (_clientView == null)
             {
                Debug.LogError($"[IllusionHealth] ClientIllusionView component not found on {gameObject.name} on the client! Flash effect will not work.");
             }
        }
    }

    /// <summary>
    /// Initializes the illusion's health state. Called by ClientIllusionView.InitializeClientRpc on all clients.
    /// Sets max health, current health, the ID of the player targeted by the illusion,
    /// and determines if the current client is the one responsible for processing damage to this illusion.
    /// </summary>
    /// <param name="initialHealth">The starting and maximum health of the illusion.</param>
    /// <param name="targetId">The NetworkObjectId of the player this illusion is targeting.</param>
    /// <param name="isClientTargeted">True if the local client is the one being targeted by this illusion.</param>
    public void Initialize(float initialHealth, ulong targetId, bool isClientTargeted)
    {
        maxHealth = initialHealth;
        currentHealth = initialHealth;
        targetedPlayerId = targetId;
        isResponsibleClient = isClientTargeted;
        isDead = false;
        // Debug.Log($"[IllusionHealth {NetworkObjectId}] Initialized. MaxHealth: {maxHealth}, TargetPlayer: {targetedPlayerId}, IsResponsibleClient: {isResponsibleClient}");
    }

    /// <summary>
    /// Client-side trigger detection for collisions with "PlayerShot" tagged objects.
    /// Only processes damage if this client is the one targeted by the illusion (`isResponsibleClient`) and the illusion is not already dead.
    /// If a valid projectile hits, it calls TakeDamageClientSide and attempts to despawn the projectile.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsClient || !isResponsibleClient || isDead) return; // Only the targeted client processes hits

        if (other.CompareTag("PlayerShot"))
        {
            ProjectileDamager damager = other.GetComponent<ProjectileDamager>();
            if (damager != null)
            {
                // Debug.Log($"[IllusionHealth {NetworkObjectId}] PlayerShot hit by {other.name} for {damager.damage} damage.");
                TakeDamageClientSide(damager.damage);

                ClientProjectileLifetime bulletLifetime = other.GetComponent<ClientProjectileLifetime>();
                if (bulletLifetime != null)
                {
                    bulletLifetime.ForceReturnToPool(); 
                }
                else
                {
                    other.gameObject.SetActive(false); 
                }
            }
        }
    }

    /// <summary>
    /// Client-side method to apply damage to the illusion.
    /// Decrements health. If health drops to or below zero, marks the illusion as dead
    /// and calls ReportDeathToServerRpc to notify the server.
    /// Calls the flash effect on ClientIllusionView.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    private void TakeDamageClientSide(float amount)
    {
        if (isDead) return;

        // ADDED: Trigger flash via ClientIllusionView
        _clientView?.FlashRed();

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Debug.Log($"[IllusionHealth {NetworkObjectId}] Took {amount} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            isDead = true;
            // TODO: Play client-side death visual effects/animations
            // Debug.Log($"[IllusionHealth {NetworkObjectId}] Died on client. Reporting to server.");
            ReportDeathToServerRpc();
        }
    }

    /// <summary>
    /// [ServerRpc] Called by the responsible client when the illusion's health reaches zero.
    /// This RPC, once executed on the server, finds its local ServerIllusionOrchestrator component
    /// and calls its ProcessClientDeathReport method, passing along the original RPC parameters
    /// (which includes the sender's client ID for verification).
    /// Requires RequireOwnership = false because the illusion is server-owned, but the targeted client (not owner) needs to send this.
    /// </summary>
    [ServerRpc(RequireOwnership = false)] 
    private void ReportDeathToServerRpc(ServerRpcParams rpcParams = default)
    {
        // Debug.Log($"[IllusionHealth {NetworkObjectId}] ServerRPC ReportDeathToServerRpc received from client {rpcParams.Receive.SenderClientId}. Server Orchestrator is {(_serverOrchestrator == null ? "NULL" : "NOT NULL")}.");
        if (!IsServer || _serverOrchestrator == null) 
        {
            if (_serverOrchestrator == null) Debug.LogError("[IllusionHealth {NetworkObjectId}] _serverOrchestrator is null on server when ReportDeathToServerRpc was called.");
            return;
        }
        
        // Debug.Log($"[IllusionHealth {NetworkObjectId}] Attempting to call ProcessClientDeathReport on _serverOrchestrator. Is GameObject active: {_serverOrchestrator.gameObject.activeInHierarchy}, Is Orchestrator component enabled: {_serverOrchestrator.enabled}");
        _serverOrchestrator.ProcessClientDeathReport(rpcParams);
    }
} 