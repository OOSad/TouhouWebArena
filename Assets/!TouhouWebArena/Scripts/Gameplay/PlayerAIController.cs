using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls a Character prefab with basic AI for dodging and potentially targeting/shooting.
/// Activated via a debug key.
/// This component only performs actions if running on the Owner client.
/// </summary>
// [RequireComponent(typeof(PlayerMovement))] // Dependency - Changed
[RequireComponent(typeof(ClientAuthMovement))] // Dependency - Changed
[RequireComponent(typeof(PlayerShootingController))] // Dependency
public class PlayerAIController : NetworkBehaviour
{
    // Make aiActive private and expose via getter
    private bool aiActive = false;
    public bool IsAIActive() => aiActive; // Public getter for PlayerMovement

    // ADDED: NetworkVariable for server control
    /// <summary>[Server Controlled] NetworkVariable to enable/disable AI via debug menu.</summary>
    private NetworkVariable<bool> IsAIDebugEnabled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("References")]
    [Tooltip("The child Hitbox object used for collision detection.")]
    [SerializeField] private Transform hitboxTransform;
    // --- Added Component References ---
    // private PlayerMovement playerMovement; // Changed
    private ClientAuthMovement clientAuthMovement; // Changed
    private PlayerShootingController playerShootingController;
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
        // playerMovement = GetComponent<PlayerMovement>(); // Changed
        clientAuthMovement = GetComponent<ClientAuthMovement>(); // Changed
        playerShootingController = GetComponent<PlayerShootingController>();
        networkObject = GetComponent<NetworkObject>(); // Get NetworkObject

        if (hitboxTransform == null)
        {
            hitboxTransform = transform.Find("Hitbox");
            if (hitboxTransform == null)
            {
                // Debug.LogError("PlayerAIController: Hitbox child object not found!");
            }
        }

        // Role determination might depend on NetworkSpawn, let's try getting it here but be ready for it to be None initially
        // We might need to get it again in OnNetworkSpawn or Update if PlayerDataManager isn't ready in Awake
        // if(clientAuthMovement != null) // Temporarily commented out - ClientAuthMovement doesn't have GetPlayerRole()
        // {
        // playerRole = clientAuthMovement.GetPlayerRole(); // Temporarily commented out
        // if(playerRole == PlayerRole.None)
        // {
        // Debug.LogWarning("PlayerAIController: PlayerRole could not be determined in Awake.");
        // }
        // }
        if(clientAuthMovement != null)
        {
            playerRole = clientAuthMovement.GetPlayerRole();
            if(playerRole == PlayerRole.None)
            {
                Debug.LogWarning("PlayerAIController: PlayerRole could not be determined in Awake. Will attempt in Update.");
            }
        }
    }

    private void Update()
    {
        // AI logic should only run on the owner's machine
        if (!IsOwner) return;

        // Role Check: Attempt to get role if not already set
        // if (playerRole == PlayerRole.None && clientAuthMovement != null) // Temporarily commented out
        // {
        // playerRole = clientAuthMovement.GetPlayerRole(); // Temporarily commented out
        // if (playerRole != PlayerRole.None)
        // {
        // Debug.Log("PlayerAIController: PlayerRole determined in Update: " + playerRole);
        // }
        // }
        if (playerRole == PlayerRole.None && clientAuthMovement != null)
        {
            playerRole = clientAuthMovement.GetPlayerRole();
            if (playerRole != PlayerRole.None)
            {
                Debug.Log("PlayerAIController: PlayerRole determined in Update: " + playerRole);
            }
        }

        // ADDED: Read NetworkVariable to control internal state
        bool serverRequestedState = IsAIDebugEnabled.Value;
        if (serverRequestedState != aiActive) // State change detected
        {
            aiActive = serverRequestedState;
            if (!aiActive) // If AI was just turned off
            {
                StopAIControl();
            }
            else
            { 
                // Optional: Reset dodge state when activating?
                isDodging = false;
            }
            // Debug.Log($"Owner client: AI active state set to {aiActive} based on NetworkVariable");
        }
        // ---------------------------------------------

        if (!aiActive)
        {
            // StopAIControl is called within HandleActivationInput when turning off
            return;
        }

        // --- AI Active Logic ---

        // Continuously try to shoot if AI is active
        playerShootingController?.StartAIShot(); // StartAIShot checks cooldowns internally

        // Movement logic is handled in FixedUpdate for physics consistency
    }

    private void FixedUpdate()
    {
        // AI logic should only run on the owner's machine
        if (!IsOwner || !aiActive || playerRole == PlayerRole.None) // playerRole check might be redundant if AI doesn't move based on it for now
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

    private bool CheckAndPerformDodge()
    {
        // Basic checks already done in FixedUpdate (IsOwner, aiActive, playerRole)
        if (hitboxTransform == null) return false;

        // Calculate detection box center slightly ahead of the hitbox
        Vector2 boxCenter = hitboxTransform.position;

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, detectionBoxSize, 0f, hazardLayers);

        Collider2D closestHazard = null;
        float minDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            // --- Filter based on player side --- 
            // float hazardX = hit.transform.position.x;
            // bool onCorrectSide = (playerRole == PlayerRole.Player1 && hazardX < 0) ||
            //                      (playerRole == PlayerRole.Player2 && hazardX > 0);
            // if (!onCorrectSide) // Temporarily disable side check as playerRole is not reliably set
            // {
            //      continue; // Skip hazards on the opponent's side
            // }
            // -----------------------------------
            if (playerRole != PlayerRole.None) // Only filter if role is known
            {
                float hazardX = hit.transform.position.x;
                // Simplified bounds for hazard checking: P1 on left (<0), P2 on right (>0)
                // This assumes a central dividing line at x=0. Adjust if stage layout is different.
                bool onMySide = (playerRole == PlayerRole.Player1 && hazardX > -0.5f) || // P1 dodges things on their side (right of center for them, world X > -0.5ish) or center
                                (playerRole == PlayerRole.Player2 && hazardX < 0.5f);  // P2 dodges things on their side (left of center for them, world X < 0.5ish) or center
                                                                                        // This logic might need refinement based on actual perceived threat zones.
                                                                                        // For now, let's assume AI dodges anything in its detection box on its half of the screen or encroaching the center.

                // Let's refine: AI should primarily dodge threats on *its* side of the screen or very near the center.
                // The player bounds are P1: x = -8 to -1; P2: x = 1 to 8.
                // So P1 cares about hazards with x > -8 and P2 cares about hazards with x < 8.
                // But more importantly, P1 is on left, P2 on right.
                // A simple check for "is this hazard on my half?" might be:
                // P1: hazardX < 0. P2: hazardX > 0.
                // However, bullets often cross the center.
                // Let's assume `hazardLayers` already filters for bullets/projectiles.
                // The AI should dodge *any* detected hazard from `hazardLayers`.
                // The previous side check was probably for targeting, not dodging.
                // For dodging, if it's in the detection box and on a hazard layer, it's a threat.
                // The commented out lines were:
                // bool onCorrectSide = (playerRole == PlayerRole.Player1 && hazardX < 0) ||
                //                      (playerRole == PlayerRole.Player2 && hazardX > 0);
                // if (!onCorrectSide) continue;
                // This seems to imply dodging hazards ONLY on the opponent's side, which is wrong.
                // Let's remove that side check for dodging. AI should dodge any hazard in its box.
            }
            // -----------------------------------

            float distanceSqr = ((Vector2)hit.transform.position - (Vector2)hitboxTransform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestHazard = hit;
            }
        }

        if (closestHazard != null)
        {
            AttemptDodge(closestHazard);
            return true; // Currently dodging
        }
        return false;
    }

    private void AttemptDodge(Collider2D hazard)
    {
        // if (clientAuthMovement == null) return; // Safety check
        if (clientAuthMovement == null) return;

        float relativeX = hitboxTransform.position.x - hazard.transform.position.x;
        // float dodgeDirection = Mathf.Sign(relativeX);
        float dodgeDirection = Mathf.Sign(relativeX);

        // if (Mathf.Approximately(relativeX, 0f))
        // {
        //     dodgeDirection = (playerRole == PlayerRole.Player1) ? -1f : 1f; 
        // }
        if (Mathf.Approximately(relativeX, 0f))
        {
            // If hazard is directly on top, try to dodge towards "own" side initially
            // Player 1 (left side) dodges left, Player 2 (right side) dodges right.
            // This is a simple tie-breaker.
            dodgeDirection = (playerRole == PlayerRole.Player1) ? -1f : 1f; 
            // If role is None, default to dodging right for consistency.
            if (playerRole == PlayerRole.None) dodgeDirection = 1f;
        }

        // Vector2 rayOrigin = hitboxTransform.position;
        // RaycastHit2D hitLeft = Physics2D.Raycast(rayOrigin, Vector2.left, dodgeCheckDistance, dodgeCheckLayers);
        // RaycastHit2D hitRight = Physics2D.Raycast(rayOrigin, Vector2.right, dodgeCheckDistance, dodgeCheckLayers);
        // bool canDodgeLeft = hitLeft.collider == null;
        // bool canDodgeRight = hitRight.collider == null;
        // float finalMoveInput = 0f;
        // bool preferRight = dodgeDirection > 0;
        Vector2 rayOrigin = hitboxTransform.position;
        RaycastHit2D hitLeft = Physics2D.Raycast(rayOrigin, Vector2.left, dodgeCheckDistance, dodgeCheckLayers);
        RaycastHit2D hitRight = Physics2D.Raycast(rayOrigin, Vector2.right, dodgeCheckDistance, dodgeCheckLayers);
        bool canDodgeLeft = hitLeft.collider == null;
        bool canDodgeRight = hitRight.collider == null;
        float finalMoveInput = 0f;
        bool preferRight = dodgeDirection > 0;

        // if (preferRight)
        // {
        //     if (canDodgeRight) finalMoveInput = 1f;
        //     else if (canDodgeLeft) finalMoveInput = -1f;
        // }
        // else 
        // {
        //     if (canDodgeLeft) finalMoveInput = -1f;
        //     else if (canDodgeRight) finalMoveInput = 1f;
        // }
        if (preferRight)
        {
            if (canDodgeRight) finalMoveInput = 1f;
            else if (canDodgeLeft) finalMoveInput = -1f;
        }
        else // Prefer left or hazard is to the right
        {
            if (canDodgeLeft) finalMoveInput = -1f;
            else if (canDodgeRight) finalMoveInput = 1f;
        }

        // if (!Mathf.Approximately(finalMoveInput, 0f))
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(finalMoveInput); // Temporarily commented out
        // }
        // else
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(0f); // Temporarily commented out
        // }
        if (!Mathf.Approximately(finalMoveInput, 0f))
        {
            clientAuthMovement.SetAIHorizontalInput(finalMoveInput);
        }
        else
        {
            // No clear dodge path, or already clear. Stop trying to dodge this frame (or hold position).
            clientAuthMovement.SetAIHorizontalInput(0f);
        }
    }

    private void AlignWithTarget()
    {
        // if (clientAuthMovement == null || hitboxTransform == null) return;
        if (clientAuthMovement == null || hitboxTransform == null) return;

        Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(hitboxTransform.position, targetDetectionRadius, targetLayers);
        Collider2D bestTarget = null;
        float minHorizontalDistance = float.MaxValue;

        foreach (Collider2D target in potentialTargets)
        {
            // // Filter for correct side (temporarily disabled as playerRole not set)
            // float targetX = target.transform.position.x;
            // bool onCorrectSide = (playerRole == PlayerRole.Player1 && targetX < 0) ||
            //                      (playerRole == PlayerRole.Player2 && targetX > 0);
            // if (!onCorrectSide) continue;
            if (playerRole != PlayerRole.None)
            {
                float targetX = target.transform.position.x;
                // Player 1 (on left, bounds approx -8 to -1) targets things to their right (targetX > P1's X)
                // Player 2 (on right, bounds approx 1 to 8) targets things to their left (targetX < P2's X)
                // A simpler rule: P1 targets X > -1 (opponent's side), P2 targets X < 1 (opponent's side)
                // This assumes P1 is on the left half and P2 on the right half.
                // Let's use the original logic: P1 targets X < 0 (their side/center), P2 targets X > 0 (their side/center).
                // This means AI targets enemies on its own side of the screen.
                bool onCorrectSide = (playerRole == PlayerRole.Player1 && target.transform.position.x < 0f) ||
                                     (playerRole == PlayerRole.Player2 && target.transform.position.x > 0f);

                // If the target is an "EnemyPlayer" type, it should always be on the opponent's side.
                // For Fairies/Spirits, they can spawn on either side. The AI should prioritize those on its own side or center.

                if (!onCorrectSide) continue;
            }

            float horizontalDistance = Mathf.Abs(target.transform.position.x - hitboxTransform.position.x);
            if (horizontalDistance < minHorizontalDistance)
            {
                minHorizontalDistance = horizontalDistance;
                bestTarget = target;
            }
        }

        // if (bestTarget != null)
        // {
        //     float targetX = bestTarget.transform.position.x;
        //     float moveInput = Mathf.Sign(targetX - transform.position.x); 
        //     // clientAuthMovement.SetAIHorizontalInput(moveInput); // Temporarily commented out
        // }
        // else
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(0f); // Temporarily commented out
        // }
        if (bestTarget != null)
        {
            float targetX = bestTarget.transform.position.x;
            float currentX = hitboxTransform.position.x; // Use hitbox X for alignment reference
            float moveInput = 0f;

            // Create a small deadzone to prevent jittering when aligned
            if (Mathf.Abs(targetX - currentX) > 0.1f) 
            {
                moveInput = Mathf.Sign(targetX - currentX);
            }
            clientAuthMovement.SetAIHorizontalInput(moveInput);
        }
        else
        {
            // No target found, stop moving.
            clientAuthMovement.SetAIHorizontalInput(0f);
        }
    }

    private void StopAIControl()
    {
        // if (clientAuthMovement != null)
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(0f); // Temporarily commented out
        // }
        if (clientAuthMovement != null)
        {
            clientAuthMovement.SetAIHorizontalInput(0f);
        }
        isDodging = false;
    }

    public void SetAIEnabledServer(bool enabled)
    {
        IsAIDebugEnabled.Value = enabled;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitboxTransform == null) return;

        // Visualize detection box
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(hitboxTransform.position, detectionBoxSize);

        // Visualize dodge check rays if AI is active
        if (Application.isPlaying && aiActive)
            {
                Gizmos.color = Color.cyan;
            Gizmos.DrawLine(hitboxTransform.position, hitboxTransform.position + Vector3.left * dodgeCheckDistance);
            Gizmos.DrawLine(hitboxTransform.position, hitboxTransform.position + Vector3.right * dodgeCheckDistance);
            }

        // Visualize targeting radius
        Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hitboxTransform.position, targetDetectionRadius);
    }
} 