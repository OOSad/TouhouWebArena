using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena.Spellcards.Behaviors; // Added for LinearMovement
// Removed unused namespaces like TouhouWebArena.Spellcards, Behaviors etc. as spawning is handled elsewhere.

/// <summary>
/// Handles **owner client** input for shooting actions (basic shot, charge attacks, spellcards).
/// Manages local burst fire timing and reads local <see cref="SpellBarController"/> state.
/// Communicates action requests to server-side managers (<see cref="ServerAttackSpawner"/>, <see cref="SpellBarManager"/>) via RPCs.
/// Also provides an interface for AI control via <see cref="StartAIShot"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(AudioSource))] // Added for player firing sound
// Renamed class from PlayerShooting to PlayerShootingController
public class PlayerShootingController : NetworkBehaviour
{
    // Removed legacy prefab fields as CharacterStats is the source now.
    // [Header("Prefabs")] ...

    [Header("Input Settings")]
    [Tooltip("The keyboard key used to trigger shooting actions.")]
    [SerializeField] private KeyCode fireKey = KeyCode.Z;
    [SerializeField] private KeyCode spellChargeKey = KeyCode.X; // New key for spell charging

    // [Header("Debug")] // New Header for Debug field - REMOVING DEBUG MARKER
    // [Tooltip("Assign a simple visible prefab here for RPC testing.")]
    // [SerializeField] private GameObject debugMarkerPrefab; // REMOVING DEBUG MARKER

    // private bool rpcTestCalled = false; // REMOVING RPC TEST CALL FLAG

    // --- Local State (Owner Client Only) ---
    private SpellBarController spellBarController; // Reference to the owner's spell bar UI component in the scene.
    private float nextBurstStartTime = 0f; // Renamed from nextFireTime // Timestamp for when the next basic shot burst can start.
    private Coroutine burstCoroutine; // Reference to the active basic shot burst coroutine, prevents overlapping bursts.
    private Coroutine continuousFireCoroutine; // For continuous fire
    private bool isHoldingFireKey = false; // Tracks if the fire key is currently held down by the owner client.
    private float fireKeyDownTime = 0f; // Time Z was pressed
    private const float TAP_THRESHOLD = 0.2f; // Max duration for a tap

    // --- Sound Related --- 
    public AudioClip playerFireSound; // Sound to play when firing
    public AudioClip playerBulletHitEnemySound; // Sound for bullet hitting an enemy
    private AudioSource audioSource; // Cached AudioSource component

    // --- Component References ---
    private CharacterStats characterStats; // Cached reference to this player's CharacterStats.
    private Transform firePoint; // Add a reference for the fire point

    // Removed Server-Side Cache (playerSpellBars dictionary)

    /// <summary>
    /// Called when the NetworkObject spawns.
    /// Initializes component references and finds the owner's <see cref="SpellBarController"/>.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Log initial state
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] OnNetworkSpawn: ENTRY. IsOwner: {IsOwner}, IsClient: {IsClient}, IsServer: {IsServer}, IsHost: {IsHost}, NetworkObject.IsSpawned: {NetworkObject.IsSpawned}, NetworkObject.OwnerClientId: {NetworkObject.OwnerClientId}");

        if (characterStats == null) // Moved from Awake to ensure it's checked after potential network init
        {
            characterStats = GetComponent<CharacterStats>();
             if (characterStats == null) {
                Debug.LogError($"[{this.GetType().Name} on {this.gameObject.name}] CharacterStats component not found! Disabling script.");
                enabled = false;
                return;
             }
        }
        if (firePoint == null) // Also moved from Awake
        {
            firePoint = transform;
        }
        // nextBurstStartTime = Time.time; // Already in Awake, probably fine there.


        // Owner client needs to find its specific spell bar for local charge level checks.
        if (IsOwner)
        {
            FindAndAssignSpellBar(); // Assuming this doesn't involve RPCs itself
            //Debug.Log($"[Client {OwnerClientId} GO:{this.gameObject.name}] OnNetworkSpawn: IsOwner is TRUE. Attempting to call TestClientRpc.");
            //TestClientRpc("Hello from OnNetworkSpawn by " + OwnerClientId.ToString()); // MOVED TO UPDATE FOR TESTING
        }
        else
        {
            Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] OnNetworkSpawn: IsOwner is FALSE.");
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

        SpellBarController[] allSpellBars = FindObjectsByType<SpellBarController>(FindObjectsSortMode.None);
        bool foundBar = false;
        foreach (SpellBarController bar in allSpellBars)
        {
            // Compare roles instead of IDs
            if (bar.TargetPlayerRole == ownerRole)
            {
                spellBarController = bar;
                foundBar = true;
                // Debug.Log($"[PlayerShootingController] Found spell bar for OwnerRole: {ownerRole}");
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
             return; 
        } else {
             nextBurstStartTime = Time.time; // Initialize to allow firing immediately
        }

        // Shots will now originate from the player's transform center.
        firePoint = transform; 

        // Get and configure AudioSource for firing sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
        else
        {
            Debug.LogError("[PlayerShootingController] AudioSource component not found despite RequireComponent attribute!");
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
        // --- RPC Test Logic (Call every frame from owner for testing) ---
        // if (IsOwner) 
        // {
        //     if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
        //     {
        //         // Potentially add NetworkManager status logs here as discussed before if still debugging general connectivity
        //         // Debug.Log($"[NetworkManager Check GO:{this.gameObject.name}] IsListening: {NetworkManager.Singleton.IsListening}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}, IsHost: {NetworkManager.Singleton.IsHost}, IsServer: {NetworkManager.Singleton.IsServer}");

        //         Debug.Log($"[Client {OwnerClientId} GO:{this.gameObject.name}] Update: IsOwner is TRUE. Attempting to call TestServerRpc every frame.");
        //         TestServerRpc("Hello from Update by GO: " + this.gameObject.name + " (Actual Owner: " + OwnerClientId.ToString() + ", Frame: " + Time.frameCount + ")");
        //     }
        // }
        // --- End RPC Test Logic ---

        // Main game logic - ensure IsOwner check is still effective for actual game actions
        if (!IsOwner) return;

        // Safety check for required component
        if (characterStats == null) return;

        // --- Z Key: Firing Logic ---
        bool justPressedFireKey = Input.GetKeyDown(fireKey);
        bool isHoldingFireKeyNow = Input.GetKey(fireKey); // Current state of Z key
        bool justReleasedFireKey = Input.GetKeyUp(fireKey);

        if (justPressedFireKey)
        {
            fireKeyDownTime = Time.time;
            isHoldingFireKey = true; // Track that we started holding
        }

        if (isHoldingFireKeyNow)
        {
            // If Z is held beyond the tap threshold, and continuous fire isn't already running,
            // and a burst isn't active, start continuous fire.
            if (Time.time - fireKeyDownTime > TAP_THRESHOLD)
            {
                if (continuousFireCoroutine == null && burstCoroutine == null)
                {
                    continuousFireCoroutine = StartCoroutine(ContinuousFireSequence());
                }
            }
        }
        
        if (justReleasedFireKey)
        {
            isHoldingFireKey = false; // No longer holding

            if (continuousFireCoroutine != null)
            {
                StopCoroutine(continuousFireCoroutine);
                continuousFireCoroutine = null;
            }
            else // If continuous fire didn't start, it might have been a tap
            {
                // Check if it was a tap (duration less than threshold)
                // And if burst cooldown has passed and no burst is currently running.
                if (Time.time - fireKeyDownTime <= TAP_THRESHOLD)
                {
                    if (Time.time >= nextBurstStartTime && burstCoroutine == null)
                    {                        
                        burstCoroutine = StartCoroutine(BurstFireSequence());
                        // Calculate cooldown until next burst can start
                        float burstDuration = characterStats.GetBurstCount() > 1 ? (characterStats.GetBurstCount() - 1) * characterStats.GetTimeBetweenBurstShots() : 0f;
                        nextBurstStartTime = Time.time + burstDuration + characterStats.GetBurstCooldown();
                    }
                }
            }
        }

        // --- X Key: Spell Charging and Activation Logic ---
        bool isHoldingSpellKey = Input.GetKey(spellChargeKey);
        bool justReleasedSpellKey = Input.GetKeyUp(spellChargeKey);

        // Inform the SpellBarManager about the spell charge key state.
        UpdateChargeStateServerRpc(isHoldingSpellKey); // Pass the X key's state

        if (justReleasedSpellKey)
        {
            if (spellBarController != null)
            {
                float chargeLevel = spellBarController.currentActiveFill.Value;
                int spellLevel = Mathf.FloorToInt(chargeLevel);

                if (spellLevel >= 2)
                {
                    RequestSpellcardServerRpc(spellLevel);
                }
                else if (spellLevel >= 1)
                {
                    RequestChargeAttackServerRpc();
                }
            }
            else
            {
                 Debug.LogWarning("[PlayerShootingController] Cannot check charge level - SpellBarController reference is missing.");
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
        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError("[PlayerShootingController] ClientGameObjectPool.Instance is null. Cannot fire.");
            burstCoroutine = null; // Mark coroutine as finished to prevent locking up
            yield break;
        }

        string prefabId = characterStats.GetBasicShotPrefabID();
        float shotSpeed = characterStats.GetBasicShotSpeed();
        float shotLifetime = characterStats.GetBasicShotLifetime();
        float spread = characterStats.GetBulletSpread();

        // Spread will now be relative to the player's transform.right
        Vector3 rightOffset = firePoint.right * (spread / 2f); // firePoint is now 'this.transform'
        Vector3 leftOffset = -rightOffset;

        for (int i = 0; i < characterStats.GetBurstCount(); i++)
        {
            // Play firing sound
            if (audioSource != null && playerFireSound != null && IsOwner) // Only owner plays their own firing sound locally
            {
                audioSource.PlayOneShot(playerFireSound);
            }

            // Spawn Left Bullet of the Pair
            SpawnAndNetworkBullet(prefabId, firePoint.position + leftOffset, firePoint.rotation, shotSpeed, shotLifetime);

            // Spawn Right Bullet of the Pair
            SpawnAndNetworkBullet(prefabId, firePoint.position + rightOffset, firePoint.rotation, shotSpeed, shotLifetime);

            if (characterStats.GetBurstCount() > 1 && i < characterStats.GetBurstCount() - 1)
            {
                yield return new WaitForSeconds(characterStats.GetTimeBetweenBurstShots());
            }
        }
        burstCoroutine = null; // Mark coroutine as finished
    }

    private IEnumerator ContinuousFireSequence()
    {
        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError("[PlayerShootingController] ClientGameObjectPool.Instance is null. Cannot continuously fire.");
            continuousFireCoroutine = null;
            yield break;
        }

        string prefabId = characterStats.GetBasicShotPrefabID();
        float shotSpeed = characterStats.GetBasicShotSpeed();
        float shotLifetime = characterStats.GetBasicShotLifetime();
        float spread = characterStats.GetBulletSpread();
        float timeBetweenShots = characterStats.GetTimeBetweenBurstShots(); // Reuse this for continuous fire rate

        Vector3 rightOffset = firePoint.right * (spread / 2f);
        Vector3 leftOffset = -rightOffset;

        float nextShotPairTime = Time.time; // Initialize to allow first shot immediately

        // isHoldingFireKey will be updated by Update() method.
        // The loop continues as long as this coroutine is not stopped externally (by releasing Z).
        while (true) 
        {
            if (Time.time >= nextShotPairTime)
            {
                // Play firing sound
                if (audioSource != null && playerFireSound != null && IsOwner) // Only owner plays their own firing sound locally
                {
                    audioSource.PlayOneShot(playerFireSound);
                }

                SpawnAndNetworkBullet(prefabId, firePoint.position + leftOffset, firePoint.rotation, shotSpeed, shotLifetime);
                SpawnAndNetworkBullet(prefabId, firePoint.position + rightOffset, firePoint.rotation, shotSpeed, shotLifetime);
                nextShotPairTime = Time.time + timeBetweenShots;
            }
            yield return null; // Check each frame
        }
        // Note: The external stop of this coroutine will set continuousFireCoroutine = null;
    }

    private void SpawnAndNetworkBullet(string prefabId, Vector3 position, Quaternion rotation, float speed, float lifetime)
    {
        // Debug.Log($"[Firing Client {OwnerClientId}] SpawnAndNetworkBullet: Spawning local {prefabId} at {position}");
        GameObject bulletInstance = ClientGameObjectPool.Instance.GetObject(prefabId);

        if (bulletInstance != null)
        {
            bulletInstance.transform.position = position;
            bulletInstance.transform.rotation = rotation;
            bulletInstance.SetActive(true);

            BulletMovement mover = bulletInstance.GetComponent<BulletMovement>();
            if (mover != null)
            {
                mover.Initialize(OwnerClientId, speed, lifetime, this); // Correctly initialize for owner, passing this controller
            }
            else
            {
                Debug.LogError($"[PlayerShootingController] Locally spawned bullet {prefabId} is missing BulletMovement script!");
            }

            // Send RPC to server to inform it about the shot
            // Debug.Log($"[Firing Client {OwnerClientId}] SpawnAndNetworkBullet: Sending FireShotServerRpc for {prefabId}");
            FireShotServerRpc(prefabId, position, rotation, speed, lifetime);
        }
        else
        {
            Debug.LogWarning($"[PlayerShootingController] Failed to get bullet {prefabId} from pool for local spawn.");
        }
    }

    [ServerRpc]
    private void FireShotServerRpc(string prefabId, Vector3 spawnPosition, Quaternion spawnRotation, float speed, float lifetime, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // Server log for receiving the shot request
        // Debug.Log($"[SERVER GO:{this.gameObject.name}] FireShotServerRpc: RECEIVED from Client {rpcParams.Receive.SenderClientId} for {prefabId}. Forwarding to FireShotClientRpc.");

        // Prepare ClientRpcParams to send to all clients EXCEPT the one who sent this ServerRpc,
        // as that client already spawned their own shot locally.
        FireShotClientRpc(prefabId, spawnPosition, spawnRotation, speed, lifetime);
    }

    [ClientRpc]
    private void FireShotClientRpc(string prefabId, Vector3 spawnPosition, Quaternion spawnRotation, float speed, float lifetime, ClientRpcParams clientRpcParams = default)
    {
        // Log Entry with more details, including who is executing this RPC.
        // Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] FireShotClientRpc: ENTRY. IsOwner: {IsOwner}, PrefabID: {prefabId}, Pos: {spawnPosition}");

        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] FireShotClientRpc: ClientGameObjectPool.Instance is null. Cannot spawn bullet {prefabId}.");
            return;
        }

        if (IsOwner)
        {
            // Debug.Log($"[Client {OwnerClientId} GO:{this.gameObject.name}] FireShotClientRpc: Owner instance, returning, as bullet was already spawned locally in SpawnAndNetworkBullet.");
            // Owner already spawned the bullet locally in SpawnAndNetworkBullet.
            // This RPC is primarily for non-owners to spawn their visual representation.
            // However, even the owner needs to run this if the local spawn was just a placeholder
            // and the definitive version with correct network ID comes from the server.
            // For now, assuming owner handles its own definitive spawn.
            return; // Owner already handled its shot locally in SpawnAndNetworkBullet
        }

        // Non-owner clients spawn the bullet visuals.
        // Debug.Log($"[REMOTE Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] FireShotClientRpc: PROCESSING for {prefabId}. Attempting to spawn visual only.");

        GameObject bulletInstance = ClientGameObjectPool.Instance.GetObject(prefabId);
        if (bulletInstance != null)
        {
            bulletInstance.transform.position = spawnPosition;
            bulletInstance.transform.rotation = spawnRotation;
            bulletInstance.SetActive(true);

            BulletMovement mover = bulletInstance.GetComponent<BulletMovement>();
            if (mover != null)
            {
                // Pass the OwnerClientId of this PlayerShootingController (the firer)
                // For remote bullets, the shootingController reference is null as they don't play hit sounds locally for their owner.
                mover.Initialize(this.OwnerClientId, speed, lifetime, null); 
            }
            else
            {
                Debug.LogError($"[REMOTE Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] Bullet {prefabId} is missing BulletMovement script after being pooled!");
            }
            // Debug.Log($"[Remote Client {NetworkManager.Singleton.LocalClientId}] FireShotClientRpc: Successfully got {prefabId} from pool. Activating and initializing.");
        }
        else
        {
            Debug.LogWarning($"[REMOTE Client {NetworkManager.Singleton.LocalClientId} GO:{this.gameObject.name}] FireShotClientRpc: Failed to get bullet {prefabId} from pool.");
        }
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
    /// **[ServerRpc(RequireOwnership = false)]** Called by the owner client when releasing the fire key after charging to Level 2 or higher.
    /// Requests the <see cref="SpellBarManager"/> to check and consume the cost.
    /// If successful, requests the <see cref="ServerAttackSpawner"/> to execute the spellcard.
    /// </summary>
    /// <param name="spellLevel">The spellcard level (2, 3, or 4) determined by the client based on charge.</param>
    /// <param name="rpcParams">Standard RPC parameters, used by server to get SenderClientId.</param>
    [ServerRpc(RequireOwnership = false)] // Allow calls from non-owners (server host)
    public void RequestSpellcardServerRpc(int spellLevel, ServerRpcParams rpcParams = default)
    {
         ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Server-side check: Can the player afford this spell?
        if (SpellBarManager.Instance != null && SpellBarManager.Instance.ConsumeSpellCost(senderClientId, spellLevel))
        {
             // Cost was paid.
             // Trigger the clearing effect around the caster FIRST.
             if (ServerAttackSpawner.Instance != null)
             {
                 ServerAttackSpawner.Instance.TriggerSpellcardClear(senderClientId, spellLevel);

                 // Now request the spellcard execution (illusion/bullets)
                 ServerAttackSpawner.Instance.ExecuteSpellcard(senderClientId, spellLevel);
             }
             else
             {
                  Debug.LogError($"[ServerRpc RequestSpellcard] ServerAttackSpawner instance not found! Cost consumed but cannot execute spellcard level {spellLevel} or clear effect.");
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
        if (Time.time >= nextBurstStartTime && burstCoroutine == null)
        {
            // Start the local timing coroutine which will send RPCs
            burstCoroutine = StartCoroutine(BurstFireSequence());

            // Calculate cooldown based on stats
            float burstDuration = characterStats.GetBurstCount() > 1 ? (characterStats.GetBurstCount() - 1) * characterStats.GetTimeBetweenBurstShots() : 0f;
            nextBurstStartTime = Time.time + burstDuration + characterStats.GetBurstCooldown();
        }
        // else: AI tried to shoot too soon or during burst, do nothing.
    }

    // Method for bullets to call to play their hit sound
    public void PlayBulletHitEnemySound()
    {
        if (audioSource != null && playerBulletHitEnemySound != null)
        {
            audioSource.PlayOneShot(playerBulletHitEnemySound);
        }
    }
} 