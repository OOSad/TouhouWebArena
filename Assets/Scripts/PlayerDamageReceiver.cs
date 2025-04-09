using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DanmakU; // Need this for DanmakuCollisionList
using System; // Needed for Action

// Detects collisions via DanmakU system and tells PlayerHealth to request damage.
[RequireComponent(typeof(Collider2D))] // Still likely needed for DanmakuCollider internal checks
[RequireComponent(typeof(Rigidbody2D))] // Still likely needed for physics system registration
[RequireComponent(typeof(DanmakuCollider))] // Need the DanmakU collider component
public class PlayerDamageReceiver : MonoBehaviour
{
    // Tag check might still be useful if non-damaging DanmakU objects exist
    // private const string BulletTag = "Bullet"; 

    private PlayerHealth _playerHealth;
    private NetworkObject _networkObject; // To check ownership
    private DanmakuCollider _danmakuCollider;

    void Awake()
    {
        // Find components on the parent player object
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _networkObject = GetComponentInParent<NetworkObject>();
        // Get the DanmakuCollider on this specific GameObject
        _danmakuCollider = GetComponent<DanmakuCollider>();

        if (_playerHealth == null)
        {
            Debug.LogError("PlayerDamageReceiver could not find PlayerHealth component on parent!", this);
            enabled = false;
        }
        if (_networkObject == null)
        {
            Debug.LogError("PlayerDamageReceiver could not find NetworkObject component on parent!", this);
            enabled = false;
        }
        if (_danmakuCollider == null)
        {
            Debug.LogError("PlayerDamageReceiver requires a DanmakuCollider component on the same GameObject!", this);
            enabled = false;
        }

        // Check for Rigidbody2D kinematic setting (optional check)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
             Debug.LogWarning($"Rigidbody2D on {gameObject.name} should ideally be Kinematic for PlayerDamageReceiver.", this);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the DanmakU collision event
        if (_danmakuCollider != null)
        {
            _danmakuCollider.OnDanmakuCollision += HandleDanmakuCollision;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors when disabled/destroyed
        if (_danmakuCollider != null)
        {
            _danmakuCollider.OnDanmakuCollision -= HandleDanmakuCollision;
        }
    }

    // Remove the standard Unity trigger method
    // private void OnTriggerEnter2D(Collider2D other) { ... }

    // This method is called by the DanmakuCollider event
    private void HandleDanmakuCollision(DanmakuCollisionList collisions)
    {
        // Only the owner client should process hits and request damage
        if (_networkObject == null || !_networkObject.IsOwner)
        {
            return;
        }

        // If we received any collision in this batch, request damage once.
        // We don't need to loop through the list unless different bullets
        // needed different handling.
        if (collisions.Count > 0)
        {
            // Optional: Could inspect collisions[0].Danmaku or collisions[0].RaycastHit here
            // if needed (e.g., check bullet type/tag if DanmakU doesn't filter)
            // Danmaku danmaku = collisions[0].Danmaku;

            Debug.Log($"Owner client received Danmaku collision ({collisions.Count} bullets in batch). Requesting damage.");

            // Tell the server we took a hit
            _playerHealth?.RequestTakeDamageServerRpc();
        }
    }

    // Remove unused Start/Update methods
    // void Start() { ... }
    // void Update() { ... }
}
