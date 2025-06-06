using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MainMenuMusic : MonoBehaviour
{
    [Tooltip("The music clip for the main menu and character select screen.")]
    public AudioClip menuMusicClip;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("[MainMenuMusic] AudioSource component not found! Please add one.", this);
            enabled = false;
            return;
        }

        if (menuMusicClip == null)
        {
            Debug.LogWarning("[MainMenuMusic] Menu Music Clip not assigned in the Inspector.", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Diagnostic log
        string lastClipName = MusicStateManager.LastPlayedMenuClip == null ? "null" : MusicStateManager.LastPlayedMenuClip.name;
        string thisClipName = menuMusicClip == null ? "null" : menuMusicClip.name;
        Debug.Log($"[MainMenuMusic Start Values] LastPlayedMenuClip: {lastClipName}, This Menu Music Clip: {thisClipName}, LastMenuClipTime: {MusicStateManager.LastMenuClipTime}, GameplayMusicActive: {MusicStateManager.GameplayMusicActive}");

        // When the main menu starts/restarts, we check the MusicStateManager.
        // If gameplay music was active, it means we are returning from a game,
        // so the menu music should start from the beginning.
        if (MusicStateManager.GameplayMusicActive)
        {
            Debug.Log("[MainMenuMusic] GameplayMusic was active. Resetting menu music state and starting from beginning.");
            MusicStateManager.GameplayMusicActive = false; // Reset the flag
            MusicStateManager.ClearMenuMusicState(); // Clear any lingering time/clip
            
            audioSource.clip = menuMusicClip;
            audioSource.time = 0f;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"[MainMenuMusic] Playing '{menuMusicClip.name}' from beginning.");
        }
        // If there's a stored menu clip (e.g., coming from character select back to main menu without gameplay intervening)
        // AND that clip is the one we intend to play (by name), resume it.
        else if (MusicStateManager.LastPlayedMenuClip != null && 
                 menuMusicClip != null &&
                 MusicStateManager.LastPlayedMenuClip.name == menuMusicClip.name)
        {
            audioSource.clip = menuMusicClip; // Use the clip assigned to this component
            audioSource.time = MusicStateManager.LastMenuClipTime;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"[MainMenuMusic] Resuming '{menuMusicClip.name}' (matched by name) from {MusicStateManager.LastMenuClipTime}s.");
        }
        // Otherwise (e.g., fresh game start, or returning from a scene that didn't set menu state)
        else if (menuMusicClip != null)
        {
            audioSource.clip = menuMusicClip;
            audioSource.time = 0f;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"[MainMenuMusic] Playing '{menuMusicClip.name}' from beginning (default case).");
        }
        MusicStateManager.GameplayMusicActive = false; // Ensure this is reset when main menu is active
    }

    void OnDisable()
    {
        Debug.Log($"[MainMenuMusic OnDisable] Called. GameplayMusicActive: {MusicStateManager.GameplayMusicActive}");
        if (audioSource != null && menuMusicClip != null)
        {
            // Check if the currently loaded clip in the AudioSource is the menuMusicClip for this instance.
            bool clipMatches = audioSource.clip != null && audioSource.clip.name == menuMusicClip.name;
            
            if (clipMatches && !MusicStateManager.GameplayMusicActive)
            {
                MusicStateManager.LastPlayedMenuClip = menuMusicClip; // Storing the reference from *this* script
                MusicStateManager.LastMenuClipTime = audioSource.time;
                Debug.Log($"[MainMenuMusic OnDisable] Saved \'{menuMusicClip.name}\' time: {audioSource.time}s. (Current audiosource clip: {audioSource.clip.name})");
            }
            else
            {
                Debug.Log("[MainMenuMusic OnDisable] Did not save state. Reasons: " +
                          $"MusicStateManager.GameplayMusicActive -> {MusicStateManager.GameplayMusicActive}, " +
                          $"Clip Name Match (audioSource.clip.name == menuMusicClip.name) -> {clipMatches} " +
                          $"(AudioSource Clip: {(audioSource.clip == null ? "null" : audioSource.clip.name)}, MenuMusicClip Field: {menuMusicClip.name})");
            }
        }
        else
        {
            Debug.Log($"[MainMenuMusic OnDisable] audioSource field ({(audioSource == null ? "null" : "assigned")}) or menuMusicClip field ({ (menuMusicClip == null ? "null" : "assigned")}) is null. Not saving state.");
        }
    }
} 