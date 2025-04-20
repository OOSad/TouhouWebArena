using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Holds character-specific stats, like spell bar charge rates.
/// Should be attached to the player character prefab.
/// </summary>
public class CharacterStats : NetworkBehaviour // Inherit from NetworkBehaviour if stats need syncing/RPCs later
{
    [Header("Character Info")]
    [SerializeField]
    [Tooltip("Unique identifier name for the character (e.g., Reimu, Marisa).")]
    private string characterName = "Default";

    [Header("Attacks")]
    [SerializeField]
    [Tooltip("The bullet prefab this character fires for normal shots.")]
    private GameObject bulletPrefab;

    [SerializeField]
    [Tooltip("The prefab for this character's specific Charge Attack.")]
    private GameObject chargeAttackPrefab;

    [Header("Spell Bar Rates")]
    [SerializeField]
    [Tooltip("Rate the passive bar fills per second (0-4).")]
    private float passiveFillRate = 0.1f;

    [SerializeField]
    [Tooltip("Rate the active bar charges per second (0-4).")]
    private float activeChargeRate = 2.0f;

    // Public getters for other scripts to read the rates
    public float GetPassiveFillRate() => passiveFillRate;
    public float GetActiveChargeRate() => activeChargeRate;

    [Header("Shooting Settings")]
    [SerializeField] 
    [Tooltip("Horizontal distance between bullet pairs.")]
    private float bulletSpread = 0.2f;

    [SerializeField] 
    [Tooltip("Number of shots/pairs in a single burst.")]
    private int burstCount = 3;

    [SerializeField] 
    [Tooltip("Time delay between shots/pairs within a burst.")]
    private float timeBetweenBurstShots = 0.08f;

    [SerializeField] 
    [Tooltip("Cooldown time after a burst finishes before the next can start.")]
    private float burstCooldown = 0.3f;

    // Public getters for shooting stats
    public GameObject GetBulletPrefab() => bulletPrefab;
    public float GetBulletSpread() => bulletSpread;
    public int GetBurstCount() => burstCount;
    public float GetTimeBetweenBurstShots() => timeBetweenBurstShots;
    public float GetBurstCooldown() => burstCooldown;

    [Header("Movement Settings")]
    [SerializeField] 
    [Tooltip("Base movement speed of the character.")]
    private float moveSpeed = 5.0f;

    [SerializeField] 
    [Tooltip("Speed multiplier when the character is focused.")]
    private float focusSpeedModifier = 0.5f;

    // Public getters for movement stats
    public float GetMoveSpeed() => moveSpeed;
    public float GetFocusSpeedModifier() => focusSpeedModifier;

    [Header("Health & Defense Settings")]
    [SerializeField]
    [Tooltip("The amount of health the character starts with (and max health).")]
    private int startingHealth = 5;

    [SerializeField]
    [Tooltip("Duration of invincibility frames after taking damage.")]
    private float invincibilityDuration = 2f;

    // Public getters for health stats
    public int GetStartingHealth() => startingHealth;
    public float GetInvincibilityDuration() => invincibilityDuration;

    [Header("Bomb Settings")]
    [SerializeField]
    [Tooltip("Radius of the character's death bomb effect.")]
    private float deathBombRadius = 5f;

    // Public getter for bomb stats
    public float GetDeathBombRadius() => deathBombRadius;

    // --- NEW: Public getter for character name --- 
    public string GetCharacterName() => characterName;
    // ---------------------------------------------

    // --- NEW: Public getter for charge attack prefab ---
    public GameObject GetChargeAttackPrefab() => chargeAttackPrefab;
    // -------------------------------------------------

    // Add other character-specific stats here later if needed
    // (e.g., unique ability cooldowns)
} 