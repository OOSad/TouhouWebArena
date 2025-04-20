using UnityEngine;
using System.Collections;
using Unity.Netcode;

// Now requires ShockwaveVisuals instead of SpriteRenderer
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(ShockwaveVisuals))] 
public class Shockwave : NetworkBehaviour
{
    [Header("Expansion Settings")]
    [SerializeField] private float maxRadius = 5f; // The maximum radius the shockwave will reach
    [SerializeField] private float expansionDuration = 0.5f; // Time in seconds to reach max radius
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Curve controlling expansion speed

    // Components
    private CircleCollider2D circleCollider;
    private float initialRadius;
    
    private Collider2D sourceCollider = null; // Optional: To prevent hitting the fairy that spawned it
    
    // --- NEW: Reference to visuals handler ---
    private ShockwaveVisuals shockwaveVisuals;

    // State
    private float currentExpansionTime = 0f;
    private bool isExpanding = false;

    void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        initialRadius = circleCollider.radius; // Store the initial radius set in the prefab FIRST
        shockwaveVisuals = GetComponent<ShockwaveVisuals>(); // Get the visuals component
        circleCollider.isTrigger = true; // Ensure the collider is a trigger
        circleCollider.radius = 0; // Start with zero radius

        if (shockwaveVisuals == null) 
        {
            Debug.LogError("Shockwave is missing required ShockwaveVisuals component!", this);
            enabled = false; // Disable if visuals are missing
        }
    }

    // --- NEW: Public getter for initial radius used by ShockwaveVisuals ---
    public float GetInitialRadius() => initialRadius;
    // ------------------------------------------------------------------

    // Optional: Call this immediately after instantiating if you want to prevent self-collision
    public void SetSourceCollider(Collider2D source)
    {
        sourceCollider = source;
    }

    void Update()
    {
        // Expansion logic only runs on the server
        if (!IsServer || !isExpanding) return;

        currentExpansionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentExpansionTime / expansionDuration);
        float curveValue = expansionCurve.Evaluate(progress); 
        circleCollider.radius = Mathf.Lerp(0, maxRadius, curveValue); 

        // Update visuals via the dedicated component
        UpdateVisualsClientRpc(progress, circleCollider.radius);

        // Check if expansion is complete
        if (currentExpansionTime >= expansionDuration)
        {
            isExpanding = false;
            DespawnShockwave();
        }
    }

    // Server-side collision detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Only server handles collision logic

        // Add other collision logic here if needed (e.g., interacting with players, other enemies)
    }

    [ClientRpc]
    private void UpdateVisualsClientRpc(float progress, float currentRadius)
    {
        if (shockwaveVisuals != null)
        {
            shockwaveVisuals.UpdateVisuals(progress, currentRadius);
        }
    }

    private void DespawnShockwave()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // Destroy the object after despawning
        }
        else if (gameObject != null)
        {            
            Destroy(gameObject); // Fallback for non-networked or already despawned cases
        }
    }

    public override void OnNetworkSpawn()
    {        
        if (IsServer)
        {
            // Initialize expansion state on the server
            currentExpansionTime = 0f;
            circleCollider.radius = 0f;
            isExpanding = true;
        }
        // Client-side visuals are handled by ShockwaveVisuals OnNetworkSpawn/OnNetworkDespawn
    }
} 