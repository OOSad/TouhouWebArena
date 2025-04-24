using UnityEngine;
using System.Collections;
using Unity.Netcode;
using TouhouWebArena; // Add namespace for IClearable

// Now requires ShockwaveVisuals instead of SpriteRenderer
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(ShockwaveVisuals))] 
/// <summary>
/// Represents a visual shockwave effect that expands outwards from a point.
/// Handles the expansion logic (radius over time based on curve), collider updates,
/// and triggering visual updates via <see cref="ShockwaveVisuals"/>.
/// Designed to be pooled and reused via <see cref="ResetAndStartExpansion"/>.
/// </summary>
public class Shockwave : NetworkBehaviour
{
    [Header("Expansion Settings")]
    /// <summary>The maximum radius the shockwave collider will reach.</summary>
    [SerializeField] private float maxRadius = 5f; // The maximum radius the shockwave will reach
    /// <summary>The time in seconds it takes for the shockwave to expand to its maximum radius.</summary>
    [SerializeField] private float expansionDuration = 0.5f; // Time in seconds to reach max radius
    /// <summary>An animation curve controlling the rate of expansion over the duration (time 0-1 mapped to radius 0-1).</summary>
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Curve controlling expansion speed

    // Components
    /// <summary>Cached reference to the CircleCollider2D component.</summary>
    private CircleCollider2D circleCollider;
    /// <summary>The initial radius of the collider, read from the prefab during Awake.</summary>
    private float initialRadius;
    /// <summary>Optional reference to the collider of the object that spawned the shockwave (used to prevent self-collision, currently unused).</summary>
    private Collider2D sourceCollider = null; // Optional: To prevent hitting the fairy that spawned it
    
    // --- NEW: Reference to visuals handler ---
    /// <summary>Cached reference to the ShockwaveVisuals component on the same GameObject.</summary>
    private ShockwaveVisuals shockwaveVisuals;

    // State
    /// <summary>[Server Only] Timer tracking the current time elapsed since the expansion started.</summary>
    private float currentExpansionTime = 0f;
    /// <summary>[Server Only] Flag indicating if the shockwave is currently expanding.</summary>
    private bool isExpanding = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Caches component references, sets the collider as a trigger, and initializes radius/visuals state.
    /// </summary>
    void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        initialRadius = circleCollider.radius; // Store the initial radius set in the prefab FIRST
        shockwaveVisuals = GetComponent<ShockwaveVisuals>(); // Get the visuals component
        circleCollider.isTrigger = true; // Ensure the collider is a trigger
        circleCollider.radius = 0; // Start with zero radius

        if (shockwaveVisuals == null) 
        {
            enabled = false; // Disable if visuals are missing
        }
    }

    // --- NEW: Public getter for initial radius used by ShockwaveVisuals ---
    /// <summary>
    /// Gets the initial radius of the shockwave collider as defined in the prefab.
    /// Used by <see cref="ShockwaveVisuals"/> for scaling calculations.
    /// </summary>
    /// <returns>The initial radius.</returns>
    public float GetInitialRadius() => initialRadius;
    // ------------------------------------------------------------------

    // Optional: Call this immediately after instantiating if you want to prevent self-collision
    /// <summary>
    /// [Server Only] Sets the collider of the object that spawned this shockwave.
    /// (Currently not used in collision logic, but could be used to prevent self-collision).
    /// </summary>
    /// <param name="source">The Collider2D of the spawning object.</param>
    public void SetSourceCollider(Collider2D source)
    {
        sourceCollider = source;
    }

    // --- Method to reset state when reused from pool ---
    /// <summary>
    /// [Server Only] Resets the shockwave's state and starts the expansion process.
    /// Called on initial spawn and when reused from the <see cref="NetworkObjectPool"/>.
    /// Resets expansion time, collider radius, enables expansion, and triggers initial visual state.
    /// </summary>
    public void ResetAndStartExpansion()
    {
        // Only the server should restart the logic
        if (!IsServer) return; 

        currentExpansionTime = 0f;
        circleCollider.radius = 0f; // Reset collider
        isExpanding = true;

        // Immediately update visuals to starting state
        if (shockwaveVisuals != null) 
        {
             shockwaveVisuals.ResetVisuals(); // Call the new reset method
             UpdateVisualsClientRpc(0f, 0f); // Send initial state (redundant? maybe ok)
        }
    }
    // ----------------------------------------------------

    /// <summary>
    /// [Server Only] Called every frame. Handles the expansion logic if <see cref="isExpanding"/> is true.
    /// Calculates the current radius based on time and <see cref="expansionCurve"/>.
    /// Updates the collider radius and calls <see cref="UpdateVisualsClientRpc"/>.
    /// Calls <see cref="DespawnShockwave"/> when expansion is complete.
    /// </summary>
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

    /// <summary>
    /// [Server Only] Handles trigger collision events. Checks for colliders with the IClearable interface
    /// and calls their Clear method with forceClear set to false.
    /// </summary>
    /// <param name="other">The Collider2D that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Only server handles collision logic

        // Check if the collided object implements the IClearable interface
        IClearable clearable = other.GetComponent<IClearable>(); // Check directly on the collided object
        // Alternative: Check parent if objects have hitboxes as children: other.GetComponentInParent<IClearable>();

        if (clearable != null)
        {
            // Call the interface method. Pass false for normal shockwave clear.
            clearable.Clear(false, PlayerRole.None); // Use false for forceClear
        }

        // Add other collision logic here if needed (e.g., interacting with players)
    }

    /// <summary>
    /// [ClientRpc] Sends the current expansion progress and radius to all clients.
    /// Calls <see cref="ShockwaveVisuals.UpdateVisuals"/> on the clients.
    /// </summary>
    /// <param name="progress">The normalized expansion progress (0 to 1).</param>
    /// <param name="currentRadius">The current radius of the shockwave collider.</param>
    [ClientRpc]
    private void UpdateVisualsClientRpc(float progress, float currentRadius)
    {
        if (shockwaveVisuals != null)
        {
            shockwaveVisuals.UpdateVisuals(progress, currentRadius);
        }
    }

    /// <summary>
    /// [Server Only] Handles returning the shockwave NetworkObject to the pool.
    /// Calls <see cref="NetworkObjectPool.ReturnNetworkObject"/>.
    /// Includes fallback destruction if the pool is unavailable.
    /// </summary>
    private void DespawnShockwave()
    {
        // Pool logic only runs on the server
        if (!IsServer) 
        {
            // If not the server, just destroy the local instance if it exists
            if (gameObject != null) Destroy(gameObject);
            return;
        }

        // On the server, attempt to return the object to the pool
        if (NetworkObject != null)
        {
            // NetworkObjectPool handles despawning internally if needed before pooling
            NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject); 
        }
        else if (gameObject != null)
        {   
            // Fallback: If NetworkObject is somehow null but GameObject exists (unlikely for networked obj)
            Destroy(gameObject); 
        }
    }

    public override void OnNetworkSpawn()
    {        
        // Reset state via the common method - handles both initial spawn and potential host reuse
        ResetAndStartExpansion(); 
    }
} 