using UnityEngine;
using Unity.Netcode;
using Unity.Collections; // Required for FixedString

/// <summary>
/// Network singleton responsible for relaying spellcard execution commands from the server to all clients.
/// </summary>
public class SpellcardNetworkHandler : NetworkBehaviour
{
    public static SpellcardNetworkHandler Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Only if it's not parented to something that persists
        }
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
    /// Called by the server to command all clients to execute a specific spellcard locally.
    /// </summary>
    /// <param name="casterClientId">The ClientId of the player who cast the spellcard.</param>
    /// <param name="targetClientId">The ClientId of the player being targeted (useful for origin calculation).</param>
    /// <param name="spellcardDataResourcePath">The path within Resources/ to load the SpellcardData/Level4SpellcardData.</param>
    /// <param name="spellLevel">The level of the spellcard (2, 3, or 4).</param>
    /// <param name="sharedRandomOffset">The server-calculated offset.</param>
    /// <param name="clientRpcParams">RPC delivery parameters (should target all clients).</param>
    [ClientRpc]
    public void ExecuteSpellcardClientRpc(
        ulong casterClientId, 
        ulong targetClientId, 
        FixedString512Bytes spellcardDataResourcePath, // Using FixedString for RPC compatibility
        int spellLevel,
        Vector2 sharedRandomOffset, // ADDED: The server-calculated offset 
        ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Received ExecuteSpellcardClientRpc. Caster: {casterClientId}, Target: {targetClientId}, Level: {spellLevel}, Path: {spellcardDataResourcePath}, Offset: {sharedRandomOffset}"); // Added offset to log
        
        if (ClientSpellcardExecutor.Instance != null)
        {
            // Convert FixedString back to string for resource loading / general use
            string pathString = spellcardDataResourcePath.ToString(); 
            // Pass the shared offset to the executor
            ClientSpellcardExecutor.Instance.StartLocalSpellcardExecution(casterClientId, targetClientId, pathString, spellLevel, sharedRandomOffset);
        }
        else
        {
            Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId}] SpellcardNetworkHandler received ExecuteSpellcardClientRpc, but ClientSpellcardExecutor.Instance is null!");
        }
    }
} 