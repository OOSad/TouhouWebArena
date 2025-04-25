using UnityEngine;
using Unity.Netcode;
using System.Collections;
// Removed unused namespaces like TouhouWebArena.Spellcards, Behaviors etc. as spawning is handled elsewhere.

/// <summary>
/// Handles **owner client** input for shooting actions (basic shot, charge attacks, spellcards).
/// Manages local burst fire timing and reads local <see cref="SpellBarController"/> state.
/// Communicates action requests to server-side managers (<see cref="ServerAttackSpawner"/>, <see cref="SpellBarManager"/>) via RPCs.
/// Also provides an interface for AI control via <see cref="StartAIShot"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterStats))]
// Renamed class from PlayerShooting to PlayerShootingController
public class PlayerShootingController : NetworkBehaviour
{
    // Removed legacy prefab fields as CharacterStats is the source now.
    // [Header("Prefabs")] ...

    [Header("Input Settings")]
    [Tooltip("The keyboard key used to trigger shooting actions.")]
    [SerializeField] private KeyCode fireKey = KeyCode.Z;

    // --- Local State (Owner Client Only) ---
    private SpellBarController spellBarController; // Reference to the owner's spell bar UI component in the scene.
    private float nextFireTime = 0f; // Timestamp for when the next basic shot burst can start.
    private Coroutine burstCoroutine; // Reference to the active basic shot burst coroutine, prevents overlapping bursts.
    private bool isHoldingChargeKey = false; // Tracks if the fire key is currently held down by the owner client.

    // --- Component References ---
    private CharacterStats characterStats; // Cached reference to this player's CharacterStats.

    // Removed Server-Side Cache (playerSpellBars dictionary)

    /// <summary>
    /// Called when the NetworkObject spawns.
    /// Initializes component references and finds the owner's <see cref="SpellBarController"/>.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Removed server-side cache initialization

        // Owner client needs to find its specific spell bar for local charge level checks.
        if (IsOwner)
        {
            FindAndAssignSpellBar();
        }
    }

    /// <summary>
    /// Finds the SpellBarController in the scene matching this player's <see cref="NetworkObject.OwnerClientId"/>.
    /// Called only by the owning client during <see cref="OnNetworkSpawn"/>.
    /// </summary>
    private void FindAndAssignSpellBar()
    {
        if (!IsOwner) return;

        // --- Get Owner's PlayerRole --- 
        PlayerRole ownerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null) 
        {
            PlayerData? data = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
            if (data.HasValue) ownerRole = data.Value.Role;
        }
        
        if (ownerRole == PlayerRole.None)
        {
             Debug.LogError($"[PlayerShootingController] Could not determine own PlayerRole (ClientId: {OwnerClientId}). Cannot find SpellBarController.");
             return;
        }
        // ------------------------------

        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>();
        bool foundBar = false;
        foreach (SpellBarController bar in allSpellBars)
        {
            // Compare roles instead of IDs
            if (bar.TargetPlayerRole == ownerRole)
            {
                spellBarController = bar;
                foundBar = true;
                Debug.Log($"[PlayerShootingController] Found spell bar for OwnerRole: {ownerRole}");
                break;
            }
        }

        if (!foundBar)
        {
            Debug.LogError($"[PlayerShootingController] Could not find SpellBarController for OwnerRole: {ownerRole}.");
        }
    }

    /// <summary>
    /// Called once before the first frame update by Unity.
    /// Caches the required <see cref="CharacterStats"/> component.
    /// </summary>
    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
             Debug.LogError("[PlayerShootingController] CharacterStats component not found! Disabling script.", this);
             enabled = false;
        } else {
             nextFireTime = Time.time; // Initialize to allow firing immediately
        }
    }


    /// <summary>
    /// Main update loop called every frame by Unity. **Only executes logic for the owner client.**
    /// Reads input, sends charge state to server (<see cref="UpdateChargeStateServerRpc"/>),
    /// requests attacks/spellcards on key release (<see cref="RequestChargeAttackServerRpc"/>, <see cref="RequestSpellcardServerRpc"/>),
    /// and initiates the local basic shot burst sequence (<see cref="BurstFireSequence"/>) on key press.
    /// </summary>
    void Update()
    {
        // Logic only runs for the player controlling this object
        if (!IsOwner) return;

        // Safety check for required component
        if (characterStats == null) return;

        // --- Input Reading (Owner Client Only) ---
        isHoldingChargeKey = Input.GetKey(fireKey);
        bool justPressedFireKey = Input.GetKeyDown(fireKey);
        bool justReleasedFireKey = Input.GetKeyUp(fireKey);

        // --- Send Input State to Server (Owner Client Only) ---
        // Inform the SpellBarManager about the charge state for server-side calculation.
        UpdateChargeStateServerRpc(isHoldingChargeKey);

        // --- Check for Action on Release (Owner Client Only) ---
        if (justReleasedFireKey)
        {
            if (spellBarController != null)
            {
                // Read the LOCAL NetworkVariable value to decide what to request.
                // The server will perform the authoritative checks.
                float chargeLevel = spellBarController.currentActiveFill.Value;
                int spellLevel = Mathf.FloorToInt(chargeLevel);

                if (spellLevel >= 2) // Request Spellcard
                {
                    RequestSpellcardServerRpc(spellLevel);
                }
                else if (spellLevel >= 1) // Request Charge Attack
                {
                    RequestChargeAttackServerRpc();
                }
                // Level 0: Nothing to request on release; active bar reset is handled via UpdateChargeStateServerRpc(false).
            }
            else {
                 Debug.LogWarning("[PlayerShootingController] Cannot check charge level - SpellBarController reference is missing.");
            }
        }

        // --- Burst Fire Action (Owner Client Only) ---
        if (justPressedFireKey)
        {
            // Check cooldown and ensure no burst is already active
            if (Time.time >= nextFireTime && burstCoroutine == null)
            {
                // Start the local coroutine to manage burst timing and send fire requests
                burstCoroutine = StartCoroutine(BurstFireSequence());

                // Calculate cooldown until next burst can start, based on stats
                float burstDuration = characterStats.GetBurstCount() > 1 ? (characterStats.GetBurstCount() - 1) * characterStats.GetTimeBetweenBurstShots() : 0f;
                nextFireTime = Time.time + burstDuration + characterStats.GetBurstCooldown();
            }
        }
    }

    /// <summary>
    /// **(Owner Client Coroutine)** Manages the timing sequence for a basic shot burst.
    /// Calls <see cref="RequestFireServerRpc"/> repeatedly according to the <see cref="CharacterStats"/> burst settings.
    /// Runs entirely on the owning client.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator BurstFireSequence()
    {
        // Use cached stats
        for (int i = 0; i < characterStats.GetBurstCount(); i++)
        {
            // Send RPC request to server to spawn one shot pair
            RequestFireServerRpc();

            // Wait if not the last shot in the burst
            if (i < characterStats.GetBurstCount() - 1)
            {
                yield return new WaitForSeconds(characterStats.GetTimeBetweenBurstShots());
            }
        }
        burstCoroutine = null; // Mark coroutine as finished
    }

    // --- Owner Client RPC Requests to Server Managers ---

    /// <summary>
    /// **[ServerRpc]** Called by the owner client to inform the server (<see cref="SpellBarManager"/>)
    /// whether the fire key is being held down.
    /// </summary>
    /// <param name="clientIsCharging">True if the owner client is holding the fire key, false otherwise.</param>
    /// <param name="rpcParams">Standard RPC parameters, used by server to get SenderClientId.</param>
    [ServerRpc]
    private void UpdateChargeStateServerRpc(bool clientIsCharging, ServerRpcParams rpcParams = default)
    {
        if (SpellBarManager.Instance != null)
        {
            SpellBarManager.Instance.UpdatePlayerActiveCharge(rpcParams.Receive.SenderClientId, clientIsCharging);
        }
        else
        {
             Debug.LogError("[ServerRpc UpdateChargeState] SpellBarManager instance not found on server!");
        }
    }

    /// <summary>
    /// **[ServerRpc]** Called by the owner client for each pair of shots within a basic shot burst.
    /// Relays the spawning request to the <see cref="ServerAttackSpawner"/>.
    /// </summary>
    /// <param name="rpcParams">Standard RPC parameters, used by server to get SenderClientId.</param>
    [ServerRpc]
    private void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
       if (ServerAttackSpawner.Instance != null)
       {
            ServerAttackSpawner.Instance.SpawnBasicShot(rpcParams.Receive.SenderClientId);
       }
       else
       {
            Debug.LogError("[ServerRpc RequestFire] ServerAttackSpawner instance not found on server!");
       }
    }

    /// <summary>
    /// **[ServerRpc]** Called by the owner client when releasing the fire key after charging to Level 1.
    /// Relays the charge attack spawning request to the <see cref="ServerAttackSpawner"/>.
    /// </summary>
    /// <param name="rpcParams">Standard RPC parameters, used by server to get SenderClientId.</param>
    [ServerRpc]
    private void RequestChargeAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        // No client-side check here; server is implicitly trusted based on receiving this RPC.
        if (ServerAttackSpawner.Instance != null)
        {
            ServerAttackSpawner.Instance.SpawnChargeAttack(rpcParams.Receive.SenderClientId);
        }
        else
        {
            Debug.LogError("[ServerRpc RequestChargeAttack] ServerAttackSpawner instance not found on server!");
        }
    }

    /// <summary>
    /// **[ServerRpc]** Called by the owner client when releasing the fire key after charging to Level 2 or higher.
    /// Requests the <see cref="SpellBarManager"/> to check and consume the cost.
    /// If successful, requests the <see cref="ServerAttackSpawner"/> to execute the spellcard.
    /// </summary>
    /// <param name="spellLevel">The spellcard level (2, 3, or 4) determined by the client based on charge.</param>
    /// <param name="rpcParams">Standard RPC parameters, used by server to get SenderClientId.</param>
    [ServerRpc]
    private void RequestSpellcardServerRpc(int spellLevel, ServerRpcParams rpcParams = default)
    {
         ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Server-side check: Can the player afford this spell?
        if (SpellBarManager.Instance != null && SpellBarManager.Instance.ConsumeSpellCost(senderClientId, spellLevel))
        {
             // Cost was paid, now request the spellcard execution
             if (ServerAttackSpawner.Instance != null)
             {
                 ServerAttackSpawner.Instance.ExecuteSpellcard(senderClientId, spellLevel);
             }
             else
             {
                  Debug.LogError($"[ServerRpc RequestSpellcard] ServerAttackSpawner instance not found! Cost consumed but cannot execute spellcard level {spellLevel}.");
             }
        }
        else
        {
            // Cost check failed (manager missing or insufficient charge).
            // SpellBarManager logs reason if charge was insufficient.
             if(SpellBarManager.Instance == null)
             {
                 Debug.LogError($"[ServerRpc RequestSpellcard] SpellBarManager instance not found! Cannot check/consume spell cost for level {spellLevel}.");
             }
        }
    }

    // --- Removed Server-Side Helper Methods ---
    // SpawnReimuChargeAttack, SpawnMarisaChargeAttack, SpawnSingleBullet,
    // ServerExecuteSpellcardActions, ConfigureBulletBehavior etc. are now in ServerAttackSpawner.
    // ServerInitializeSpellBars, Server-side Update passive fill are now in SpellBarManager.


    // --- AI Control Interface ---
    /// <summary>
    /// Called by an AI controller (e.g., <see cref="PlayerAIController"/>) on the owner client
    /// to initiate a basic shot burst sequence, respecting standard cooldowns.
    /// </summary>
    public void StartAIShot()
    {
        // Ensure called on the correct instance
        if (!IsOwner)
        {
            Debug.LogWarning("[PlayerShootingController] StartAIShot called on non-owner instance.");
            return;
        }
        // Safety check
        if (characterStats == null) return;

        // Check cooldown and ensure no burst is already active
        if (Time.time >= nextFireTime && burstCoroutine == null)
        {
            // Start the local timing coroutine which will send RPCs
            burstCoroutine = StartCoroutine(BurstFireSequence());

            // Calculate cooldown based on stats
            float burstDuration = characterStats.GetBurstCount() > 1 ? (characterStats.GetBurstCount() - 1) * characterStats.GetTimeBetweenBurstShots() : 0f;
            nextFireTime = Time.time + burstDuration + characterStats.GetBurstCooldown();
        }
        // else: AI tried to shoot too soon or during burst, do nothing.
    }
} 