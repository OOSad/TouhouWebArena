using UnityEngine;
using Unity.Netcode;

public class SpiritController : NetworkBehaviour, IClearableByBomb
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CircleCollider2D bodyCollider; // Or other collider type
    [SerializeField] private GameObject spiritPrefabRef; // Assign Spirit prefab itself here
    // Potential reference to a bullet clearing component
    // [SerializeField] private BulletClearer bulletClearer;

    [Header("Visuals (Assign Children)")]
    [SerializeField]
    [Tooltip("The child GameObject holding visuals for the normal state.")]
    private GameObject normalVisualObject;
    [SerializeField]
    [Tooltip("The child GameObject holding visuals for the activated state.")]
    private GameObject activatedVisualObject;

    [Header("Movement")]
    [SerializeField] private float normalMoveSpeed = 2f;
    [SerializeField] private float activatedMoveSpeed = 1f;
    private bool aimAtPlayerOnSpawn = false; // Set by spawner (only needed on server)

    [Header("Health")]
    [SerializeField] private int normalMaxHp = 5;
    [SerializeField] private int activatedMaxHp = 1;

    // --- Networked State ---
    private NetworkVariable<int> currentHp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isActivated = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // -----------------------

    [Header("Death Effects")]
    [SerializeField] private GameObject normalDeathEffectPrefab;
    [SerializeField] private GameObject activatedDeathEffectPrefab;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 15f; // Time in seconds before auto-despawn

    [Header("Timeout Behavior (Server Only)")]
    [SerializeField] private GameObject spiritLargeBulletPrefab; // Prefab to spawn on timeout (should have StageSmallBulletMoverScript)
    [SerializeField] private float activatedTimeoutDuration = 3.0f;
    [SerializeField] private float bulletSpreadAngle = 15f; // Angle for side bullets

    [Header("Revenge Spawn (Server Only)")]
    [SerializeField] private int maxSpiritsPerSide = 10; // Max spirits allowed per side
    [SerializeField] private Vector2 revengeSpawnZoneSize = new Vector2(7f, 1f); // How large is the spawn zone

    // --- Server-Side State ---
    private Transform playerTransform; // Set by spawner (only needed on server)
    private PlayerRole ownerRole = PlayerRole.None; // Which side this spirit belongs to
    private bool isDying = false; // Server-side flag to prevent multiple deaths
    private float currentLifetime; // Server-side timer
    private float activatedTimer = 0f; // Server-side timer for timeout
    private Transform player1SpawnZoneRef; // Passed in Initialize
    private Transform player2SpawnZoneRef; // Passed in Initialize
    // ------------------------

    // --- NEW: Public Getter for Owner Role ---
    public PlayerRole GetOwnerRole() 
    {
        return ownerRole;
    }
    // ----------------------------------------

    #region Initialization and Spawning

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Comment out rotation log
        // Debug.Log($"[Spirit {NetworkObjectId}] OnNetworkSpawn. IsServer: {IsServer}, IsClient: {IsClient}, Rotation Euler: {transform.rotation.eulerAngles}");

        // Subscribe to state changes to update visuals
        isActivated.OnValueChanged += OnActivationStateChanged;

        // Immediately update visuals on spawn based on the synchronized initial state
        UpdateVisuals(isActivated.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe to prevent errors
        isActivated.OnValueChanged -= OnActivationStateChanged;

        // --- Deregister from Registry (Server Only) --- 
        // This is a safety net; primary deregistration happens before despawn calls
        if (IsServer && SpiritRegistry.Instance != null)
        {
            SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // -------------------------------------------

        base.OnNetworkDespawn();
    }

    // Called by the spawner ONLY ON SERVER after instantiation but before Spawn()
    public void Initialize(Transform targetPlayer, PlayerRole owner, bool shouldAim, 
                         Transform p1Zone, Transform p2Zone) // Added spawn zone refs
    {
        if (!IsServer) return; // Should only be called on Server

        // Comment out rotation log
        // Debug.Log($"[Server Spirit {NetworkObjectId}] Initialize START. Current Rotation Euler: {transform.rotation.eulerAngles}"); 

        playerTransform = targetPlayer; // Can be null if not aiming
        ownerRole = owner; // Store the owner role
        aimAtPlayerOnSpawn = shouldAim;
        player1SpawnZoneRef = p1Zone;
        player2SpawnZoneRef = p2Zone;

        // Validate essential references passed in
        if (ownerRole == PlayerRole.None)
        {
            Debug.LogError($"[Server Spirit {NetworkObjectId}] Initialized with PlayerRole.None!", this);
        }
        if (player1SpawnZoneRef == null || player2SpawnZoneRef == null)
        {
             Debug.LogError($"[Server Spirit {NetworkObjectId}] Initialized with null spawn zone references!", this);
        }
        if (spiritPrefabRef == null) // Check prefab needed for revenge spawn
        {
             Debug.LogError($"[Server Spirit {NetworkObjectId}] spiritPrefabRef is not assigned in the inspector! Revenge spawn will fail.", this);
        }

        // Initial setup based on state (server-side)
        currentHp.Value = normalMaxHp;
        isActivated.Value = false; // Explicitly set initial state
        activatedTimer = 0f; // Reset timer on init

        // Set initial velocity (server-side)
        SetInitialVelocity();

        // Initialize lifetime timer on server
        currentLifetime = maxLifetime;

        // --- Register with Registry (Server Only) --- 
        if (IsServer)
        {
            if (SpiritRegistry.Instance != null)
            {
                SpiritRegistry.Instance.Register(this, ownerRole);
            }
            else
            {
                 Debug.LogError($"[Server Spirit {NetworkObjectId}] SpiritRegistry.Instance is null during Initialize!");
            }
        }
        // ------------------------------------------
    }

    #endregion

    #region State Management and Visuals

    // Server-only method to change the state
    private void ServerSetActivationState(bool activate)
    {
        if (!IsServer) return;
        if (isActivated.Value == activate) return; // Already in this state

        isActivated.Value = activate;

        if (activate)
        {
            // Update HP, capping at new max (server-side)
            currentHp.Value = Mathf.Min(currentHp.Value, activatedMaxHp);
            rb.velocity = Vector2.up * activatedMoveSpeed; // Move upwards (server-side)
            activatedTimer = 0f; // Reset timer when activated
        }
        else
        {
            // This case currently shouldn't happen as we don't deactivate
            currentHp.Value = normalMaxHp; // Reset HP?
            SetInitialVelocity(); // Reset velocity (server-side)
        }
    }

    // Callback function when isActivated changes (runs on server and clients)
    private void OnActivationStateChanged(bool previousValue, bool newValue)
    {
        UpdateVisuals(newValue);
    }

    // Updates local visuals based on the activation state
    private void UpdateVisuals(bool activated)
    {
        if (normalVisualObject == null || activatedVisualObject == null)
        {
            Debug.LogError($"Spirit {NetworkObjectId}: Visual objects not assigned! Cannot update visuals.", this);
            return;
        }

        // Activate/Deactivate based on state
        normalVisualObject.SetActive(!activated); 
        activatedVisualObject.SetActive(activated);
    }

    // Server-only method to set initial velocity
    private void SetInitialVelocity()
    {
        if (!IsServer) return;

        if (aimAtPlayerOnSpawn && playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.velocity = direction * normalMoveSpeed;
        }
        else
        {
            rb.velocity = Vector2.down * normalMoveSpeed;
        }
    }

    #endregion

    #region Interaction and Damage

    // Server-side check for triggers
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Check if entering a ScopeStyle zone
        if (!isActivated.Value && other.CompareTag("ScopeStyleZone"))
        {
            ServerSetActivationState(true); // Activate the spirit (server changes NetworkVariable)
        }

        // Check collision with player bullets
        if (other.CompareTag("PlayerShot")) // Changed from PlayerBullet to PlayerShot
        {
            // Try to get the damage value from the ProjectileDamager script
            int damageAmount = 1; // Default damage if script not found or has no damage value
            ProjectileDamager damager = other.GetComponent<ProjectileDamager>(); // Get the new damager component
            if (damager != null)
            {
                damageAmount = damager.damage; // Use the damage value from the component
            }

            // --- Get Killer Role from Bullet --- 
            PlayerRole killer = PlayerRole.None;
            BulletMovement bullet = other.GetComponent<BulletMovement>();
            if (bullet != null)
            {
                killer = bullet.OwnerRole.Value;
            }
            else
            {
                Debug.LogWarning($"[Server Spirit {NetworkObjectId}] Hit by PlayerShot without BulletMovement component! Cannot determine killer.", other.gameObject);
            }
            // -----------------------------------

            // Apply damage using the public TakeDamage method
            TakeDamage(damageAmount, killer); 

            // NOTE: The bullet should handle its own destruction when it hits something.
        }
        // --- Check collision with Player --- 
        else if (other.CompareTag("Player"))
        {
            // Find the PlayerHealth component (might be on parent)
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>(); 
            if (playerHealth != null)
            {
                 // Check if player is vulnerable
                if (!playerHealth.IsInvincible.Value)
                {
                    playerHealth.TakeDamage(1); // Deal 1 damage
                    // Note: Spirit does not die upon colliding with player
                }
            }
            else
            {
                 Debug.LogError($"[Server Spirit {NetworkObjectId}] Collided with Player ({other.name}) but PlayerHealth component not found in parent!");
            }
        }
        // ---------------------------------
    }

    // --- MODIFIED: Renamed from ApplyDamageInternal and made public ---
    public void TakeDamage(int amount, PlayerRole killerRole)
    {
        // Basic server check + check if already dying
        if (!IsServer || isDying || currentHp.Value <= 0) return;

        currentHp.Value -= amount;
        if (currentHp.Value <= 0)
        {
            Die(killerRole); // Pass killerRole to Die method
        }
    }
    // -----------------------------------------------------------------

    // Server-side method to handle death logic
    private void Die(PlayerRole killerRole = PlayerRole.None) // Added killerRole parameter
    {
        if (!IsServer || isDying) return; // Should only run once on server
        isDying = true;

        // --- Deregister Before Despawn --- 
        if (SpiritRegistry.Instance != null)
        {
             SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // ------------------------------- 

        GameObject effectPrefab = null;

        // Use the current NETWORKED activation state
        if (isActivated.Value)
        {
             Debug.Log($"Activated Spirit {NetworkObjectId} died. Spawning activated effect.");
             effectPrefab = activatedDeathEffectPrefab;
        }
        else
        {
             Debug.Log($"Normal Spirit {NetworkObjectId} died. Spawning normal effect.");
             effectPrefab = normalDeathEffectPrefab;
        }

        // --- Spawn Death Effect --- (Existing Logic)
        if (effectPrefab != null)
        {
            // ... (instantiate, spawn network object)
            GameObject effectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            NetworkObject effectNetworkObject = effectInstance.GetComponent<NetworkObject>();
            if (effectNetworkObject != null)
            {
                effectNetworkObject.Spawn(true); 
            }
            else
            {
                Debug.LogError($"Death effect prefab '{effectPrefab.name}' is missing a NetworkObject component!", effectPrefab);
                Destroy(effectInstance); 
            }
        }
        else { Debug.LogWarning($"Spirit {NetworkObjectId} died but corresponding death effect prefab is not assigned."); }
        // -------------------------

        // --- Spawn Spirit on Opponent's Side --- 
        Debug.Log($"[Server Spirit {NetworkObjectId}] Die called. Killer: {killerRole}, Owner: {ownerRole}");
        // If killed by a player (not None), send a spirit to the opponent.
        if (killerRole != PlayerRole.None)
        {
            // We no longer check if killer == owner, as players only kill spirits on their own side.
            Debug.Log($"[Server Spirit {NetworkObjectId}] Conditions met for opponent spawn. Calling SpawnRevengeSpirit...");
            SpawnRevengeSpirit(killerRole); // This method calculates the opponent's side
        }
        else { Debug.Log($"[Server Spirit {NetworkObjectId}] No opponent spawn: Killer was None."); }
        // -------------------------------------

        // Despawn the spirit object itself on the network
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        { 
            networkObject.Despawn(true);
        }
    }

    #endregion

    #region IClearableByBomb Implementation

    /// <summary>
    /// Called when the player's death bomb effect should clear this spirit.
    /// </summary>
    /// <param name="bombingPlayer">The role of the player who activated the bomb.</param>
    public void ClearByBomb(PlayerRole bombingPlayer)
    {
        // Since PlayerDeathBomb runs on server, this is called on the server instance.
        // We can directly perform the server-side checks and destroy.
        if (!IsServer || isDying || currentHp.Value <= 0) 
        {
             Debug.LogWarning($"[Server Spirit {NetworkObjectId}] ClearByBomb called, but ignoring. IsServer={IsServer}, isDying={isDying}, HP={currentHp.Value}");
            return;
        }

        // --- Call Die instead of directly despawning --- 
        // The Die method now handles deregistering, effects, opponent spawn, and despawn.
        Debug.Log($"[Server Spirit {NetworkObjectId}] Clearing self via Die() due to bomb from {bombingPlayer}.");
        Die(bombingPlayer); // Pass the bomber as the killer
        // ------------------------------------------------

        // REMOVED direct deregister and despawn logic from here:
        // isDying = true; 
        // if (SpiritRegistry.Instance != null) { SpiritRegistry.Instance.Deregister(this, ownerRole); }
        // NetworkObject networkObject = GetComponent<NetworkObject>();
        // if (networkObject != null && networkObject.IsSpawned) { networkObject.Despawn(true); }
    }

    #endregion

    #region Update Loop (Server)

    // Server-side Update for lifetime check AND activation timeout
    void Update()
    {
        if (!IsServer || isDying) return; // Only run on server, and stop if already dying

        // Optional: Log rotation in first server update frame (can get spammy)
        // if (Time.frameCount % 100 == 1) // Example: Log every 100 frames
        //    Debug.Log($"[Server Spirit {NetworkObjectId}] Update Frame. Rotation Euler: {transform.rotation.eulerAngles}");

        // --- Lifetime Check --- 
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0f)
        {
            Debug.Log($"[Server Spirit {NetworkObjectId}] Despawning due to lifetime expiry.");
            Die(PlayerRole.None); // Use the existing Die method, explicitly pass None
            return; // Exit early as Die handles despawn
        }

        // --- Activated Timeout Check --- 
        if (isActivated.Value)
        {
            activatedTimer += Time.deltaTime;
            if (activatedTimer >= activatedTimeoutDuration)
            {
                Debug.Log($"[Server Spirit {NetworkObjectId}] Triggering timeout bullet spawn.");
                HandleActivatedTimeout(); // This method now handles deregistering and despawn
            }
        }
    }

    #endregion

    // --- Activated Timeout Bullet Spawning (Server Only) ---
    private void HandleActivatedTimeout()
    {
        // Double check conditions just in case
        if (!IsServer || isDying || !isActivated.Value) { return; }

        isDying = true; // Prevent other actions

        // --- Deregister Before Despawn --- 
        if (SpiritRegistry.Instance != null)
        {
             SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // ------------------------------- 

        if (playerTransform == null)
        {
            // ... (rest of existing null check logic, despawn self)
            Debug.LogWarning($"[Server Spirit {NetworkObjectId}] Cannot spawn timeout bullets: Target player transform is null.");
            NetworkObject selfNO = GetComponent<NetworkObject>();
            if(selfNO != null && selfNO.IsSpawned) selfNO.Despawn(true);
            return;
        }
        if (spiritLargeBulletPrefab == null)
        {
            // ... (rest of existing null check logic, despawn self)
            Debug.LogError($"[Server Spirit {NetworkObjectId}] Cannot spawn timeout bullets: spiritLargeBulletPrefab is not assigned!");
            NetworkObject selfNO = GetComponent<NetworkObject>();
            if(selfNO != null && selfNO.IsSpawned) selfNO.Despawn(true);
            return;
        }

        // ... (rest of existing bullet spawning logic) ...
        Vector3 currentPosition = transform.position;
        Vector3 directionToPlayer = (playerTransform.position - currentPosition).normalized;
        Vector3 leftDirection = Quaternion.Euler(0, 0, bulletSpreadAngle) * directionToPlayer;
        Vector3 rightDirection = Quaternion.Euler(0, 0, -bulletSpreadAngle) * directionToPlayer;
        SpawnTimeoutBullet(directionToPlayer, currentPosition);
        SpawnTimeoutBullet(leftDirection, currentPosition);
        SpawnTimeoutBullet(rightDirection, currentPosition);

        // Despawn self
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        { 
            networkObject.Despawn(true);
        }
    }

    private void SpawnTimeoutBullet(Vector3 direction, Vector3 spawnPosition)
    {
        // ... (Existing logic)
    }
    // -------------------------------------------------

    // --- NEW: Revenge Spirit Spawning (Server Only) ---
    private void SpawnRevengeSpirit(PlayerRole killerRole)
    {
        Debug.Log($"[Server Spirit {NetworkObjectId}] SpawnRevengeSpirit entered. Killer: {killerRole}");
        if (spiritPrefabRef == null)
        {
            Debug.LogError($"[Server Spirit {NetworkObjectId}] Cannot spawn revenge spirit: spiritPrefabRef is not assigned!");
            return;
        }
        if (SpiritRegistry.Instance == null)
        {
            Debug.LogError($"[Server Spirit {NetworkObjectId}] Cannot spawn revenge spirit: SpiritRegistry instance not found!");
            return;
        }

        PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
        Debug.Log($"[Server Spirit {NetworkObjectId}] Determined Opponent Role: {opponentRole}");

        // Check max spirit count for the opponent
        int opponentSpiritCount = SpiritRegistry.Instance.GetSpiritCount(opponentRole);
        Debug.Log($"[Server Spirit {NetworkObjectId}] Opponent ({opponentRole}) current spirit count: {opponentSpiritCount}, Max allowed: {maxSpiritsPerSide}");
        if (opponentSpiritCount >= maxSpiritsPerSide)
        {
            Debug.LogWarning($"[Server Spirit {NetworkObjectId}] Max spirit count reached for {opponentRole}. Revenge spirit not spawned.");
            return;
        }

        // Determine opponent's spawn zone
        Transform opponentZone = (opponentRole == PlayerRole.Player1) ? player1SpawnZoneRef : player2SpawnZoneRef;
        if (opponentZone == null)
        {
             Debug.LogError($"[Server Spirit {NetworkObjectId}] Cannot spawn revenge spirit: Opponent ({opponentRole}) spawn zone reference is null!");
            return;
        }
        Debug.Log($"[Server Spirit {NetworkObjectId}] Found opponent spawn zone: {opponentZone.name}");

        // Calculate spawn position within opponent's zone
        Vector3 center = opponentZone.position;
        float randomX = Random.Range(-revengeSpawnZoneSize.x / 2f, revengeSpawnZoneSize.x / 2f);
        float randomY = Random.Range(-revengeSpawnZoneSize.y / 2f, revengeSpawnZoneSize.y / 2f);
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);
        Debug.Log($"[Server Spirit {NetworkObjectId}] Calculated revenge spawn position: {spawnPosition}");

        // Instantiate with corrected rotation
        GameObject spiritInstance = Instantiate(spiritPrefabRef, spawnPosition, Quaternion.Euler(0, 0, 90f)); // Re-apply 90-degree Z rotation
        // Comment out rotation log
        // Debug.Log($"[Server Spirit Spawner] Instantiated revenge spirit. Initial Rotation Euler: {spiritInstance.transform.rotation.eulerAngles}");

        // Get Components
        SpiritController newSpiritController = spiritInstance.GetComponent<SpiritController>();
        NetworkObject newNetworkObject = spiritInstance.GetComponent<NetworkObject>();

        if (newSpiritController == null || newNetworkObject == null)
        {
            Debug.LogError($"[Server Spirit {NetworkObjectId}] Revenge spirit prefab is missing SpiritController or NetworkObject! Destroying instance.", spiritInstance);
            Destroy(spiritInstance);
            return;
        }

        // Find opponent's player transform (for potential aiming, though we disable it here)
        Transform opponentPlayerTransform = null;
        if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerDataByRole(opponentRole);
            if (opponentData.HasValue)
            {
                NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(opponentData.Value.ClientId);
                if (playerNetObj != null) opponentPlayerTransform = playerNetObj.transform;
            }
        }

        if (opponentPlayerTransform != null) Debug.Log($"[Server Spirit {NetworkObjectId}] Found opponent player transform: {opponentPlayerTransform.name}");
        else Debug.LogWarning($"[Server Spirit {NetworkObjectId}] Could not find opponent player transform for Initialize.");

        // Spawn Network Object
        newNetworkObject.Spawn(true);
        Debug.Log($"[Server Spirit {NetworkObjectId}] Spawned new spirit NetworkObject (ID: {newNetworkObject.NetworkObjectId})");

        // Initialize the new spirit
        // Note: Revenge spirits are not set to aim initially
        newSpiritController.Initialize(opponentPlayerTransform, opponentRole, false, player1SpawnZoneRef, player2SpawnZoneRef);
        Debug.Log($"[Server Spirit {NetworkObjectId}] Initialized revenge spirit (NetID: {newNetworkObject.NetworkObjectId}) for {opponentRole}.");
    }
    // -------------------------------------------------
}