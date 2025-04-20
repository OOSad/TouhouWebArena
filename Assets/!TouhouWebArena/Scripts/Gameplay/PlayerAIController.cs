using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls a Character prefab with basic AI for dodging and potentially targeting/shooting.
/// Activated via a debug key.
/// This component only performs actions if running on the Owner client.
/// </summary>
[RequireComponent(typeof(PlayerMovement))] // Dependency
[RequireComponent(typeof(PlayerShooting))] // Dependency
public class PlayerAIController : MonoBehaviour // Needs NetworkBehaviour if it sends RPCs directly, but relies on other components for now
{
    [Header("AI Settings")]
    [SerializeField] private KeyCode activationKey = KeyCode.I;
    // Make aiActive private and expose via getter
    private bool aiActive = false;
    public bool IsAIActive() => aiActive; // Public getter for PlayerMovement

    [Header("References")]
    [Tooltip("The child Hitbox object used for collision detection.")]
    [SerializeField] private Transform hitboxTransform;
    // --- Added Component References ---
    private PlayerMovement playerMovement;
    private PlayerShooting playerShooting;
    private NetworkObject networkObject; // Needed for IsOwner check
    // ------------------------------

    [Header("Dodging Parameters")]
    [Tooltip("The layers containing objects the AI should dodge.")]
    [SerializeField] private LayerMask hazardLayers;
    [Tooltip("The size of the box used to detect hazards around the hitbox.")]
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(1f, 2f);
    [Tooltip("The distance for raycasts checking for clear dodge paths.")]
    [SerializeField] private float dodgeCheckDistance = 1.5f;
    [Tooltip("Layer mask for checking clear dodge paths (e.g., Stage boundaries, other hazards)")]
    [SerializeField] private LayerMask dodgeCheckLayers;

    // --- Added Targeting Parameters ---
    [Header("Targeting Parameters")]
    [Tooltip("The layers containing objects the AI should target (e.g., Fairies, Spirits).")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("The radius around the player to search for targets.")]
    [SerializeField] private float targetDetectionRadius = 10f;
    // --------------------------------

    // Internal state
    private bool isDodging = false;
    private PlayerRole playerRole = PlayerRole.None; // Store the player's role


    private void Awake()
    {
        // Get required components
        playerMovement = GetComponent<PlayerMovement>();
        playerShooting = GetComponent<PlayerShooting>();
        networkObject = GetComponent<NetworkObject>(); // Get NetworkObject

        if (playerMovement == null) Debug.LogError("PlayerAIController: PlayerMovement component not found!", this);
        if (playerShooting == null) Debug.LogError("PlayerAIController: PlayerShooting component not found!", this);
        if (networkObject == null) Debug.LogError("PlayerAIController: NetworkObject component not found!", this);

        if (hitboxTransform == null)
        {
            hitboxTransform = transform.Find("Hitbox");
            if (hitboxTransform == null)
            {
                Debug.LogError("PlayerAIController could not find the Hitbox child transform!", this);
            }
        }

        // Role determination might depend on NetworkSpawn, let's try getting it here but be ready for it to be None initially
        // We might need to get it again in OnNetworkSpawn or Update if PlayerDataManager isn't ready in Awake
        if(playerMovement != null)
        {
            playerRole = playerMovement.GetPlayerRole();
            if(playerRole == PlayerRole.None)
            {
                 Debug.LogWarning($"PlayerAIController (Awake): Could not determine PlayerRole for {gameObject.name}. Will retry.");
            }
        }
    }

    private void Update()
    {
        // AI logic should only run on the owner's machine
        if (networkObject == null || !networkObject.IsOwner) return;

        // Role Check: Attempt to get role if not already set
        if (playerRole == PlayerRole.None && playerMovement != null)
        {
            playerRole = playerMovement.GetPlayerRole();
            if (playerRole != PlayerRole.None)
            {
                Debug.Log($"PlayerAIController (Update): Determined PlayerRole for {gameObject.name} as {playerRole}");
            }
        }

        // Handle activation input regardless of role being known yet
        HandleActivationInput();

        if (!aiActive)
        {
            // StopAIControl is called within HandleActivationInput when turning off
            return;
        }

        // --- AI Active Logic ---

        // Continuously try to shoot if AI is active
        playerShooting?.StartAIShot(); // StartAIShot checks cooldowns internally

        // Movement logic is handled in FixedUpdate for physics consistency
    }

    private void FixedUpdate()
    {
        // AI logic should only run on the owner's machine
        if (networkObject == null || !networkObject.IsOwner || !aiActive || playerRole == PlayerRole.None)
        {
            // Do nothing if not owner, AI inactive, or role unknown
            return;
        }

        isDodging = CheckAndPerformDodge();

        if (!isDodging)
        {
            AlignWithTarget(); // Call the targeting logic
        }
    }

    private void HandleActivationInput()
    {
        // Input reading is owner-only anyway, but check networkObject for safety
        if (networkObject == null || !networkObject.IsOwner) return; 

        if (Input.GetKeyDown(activationKey))
        {
            aiActive = !aiActive;
            Debug.Log($"Player AI for {gameObject.name} (Role: {playerRole}) {(aiActive ? "Activated" : "Deactivated")}");
            if (!aiActive)
            {
                StopAIControl();
            }
            else
            {
                // Optional: Reset dodge state when activating?
                isDodging = false;
            }
        }
    }

    private bool CheckAndPerformDodge()
    {
        // Basic checks already done in FixedUpdate (IsOwner, aiActive, playerRole)
        if (hitboxTransform == null) return false;

        // Calculate detection box center slightly ahead of the hitbox
        // Use hitbox position as center, extend detectionDistance forward for BoxCast later if needed
        // For OverlapBox, center it based on hitbox directly
        Vector2 boxCenter = hitboxTransform.position;
        // Adjust center slightly based on movement? For now, just use hitbox pos.

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, detectionBoxSize, 0f, hazardLayers);

        Collider2D closestHazard = null;
        float minDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            // --- Filter based on player side --- 
            float hazardX = hit.transform.position.x;
            bool onCorrectSide = (playerRole == PlayerRole.Player1 && hazardX < 0) ||
                                 (playerRole == PlayerRole.Player2 && hazardX > 0);
            if (!onCorrectSide)
            {
                 // Debug.Log($"AI ({playerRole}): Ignoring hazard {hit.name} on wrong side (X: {hazardX})");
                 continue; // Skip hazards on the opponent's side
            }
            // -----------------------------------

            // Basic check: is the hazard close enough?
            // Consider using ClosestPoint for non-point colliders
            float distanceSqr = ((Vector2)hit.transform.position - (Vector2)hitboxTransform.position).sqrMagnitude;
            // Maybe add a maximum distance check too? detectionBoxSize handles width/height.
            // float maxDetectionRadiusSqr = (detectionBoxSize.x * detectionBoxSize.x) / 4f + detectionDistance * detectionDistance; // Rough estimate
            // if (distanceSqr > maxDetectionRadiusSqr) continue;

            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestHazard = hit;
            }
        }

        if (closestHazard != null)
        {
            // Hazard detected, attempt to dodge
            // Debug.Log($"AI ({playerRole}): Hazard detected ({closestHazard.name}), attempting dodge.");
            AttemptDodge(closestHazard);
            return true; // Currently dodging
        }

        // No hazards detected that require dodging
        return false;
    }

    private void AttemptDodge(Collider2D hazard)
    {
        if (playerMovement == null) return; // Safety check

        // Simple dodge: move away horizontally from the hazard
        float relativeX = hitboxTransform.position.x - hazard.transform.position.x;
        float dodgeDirection = Mathf.Sign(relativeX);

        // If hazard is directly aligned vertically, pick a default direction based on which side has more room
        // Or just pick a consistent direction (e.g., away from center line)
        if (Mathf.Approximately(relativeX, 0f))
        {
            dodgeDirection = (playerRole == PlayerRole.Player1) ? -1f : 1f; // P1 dodges left, P2 dodges right
            // Debug.Log($"AI ({playerRole}): Hazard aligned vertically, default dodge direction: {dodgeDirection}");
        }

        // Check if the chosen direction is clear using a short raycast
        Vector2 rayOrigin = hitboxTransform.position;
        // Use dodgeCheckLayers mask provided in inspector
        RaycastHit2D hitLeft = Physics2D.Raycast(rayOrigin, Vector2.left, dodgeCheckDistance, dodgeCheckLayers);
        RaycastHit2D hitRight = Physics2D.Raycast(rayOrigin, Vector2.right, dodgeCheckDistance, dodgeCheckLayers);

        bool canDodgeLeft = hitLeft.collider == null;
        bool canDodgeRight = hitRight.collider == null;

        float finalMoveInput = 0f;

        // Determine preferred dodge direction based on hazard relative position
        bool preferRight = dodgeDirection > 0;

        if (preferRight)
        {
            if (canDodgeRight) finalMoveInput = 1f;       // Preferred direction (Right) is clear
            else if (canDodgeLeft) finalMoveInput = -1f; // Preferred (Right) blocked, try opposite (Left)
            // else: Both blocked, finalMoveInput remains 0
        }
        else // Prefer Left
        {
            if (canDodgeLeft) finalMoveInput = -1f;       // Preferred direction (Left) is clear
            else if (canDodgeRight) finalMoveInput = 1f; // Preferred (Left) blocked, try opposite (Right)
            // else: Both blocked, finalMoveInput remains 0
        }

        if (finalMoveInput != 0)
        {
            // Debug.Log($"AI ({playerRole}): Dodging {(finalMoveInput > 0 ? "Right" : "Left")}");
        }

        // Apply horizontal movement via PlayerMovement
        playerMovement.SetAIHorizontalInput(finalMoveInput);
    }

    private void AlignWithTarget()
    {
        if (playerMovement == null || hitboxTransform == null) return;

        Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(hitboxTransform.position, targetDetectionRadius, targetLayers);

        Collider2D bestTarget = null;
        float minHorizontalDistance = float.MaxValue;

        foreach (Collider2D target in potentialTargets)
        {
            // Filter for correct side
            float targetX = target.transform.position.x;
            bool onCorrectSide = (playerRole == PlayerRole.Player1 && targetX < 0) ||
                                 (playerRole == PlayerRole.Player2 && targetX > 0);
            if (!onCorrectSide) continue;

            // Find target closest horizontally
            float horizontalDistance = Mathf.Abs(targetX - hitboxTransform.position.x);
            if (horizontalDistance < minHorizontalDistance)
            {
                minHorizontalDistance = horizontalDistance;
                bestTarget = target;
            }
        }

        float moveInput = 0f;
        if (bestTarget != null)
        {
            // Move towards the best target's x position
            moveInput = Mathf.Sign(bestTarget.transform.position.x - hitboxTransform.position.x);
            // Debug.Log($"AI ({playerRole}): Aligning with {bestTarget.name} (Dir: {moveInput})");
        }
        else
        {
            // No valid target found, stop moving horizontally
            // Debug.Log($"AI ({playerRole}): No target found to align with.");
        }

        playerMovement.SetAIHorizontalInput(moveInput);
    }

    private void StopAIControl()
    {
        // Ensure the AI stops providing input to movement systems
        // PlayerShooting doesn't need explicit stop, as AI calls StartAIShot only when active
        playerMovement?.SetAIHorizontalInput(0f); // Use conditional access
        isDodging = false; // Reset state
        // Debug.Log($"AI Control Stopped for {gameObject.name}");
    }

    // Draw Gizmos for the detection box in the Scene view for easier debugging
    private void OnDrawGizmosSelected()
    {
        // Only draw gizmos if the component has been initialized properly
        if (hitboxTransform != null)
        {
            // Use different colors based on AI state for better visualization
            Color gizmoColor;
            if (!aiActive)
            {
                 gizmoColor = Color.gray; // AI Inactive
            }
            else if (isDodging)
            {
                gizmoColor = Color.red; // AI Active and Dodging
            }
            else
            {
                 gizmoColor = Color.yellow; // AI Active, not Dodging
            }
            Gizmos.color = gizmoColor;

            // Draw the OverlapBox
            Vector2 boxCenter = hitboxTransform.position; // Centered on hitbox
            Gizmos.DrawWireCube(boxCenter, detectionBoxSize);

            // Draw dodge check rays if AI is active
            if (aiActive)
            {
                Gizmos.color = Color.cyan;
                Vector2 rayOrigin = hitboxTransform.position;
                Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.left * dodgeCheckDistance);
                Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.right * dodgeCheckDistance);
            }

            // Draw targeting radius if AI is active
            if (aiActive)
            {
                Gizmos.color = Color.green; // Use green for targeting radius
                Gizmos.DrawWireSphere(hitboxTransform.position, targetDetectionRadius);
            }
        }
    }
} 