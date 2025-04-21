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

        playerTransform = targetPlayer; // Can be null if not aiming
        ownerRole = owner; // Store the owner role
        aimAtPlayerOnSpawn = shouldAim;
        player1SpawnZoneRef = p1Zone;
        player2SpawnZoneRef = p2Zone;

        // Validate essential references passed in
        if (ownerRole == PlayerRole.None)
        {
            // Consider warning or error
        }
        if (player1SpawnZoneRef == null || player2SpawnZoneRef == null)
        {
            // Consider warning or error
        }
        if (spiritPrefabRef == null) // Check prefab needed for revenge spawn
        {
            // Consider warning or error
        }

        // --- Reset State for Pooling --- 
        isDying = false; // CRITICAL: Reset dying flag
        currentHp.Value = normalMaxHp;
        isActivated.Value = false; // Explicitly set initial state
        activatedTimer = 0f; // Reset timer on init
        currentLifetime = maxLifetime; // Reset lifetime timer
        // -------------------------------        

        // Set initial velocity (server-side)
        SetInitialVelocity();

        // --- Register with Registry (Server Only) --- 
        if (IsServer) // Check IsServer here, although Initialize should only be called on server
        {
            if (SpiritRegistry.Instance != null)
            {
                SpiritRegistry.Instance.Register(this, ownerRole);
            }
            else
            {
                // Consider warning
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
            // Consider warning
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
            // -----------------------------------

            // Apply damage using the public TakeDamage method
            TakeDamage(damageAmount, killer); 

            // NOTE: The bullet should handle its own destruction when it hits something.
        }
        // Check collision with player body
        else if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>(); // Use GetComponentInParent
            if (playerHealth != null) // Check for null
            {
                // --- Get Player Role --- 
                PlayerRole playerHitRole = PlayerRole.None;
                if (PlayerDataManager.Instance != null)
                {
                    PlayerData? data = PlayerDataManager.Instance.GetPlayerData(playerHealth.OwnerClientId);
                    if (data.HasValue)
                    {
                         playerHitRole = data.Value.Role;
                    }
                }
                // --------------------
                
                // Player takes 1 damage, and the spirit dies. The spirit doesn't have a killer in this case.
                // MODIFIED: Removed owner check. Any spirit damages any player.
                if (playerHitRole != PlayerRole.None) // Only check if we successfully identified the player
                {
                    playerHealth.TakeDamage(1); // Corrected method call
                    // REMOVED: Die(PlayerRole.None); // Spirit no longer dies on player contact
                }
            }
            else
            {
                // Consider warning
            }
        }
    }

    // --- MODIFIED: Renamed from ApplyDamageInternal and made public ---
    public void TakeDamage(int amount, PlayerRole killerRole)
    {
        // Basic server check + check if already dying
        if (!IsServer || isDying || currentHp.Value <= 0) return;

        currentHp.Value -= amount;
        if (currentHp.Value <= 0 && !isDying)
        {
            Die(killerRole); // Pass the killer role to Die
        }
    }
    // -----------------------------------------------------------------

    // Server-side method to handle death logic
    private void Die(PlayerRole killerRole = PlayerRole.None) // Added killerRole parameter
    {
        if (!IsServer || isDying) return;
        isDying = true; // Prevent multiple calls

        // --- Handle Death Effect Spawning --- 
        GameObject effectPrefab = isActivated.Value ? activatedDeathEffectPrefab : normalDeathEffectPrefab;
        if (effectPrefab != null)
        {
            GameObject deathEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            NetworkObject netEffect = deathEffect.GetComponent<NetworkObject>();
            if (netEffect != null)
            {
                netEffect.Spawn(true); // Spawn the effect for all clients
            }
        }

        // --- Handle Revenge Spawn (Trigger ONLY if killed by the OWNING player) ---
        bool killedByOwner = (killerRole != PlayerRole.None && killerRole == ownerRole);
        if (killedByOwner && !isActivated.Value) // Only normal spirits trigger revenge when killed by owner
        {
            // When the OWNER kills the spirit, the revenge spirit should be owned by the OPPONENT.
            PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
            SpawnRevengeSpirit(opponentRole); // Pass OPPONENT's role as the owner for the new spirit
        }

        // --- Deregister from Registry --- 
        if (SpiritRegistry.Instance != null)
        {
            SpiritRegistry.Instance.Deregister(this, ownerRole); 
        }
        // ---------------------------------

        // --- Return to Pool instead of Despawning --- 
        if (NetworkObject != null)
        {
            NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject);
        }
        else if (gameObject != null) // Fallback if NetworkObject somehow null
        {
            Destroy(gameObject);
        }
        // --------------------------------------------
    }

    #endregion

    #region IClearableByBomb Implementation

    /// <summary>
    /// Called when the player's death bomb effect should clear this spirit.
    /// </summary>
    /// <param name="bombingPlayer">The role of the player who activated the bomb.</param>
    public void ClearByBomb(PlayerRole bombingPlayer)
    {
        // Only execute on the server and if the spirit is not already dying
        if (!IsServer || isDying)
        {
             return;
        }

        // --- MODIFIED: Check if owner bombed --- 
        if (bombingPlayer == ownerRole)
        {
            // Owner bombed their own spirit, treat it like a self-kill for revenge purposes
            Die(bombingPlayer); // Pass bombingPlayer as killerRole to trigger revenge for opponent
        }
        else
        {
            // Opponent bombed this spirit, clear without revenge
            Die(PlayerRole.None); // Killer is None, no revenge triggered
        }
        // --------------------------------------
    }

    #endregion

    #region Update Loop (Server)

    // Server-side Update for lifetime check AND activation timeout
    void Update()
    {
        if (!IsServer || isDying) return; // Only run on server, and stop if already dying

        // Lifetime countdown
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0f)
        {
            Die(PlayerRole.None); // Die with no killer due to timeout
            return; // Important: exit Update after Die() to avoid further logic on a dying object
        }

        // Activated state timeout handling
        if (isActivated.Value)
        {
            activatedTimer += Time.deltaTime;
            if (activatedTimer >= activatedTimeoutDuration)
            {
                HandleActivatedTimeout(); // This method now handles the entire timeout sequence (spawning bullets, despawning self)
                return; // Exit Update to prevent further processing on the now-despawned object
            }
        }
    }

    #endregion

    // --- Activated Timeout Bullet Spawning (Server Only) ---
    private void HandleActivatedTimeout()
    {
        // Double check conditions just in case
        if (!IsServer || isDying || !isActivated.Value) 
        {
            return; 
        }

        isDying = true; // Prevent other actions

        // --- Deregister Before Despawn --- 
        if (SpiritRegistry.Instance != null)
        {
             SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // -----------------------------------------------------------

        // --- Find the OPPONENT player transform at the time of timeout --- 
        PlayerRole opponentRole = (ownerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
        Transform opponentPlayerTransform = null;
        if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerDataByRole(opponentRole);
            if (opponentData.HasValue)
            {
                NetworkObject opponentNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(opponentData.Value.ClientId);
                if (opponentNetObj != null)
                {
                    opponentPlayerTransform = opponentNetObj.transform;
                }
            }
        }
        // -----------------------------------------------------------

        // Check if we found the opponent and if the bullet prefab is assigned
        if (opponentPlayerTransform == null) 
        {
            // Consider warning
            NetworkObject selfNO = GetComponent<NetworkObject>();
            if(selfNO != null && selfNO.IsSpawned) selfNO.Despawn(true);
            return;
        }
        if (spiritLargeBulletPrefab == null)
        {
            // Consider error
            NetworkObject selfNO = GetComponent<NetworkObject>();
            if(selfNO != null && selfNO.IsSpawned) selfNO.Despawn(true);
            return;
        }

        // Calculate directions based on the opponent's current position
        Vector3 currentPosition = transform.position;
        Vector3 directionToPlayer = (opponentPlayerTransform.position - currentPosition).normalized;
        Vector3 leftDirection = Quaternion.Euler(0, 0, bulletSpreadAngle) * directionToPlayer;
        Vector3 rightDirection = Quaternion.Euler(0, 0, -bulletSpreadAngle) * directionToPlayer;

        // Spawn the bullets
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
        if (!IsServer) return;

        if (spiritLargeBulletPrefab == null)
        {
            // Consider error
            return;
        }

        // Calculate rotation to face the direction
        Quaternion bulletRotation = Quaternion.LookRotation(Vector3.forward, direction); // Use LookRotation for 2D up direction

        // Instantiate the bullet prefab
        GameObject bulletInstance = Instantiate(spiritLargeBulletPrefab, spawnPosition, bulletRotation);

        // Get the NetworkObject
        NetworkObject bulletNetworkObject = bulletInstance.GetComponent<NetworkObject>();
        if (bulletNetworkObject == null)
        {
            // Consider error
            Destroy(bulletInstance);
            return;
        }

        // Spawn the bullet on the network
        bulletNetworkObject.Spawn(true); // Spawn server-owned
    }
    // -------------------------------------------------

    // --- NEW: Revenge Spirit Spawning (Server Only) ---
    private void SpawnRevengeSpirit(PlayerRole killerRole) // killerRole is now the OPPONENT's role
    {
        if (!IsServer || killerRole == PlayerRole.None) return;

        if (spiritPrefabRef == null)
        {
            // Consider error
            return;
        }
        if (SpiritRegistry.Instance == null)
        {
            // Consider warning
             return;
        }

        // The new spirit is owned by the OPPONENT (passed in as killerRole).
        PlayerRole newSpiritOwnerRole = killerRole; 

        // Check max spirit count for the OPPONENT's side.
        int opponentSpiritCount = SpiritRegistry.Instance.GetSpiritCount(newSpiritOwnerRole);
        if (opponentSpiritCount >= maxSpiritsPerSide)
        {
             // Consider warning
             return; // Opponent is at max capacity
        }

        // Determine the OPPONENT's spawn zone.
        Transform opponentSpawnZone = (newSpiritOwnerRole == PlayerRole.Player1) ? player1SpawnZoneRef : player2SpawnZoneRef;
        if (opponentSpawnZone == null)
        {
            // Consider error
            return;
        }
        
        // Calculate random position within the OPPONENT's spawn zone.
        float spawnX = Random.Range(-revengeSpawnZoneSize.x / 2f, revengeSpawnZoneSize.x / 2f);
        float spawnY = Random.Range(-revengeSpawnZoneSize.y / 2f, revengeSpawnZoneSize.y / 2f);
        Vector3 spawnPosition = opponentSpawnZone.position + new Vector3(spawnX, spawnY, 0);

        // --- Pool Integration --- 
        PoolableObjectIdentity identity = spiritPrefabRef.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            Debug.LogError($"[SpiritController] Revenge Spawn: Spirit prefab ref '{spiritPrefabRef.name}' missing identity/ID!", this);
            return;
        }
        string prefabID = identity.PrefabID;
        NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        if (pooledNetworkObject == null)
        {
            Debug.LogError($"[SpiritController] Revenge Spawn: Failed to get Spirit '{prefabID}' from pool.", this);
            return;
        }
        // ----------------------

        // Get required components from pooled object
        SpiritController newSpiritController = pooledNetworkObject.GetComponent<SpiritController>();
        // NetworkObject newNetworkObject = pooledNetworkObject; // Already have reference

        if (newSpiritController == null)
        {
            Debug.LogError("[SpiritController] Revenge Spawn: Pooled object missing SpiritController! Returning to pool.", this);
            NetworkObjectPool.Instance.ReturnNetworkObject(pooledNetworkObject); // Return broken object
            return;
        }

        // Position and Activate pooled object
        pooledNetworkObject.transform.position = spawnPosition;
        pooledNetworkObject.transform.rotation = Quaternion.identity;
        pooledNetworkObject.gameObject.SetActive(true);

        // Find OPPONENT player transform (to potentially aim at, though we don't aim revenge spirits)
        Transform opponentPlayerTransform = null;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerDataByRole(newSpiritOwnerRole); 
            if (opponentData.HasValue && NetworkManager.Singleton != null) 
            {
                NetworkObject opponentNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(opponentData.Value.ClientId);
                if (opponentNetObj != null)
                {
                    opponentPlayerTransform = opponentNetObj.transform; 
                }
            }
        }

        // Spawn the new spirit on the network FIRST
        pooledNetworkObject.Spawn(false); 
        
        // Initialize the new spirit AFTER spawning
        newSpiritController.Initialize(opponentPlayerTransform, newSpiritOwnerRole, false, player1SpawnZoneRef, player2SpawnZoneRef); // Don't aim initially
    }
    // -------------------------------------------------
}