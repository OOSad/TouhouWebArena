using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Holds character-specific base stats and configuration values.
/// This component is attached to the player character prefab and provides data
/// for other systems like movement, shooting, health, spell bar, etc.
/// Values are typically configured in the Inspector.
/// </summary>
public class CharacterStats : NetworkBehaviour // Inherit from NetworkBehaviour for potential future networked stats/RPCs
{
    [Header("Character Info")]
    [SerializeField]
    [Tooltip("Unique identifier name for the character (e.g., HakureiReimu, KirisameMarisa). Used for loading specific assets like spellcards.")]
    private string characterName = "Default";

    [Header("Attacks")]
    [SerializeField]
    [Tooltip("The bullet prefab this character fires for basic shots.")]
    private GameObject bulletPrefab;

    [SerializeField]
    [Tooltip("The prefab for this character's specific Charge Attack (Level 1).")]
    private GameObject chargeAttackPrefab;

    [Header("Spell Bar Rates")]
    [SerializeField]
    [Tooltip("Rate the passive spell bar fills per second (units per second, where 4 units is 100%).")]
    private float passiveFillRate = 0.1f;

    [SerializeField]
    [Tooltip("Rate the active spell bar charges per second while holding the fire key (units per second, where 4 units is 100%).")]
    private float activeChargeRate = 2.0f;

    /// <summary>Gets the rate at which the passive spell bar fills automatically over time.</summary>
    public float GetPassiveFillRate() => passiveFillRate;
    /// <summary>Gets the rate at which the active spell bar charges while the fire key is held.</summary>
    public float GetActiveChargeRate() => activeChargeRate;

    [Header("Shooting Settings")]
    [SerializeField] 
    [Tooltip("Horizontal distance between the pair of bullets fired in a basic shot.")]
    private float bulletSpread = 0.2f;

    [SerializeField] 
    [Tooltip("Number of bullet pairs fired in a single burst of the basic shot.")]
    private int burstCount = 3;

    [SerializeField] 
    [Tooltip("Time delay (in seconds) between each pair of shots within a single burst.")]
    private float timeBetweenBurstShots = 0.08f;

    [SerializeField] 
    [Tooltip("Cooldown time (in seconds) after a burst finishes before the next burst can start.")]
    private float burstCooldown = 0.3f;

    /// <summary>Gets the prefab used for the character's basic shots.</summary>
    public GameObject GetBulletPrefab() => bulletPrefab;
    /// <summary>Gets the horizontal spread between the pair of basic shot bullets.</summary>
    public float GetBulletSpread() => bulletSpread;
    /// <summary>Gets the number of bullet pairs fired in a single basic shot burst.</summary>
    public int GetBurstCount() => burstCount;
    /// <summary>Gets the time delay between bullet pairs within a basic shot burst.</summary>
    public float GetTimeBetweenBurstShots() => timeBetweenBurstShots;
    /// <summary>Gets the cooldown time required after a basic shot burst finishes.</summary>
    public float GetBurstCooldown() => burstCooldown;

    [Header("Movement Settings")]
    [SerializeField] 
    [Tooltip("Base movement speed of the character in units per second.")]
    private float moveSpeed = 5.0f;

    [SerializeField] 
    [Tooltip("Speed multiplier applied when the character is in focus mode (holding Shift).")]
    private float focusSpeedModifier = 0.5f;

    /// <summary>Gets the character's base movement speed.</summary>
    public float GetMoveSpeed() => moveSpeed;
    /// <summary>Gets the speed multiplier applied during focus mode.</summary>
    public float GetFocusSpeedModifier() => focusSpeedModifier;

    [Header("Health & Defense Settings")]
    [SerializeField]
    [Tooltip("The amount of health the character starts with (usually also the maximum health).")]
    private int startingHealth = 5;

    [SerializeField]
    [Tooltip("Duration (in seconds) of the invincibility period after taking damage.")]
    private float invincibilityDuration = 2f;

    /// <summary>Gets the character's starting (and maximum) health value.</summary>
    public int GetStartingHealth() => startingHealth;
    /// <summary>Gets the duration of the invincibility frames after taking damage.</summary>
    public float GetInvincibilityDuration() => invincibilityDuration;

    [Header("Bomb Settings")]
    [SerializeField]
    [Tooltip("Radius (in world units) of the character's death bomb effect, which clears certain objects.")]
    private float deathBombRadius = 5f;

    /// <summary>Gets the radius of the character's death bomb effect.</summary>
    public float GetDeathBombRadius() => deathBombRadius;

    /// <summary>Gets the unique identifier name for this character (e.g., "HakureiReimu").</summary>
    public string GetCharacterName() => characterName;

    /// <summary>Gets the prefab used for this character's Charge Attack (Level 1).</summary>
    public GameObject GetChargeAttackPrefab() => chargeAttackPrefab;

    // OnNetworkSpawn is no longer needed here for pool registration
    // The pool manager now initializes from its Inspector list.
    // public override void OnNetworkSpawn()
    // {
    //     base.OnNetworkSpawn();
    //     if (!IsServer) return; 
    //     if (NetworkObjectPool.Instance == null) return;
    //     // ... removed registration logic ... 
    // }

    // Add other character-specific stats here later if needed
    // (e.g., unique ability cooldowns)
} 