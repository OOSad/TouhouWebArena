using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(Collider2D))]
public class ReimuExtraAttackOrb : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float initialUpwardForce = 5f;
    [SerializeField] private int damageAmount = 10; // Or however much damage it should deal

    // NetworkVariable to store which player this orb should damage
    public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } =
        new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server applies the initial force
        if (IsServer)
        {
            rb.AddForce(Vector2.up * initialUpwardForce, ForceMode2D.Impulse);
        }
    }

    // Collision detection runs on server and clients
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit an object tagged "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // Try to get the player's identity/controller script
            // Replace 'PlayerController' with the actual name of your player script
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>(); // Use PlayerMovement

            if (playerMovement != null)
            {
                // Determine the role of the player we hit
                // This assumes PlayerController has access to its role. Adjust as needed.
                PlayerRole hitPlayerRole = playerMovement.GetPlayerRole(); // Call method on PlayerMovement

                Debug.Log($"[Orb {NetworkObjectId}] Hit player object. TargetRole={TargetPlayerRole.Value}, HitPlayerRole={hitPlayerRole}");

                // If the hit player's role matches the target role for this orb...
                if (hitPlayerRole != PlayerRole.None && hitPlayerRole == TargetPlayerRole.Value)
                {
                     Debug.Log($"[Orb {NetworkObjectId}] Hit the correct target ({hitPlayerRole}). Requesting damage.");
                    // Request the server to apply damage and destroy the orb
                    RequestDamageServerRpc(playerMovement.OwnerClientId); // Pass the ClientId of the player hit
                }
            }
             else
            {
                 Debug.LogWarning($"[Orb {NetworkObjectId}] Collided with Player tagged object, but couldn't get PlayerMovement script.");
            }
        }
        // Note: Bouncing off walls tagged "Wall" (or similar) is handled by PhysicsMaterial2D
        // You might add specific logic here if needed (e.g., play sound on bounce)
    }

    [ServerRpc(RequireOwnership = false)] // Orb isn't owned by a client, so allow requests from anyone (server essentially)
    private void RequestDamageServerRpc(ulong targetClientId)
    {
        Debug.Log($"[Orb {NetworkObjectId} ServerRPC] Received damage request for ClientId {targetClientId}.");

        // Find the target player's health component on the server
        // You'll need a way to get the player's GameObject/NetworkObject from their ClientId
        // PlayerDataManager might help, or NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject
        NetworkObject targetPlayerNetworkObject = NetworkManager.Singleton.ConnectedClients[targetClientId]?.PlayerObject;

        if (targetPlayerNetworkObject != null)
        {
            // Replace 'PlayerHealth' with the actual name of your health script
            PlayerHealth playerHealth = targetPlayerNetworkObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"[Orb {NetworkObjectId} ServerRPC] Applying {damageAmount} damage to player {targetClientId}.");
                playerHealth.TakeDamage(damageAmount); // Apply damage

                // Despawn the orb after dealing damage
                Debug.Log($"[Orb {NetworkObjectId} ServerRPC] Despawning self.");
                if (NetworkObject != null && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn(true);
                }
            }
            else
            {
                 Debug.LogError($"[Orb {NetworkObjectId} ServerRPC] Could not find PlayerHealth component on target player object {targetClientId}.");
                 // Still despawn the orb even if damage failed
                 if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
            }
        }
         else
        {
             Debug.LogError($"[Orb {NetworkObjectId} ServerRPC] Could not find PlayerObject for target client ID {targetClientId}.");
             // Still despawn the orb
             if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
