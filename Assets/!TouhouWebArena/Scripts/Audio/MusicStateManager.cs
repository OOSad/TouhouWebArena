using UnityEngine;

public static class MusicStateManager
{
    public static AudioClip LastPlayedMenuClip { get; set; } = null;
    public static float LastMenuClipTime { get; set; } = 0f;

    // This flag indicates that gameplay music is now in control,
    // so menu/character select music should not try to resume if we briefly
    // pass through a menu scene during a shutdown sequence, for example.
    public static bool GameplayMusicActive { get; set; } = false;

    public static void ClearMenuMusicState()
    {
        LastPlayedMenuClip = null;
        LastMenuClipTime = 0f;
        // GameplayMusicActive remains true until explicitly reset by a menu player
    }
} 