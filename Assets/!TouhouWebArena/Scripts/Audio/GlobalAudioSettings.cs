using UnityEngine;

public static class GlobalAudioSettings
{
    [Tooltip("Global volume for sound effects played via PlayClipAtPoint or other managed systems.")]
    public static float SfxVolume = 0.05f;

    [Tooltip("The Unity Time.time when the last enemy defeat sound was played.")]
    public static float LastEnemyDefeatSoundPlayTime = -1f; // Initialize to a negative value to allow the first sound immediately

    [Tooltip("Minimum interval in seconds required between playing enemy defeat sounds to prevent stacking.")]
    public static float MinIntervalBetweenEnemyDefeatSounds = 0.1f; // 100ms interval, can be adjusted
} 