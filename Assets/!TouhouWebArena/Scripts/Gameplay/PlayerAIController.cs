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

        float relativeX = hitboxTransform.position.x - hazard.transform.position.x;
        // float dodgeDirection = Mathf.Sign(relativeX);

        // if (Mathf.Approximately(relativeX, 0f))
        // {
        //     dodgeDirection = (playerRole == PlayerRole.Player1) ? -1f : 1f; 
        // }

        // Vector2 rayOrigin = hitboxTransform.position;
        // RaycastHit2D hitLeft = Physics2D.Raycast(rayOrigin, Vector2.left, dodgeCheckDistance, dodgeCheckLayers);
        // RaycastHit2D hitRight = Physics2D.Raycast(rayOrigin, Vector2.right, dodgeCheckDistance, dodgeCheckLayers);
        // bool canDodgeLeft = hitLeft.collider == null;
        // bool canDodgeRight = hitRight.collider == null;
        // float finalMoveInput = 0f;
        // bool preferRight = dodgeDirection > 0;

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

        // if (!Mathf.Approximately(finalMoveInput, 0f))
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(finalMoveInput); // Temporarily commented out
        // }
        // else
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(0f); // Temporarily commented out
        // }
    }

    private void AlignWithTarget()
    {
        // if (clientAuthMovement == null || hitboxTransform == null) return;

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
    }

    private void StopAIControl()
    {
        // if (clientAuthMovement != null)
        // {
        //     // clientAuthMovement.SetAIHorizontalInput(0f); // Temporarily commented out
        // }
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