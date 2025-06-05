using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))] // Ensure AudioSource is present
public class LilyWhiteAttackPattern : MonoBehaviour
{
    public enum SweepDirection
    {
        Shortest, // Default LerpAngle behavior
        Clockwise,
        CounterClockwise
    }

    [System.Serializable]
    public struct LilySweepParameters
    {
        [Tooltip("Descriptive name for this sweep, for Inspector clarity.")]
        public string sweepName;
        [Tooltip("PrefabID of the bullet to spawn from ClientGameObjectPool.")]
        public string bulletPrefabId;
        [Tooltip("Color to apply to the spawned bullets. Ensure Alpha is 255 for visibility.")]
        public Color bulletColor;
        [Tooltip("Delay in seconds after Lily White starts ascending before this sweep begins.")]
        public float startDelay;
        [Tooltip("Time in seconds between spawning each 3-bullet claw.")]
        public float clawSpawnInterval;
        [Tooltip("Angular offsets in degrees for each bullet in a claw, relative to the claw's center aim direction.")]
        public float[] clawAngleOffsetsDegrees;
        [Tooltip("Absolute screen angle in degrees where the sweep's aiming starts (0=right, 90=up).")]
        public float sweepStartAngleDegrees;
        [Tooltip("Target screen angle in degrees for the sweep's aiming.")]
        public float sweepEndAngleDegrees;
        [Tooltip("Direction of the sweep's rotation.")]
        public SweepDirection sweepRotationDirection; // New field
        [Tooltip("Total duration in seconds for this sweep's rotation to complete.")]
        public float sweepDurationSeconds;
        [Tooltip("Minimum speed of the spawned bullets.")]
        public float minBulletSpeed; // Changed from bulletSpeed
        [Tooltip("Maximum speed of the spawned bullets. Set to the same as Min Speed for no variation.")]
        public float maxBulletSpeed; // New field
        [Tooltip("Lifetime of the spawned bullets in seconds.")]
        public float bulletLifetime;
    }

    [Header("Attack Configuration")]
    public List<LilySweepParameters> attackSweeps = new List<LilySweepParameters>();

    [Header("Sound Settings")] // New header for sound related fields
    public AudioClip attackSoundClip;
    public float attackSoundRepeatDelay = 0.15f; // Time between each sound play
    
    private AudioSource audioSource;
    private Coroutine attackSoundCoroutine; // To manage the sound loop

    private Transform lilyTransform; // To store Lily White's current transform for bullet spawning
    private PlayerRole _targetedPlayerRole = PlayerRole.None; // New field to store the targeted player role

    void Awake() // Added Awake to get AudioSource
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
             Debug.LogError("[LilyWhiteAttackPattern] AudioSource component not found despite RequireComponent!");
        }
    }

    public void StartAttackSequence(Transform ownerTransform, PlayerRole targetedPlayerRole)
    {
        this.lilyTransform = ownerTransform;
        _targetedPlayerRole = targetedPlayerRole; // Store the targeted player role
        if (attackSweeps == null || attackSweeps.Count == 0)
        {
            Debug.LogWarning("LilyWhiteAttackPattern: No attack sweeps configured.");
            return;
        }

        foreach (LilySweepParameters sweepParams in attackSweeps)
        {
            StartCoroutine(ExecuteSweepCoroutine(sweepParams));
        }
    }

    private IEnumerator ExecuteSweepCoroutine(LilySweepParameters p)
    {
        if (string.IsNullOrEmpty(p.bulletPrefabId))
        {
            Debug.LogError($"LilySweepParameters '{p.sweepName}' has no bulletPrefabId defined. Skipping sweep.");
            yield break;
        }
        if (p.clawAngleOffsetsDegrees == null || p.clawAngleOffsetsDegrees.Length == 0)
        {
            Debug.LogWarning($"LilySweepParameters '{p.sweepName}' has no clawAngleOffsetsDegrees defined. Defaulting to a single bullet.");
            p.clawAngleOffsetsDegrees = new float[] { 0f };
        }

        yield return new WaitForSeconds(p.startDelay);

        // Start the attack sound loop
        if (attackSoundCoroutine != null) 
        {
            StopCoroutine(attackSoundCoroutine);
        }
        attackSoundCoroutine = StartCoroutine(PlayAttackSoundLoopCoroutine());

        float sweepTimer = 0f;
        float clawSpawnTimer = 0f;

        // Remove obsolete adjustedSweepEndAngle logic based on Mathf.LerpAngle assumptions
        // float adjustedSweepEndAngle = p.sweepEndAngleDegrees;
        // --- DIAGNOSTIC LOGS (Initial parameters) ---
        Debug.Log($"[LilyWhiteAttackPattern] Executing Sweep: {p.sweepName}\nStart Angle: {p.sweepStartAngleDegrees}, End Angle: {p.sweepEndAngleDegrees}, Direction: {p.sweepRotationDirection}");

        float totalRotationAmount = 0f;
        if (p.sweepRotationDirection == SweepDirection.Shortest)
        {
            totalRotationAmount = Mathf.DeltaAngle(p.sweepStartAngleDegrees, p.sweepEndAngleDegrees);
        }
        else if (p.sweepRotationDirection == SweepDirection.Clockwise)
        {
            float normalizedStart = (p.sweepStartAngleDegrees % 360 + 360) % 360;
            float normalizedEnd = (p.sweepEndAngleDegrees % 360 + 360) % 360;
            if (normalizedEnd >= normalizedStart)
            {
                totalRotationAmount = normalizedEnd - normalizedStart;
            }
            else // End angle is "behind" start angle when going clockwise (e.g. start 70, end 60)
            {
                totalRotationAmount = (360f - normalizedStart) + normalizedEnd;
            }
        }
        else // CounterClockwise
        {
            float normalizedStart = (p.sweepStartAngleDegrees % 360 + 360) % 360;
            float normalizedEnd = (p.sweepEndAngleDegrees % 360 + 360) % 360;
            if (normalizedEnd <= normalizedStart)
            {
                totalRotationAmount = normalizedEnd - normalizedStart; // Will be negative or zero
            }
            else // End angle is "ahead" of start angle when going counter-clockwise (e.g. start 60, end 70)
            {
                totalRotationAmount = -((360f - normalizedEnd) + normalizedStart);
            }
        }

        Debug.Log($"[LilyWhiteAttackPattern] Calculated Total Rotation Amount: {totalRotationAmount}");

        while (sweepTimer < p.sweepDurationSeconds)
        {
            sweepTimer += Time.deltaTime;
            clawSpawnTimer += Time.deltaTime;

            if (clawSpawnTimer >= p.clawSpawnInterval)
            {
                clawSpawnTimer -= p.clawSpawnInterval;

                float sweepProgress = Mathf.Clamp01(sweepTimer / p.sweepDurationSeconds);
                // Custom angle interpolation
                float currentClawCenterAngle = p.sweepStartAngleDegrees + (totalRotationAmount * sweepProgress);
                
                // --- BEGIN DIAGNOSTIC LOGS ---
                if (p.sweepName == "Blue Sweep, Small Bullets") //REPLACE WITH YOUR TEST SWEEP NAME IF DIFFERENT
                {
                    Debug.Log($"[LilyWhiteAttackPattern] Sweep '{p.sweepName}' (PROGRESSIVE) - Time: {sweepTimer:F2}, Progress: {sweepProgress:F2}, CurrentClawCenterAngle: {currentClawCenterAngle:F2}");
                }
                // --- END DIAGNOSTIC LOGS ---

                SpawnClaw(currentClawCenterAngle, p);
            }
            yield return null;
        }

        // Stop the attack sound loop once the sweep is done
        if (attackSoundCoroutine != null)
        { 
            StopCoroutine(attackSoundCoroutine);
            attackSoundCoroutine = null;
        }
    }

    private void SpawnClaw(float centerAngleDegrees, LilySweepParameters p)
    {
        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError("LilyWhiteAttackPattern: ClientGameObjectPool.Instance is null. Cannot spawn bullets.");
            return;
        }
        if (lilyTransform == null)
        {
            Debug.LogError("LilyWhiteAttackPattern: lilyTransform is null. Cannot determine spawn position.");
            return;
        }

        foreach (float offsetAngle in p.clawAngleOffsetsDegrees)
        {
            float finalBulletAngleDegrees = centerAngleDegrees + offsetAngle;
            Quaternion bulletRotation = Quaternion.Euler(0, 0, finalBulletAngleDegrees);
            Vector2 directionVector = bulletRotation * Vector2.right;

            GameObject bulletInstance = ClientGameObjectPool.Instance.GetObject(p.bulletPrefabId);
            if (bulletInstance == null)
            {
                Debug.LogWarning($"LilyWhiteAttackPattern: Failed to get bullet prefab '{p.bulletPrefabId}' from pool for sweep '{p.sweepName}'.");
                continue;
            }

            bulletInstance.transform.position = lilyTransform.position;

            SpriteRenderer sr = bulletInstance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = p.bulletColor;
            }

            StageSmallBulletMoverScript mover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
            if (mover != null)
            {
                // Calculate a random speed between min and max
                float actualBulletSpeed = (p.minBulletSpeed == p.maxBulletSpeed) ? p.minBulletSpeed : Random.Range(p.minBulletSpeed, p.maxBulletSpeed);
                mover.Initialize(directionVector, actualBulletSpeed, p.bulletLifetime, _targetedPlayerRole);
            }
            else
            {
                Debug.LogWarning($"Bullet prefab '{p.bulletPrefabId}' does not have StageSmallBulletMoverScript. Movement might not be initialized.");
            }
            
            bulletInstance.SetActive(true);

            // Debug logs can be re-enabled if needed
            // Debug.Log($"--- Bullet Debug START for '{p.bulletPrefabId}' (Sweep: {p.sweepName}) ---");
            // if (sr != null)
            // {
            //     Debug.Log($"SR.enabled: {sr.enabled}");
            //     Debug.Log($"SR.sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
            //     Debug.Log($"SR.color: {sr.color}");
            //     Debug.Log($"SR.sortingLayerName: {sr.sortingLayerName}");
            //     Debug.Log($"SR.sortingOrder: {sr.sortingOrder}");
            // }
            // else
            // {
            //     Debug.LogWarning("SpriteRenderer component is NULL.");
            // }
            // Debug.Log($"Transform.localScale: {bulletInstance.transform.localScale}");
            // Debug.Log($"GameObject.layer: {LayerMask.LayerToName(bulletInstance.layer)} (Raw int: {bulletInstance.layer})");
            // Debug.Log($"--- Bullet Debug END for '{p.bulletPrefabId}' (Sweep: {p.sweepName}) ---");
        }
    }

    private IEnumerator PlayAttackSoundLoopCoroutine()
    {
        if (attackSoundClip == null || audioSource == null)
        {
            Debug.LogWarning("LilyWhiteAttackPattern: Attack sound clip or AudioSource missing, cannot play attack sound loop.");
            yield break;
        }

        audioSource.loop = false; // Ensure PlayOneShot behavior is not overridden by AudioSource loop setting

        while (true) // Loop indefinitely until stopped by StopCoroutine
        {
            audioSource.PlayOneShot(attackSoundClip);
            yield return new WaitForSeconds(attackSoundRepeatDelay);
        }
    }
} 