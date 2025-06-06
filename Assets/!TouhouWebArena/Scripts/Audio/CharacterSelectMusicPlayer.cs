using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CharacterSelectMusicPlayer : MonoBehaviour
{
    [Tooltip("The music clip for the character select screen. Assign the same clip as MainMenuMusic if it should continue.")]
    public AudioClip characterSelectMusicClip;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("[CharacterSelectMusicPlayer] AudioSource component not found! Please add one.", this);
            enabled = false;
            return;
        }

        if (characterSelectMusicClip == null)
        {
            Debug.LogWarning("[CharacterSelectMusicPlayer] Character Select Music Clip not assigned in the Inspector.", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Diagnostic log
        string lastClipName = MusicStateManager.LastPlayedMenuClip == null ? "null" : MusicStateManager.LastPlayedMenuClip.name;
        string thisClipName = characterSelectMusicClip == null ? "null" : characterSelectMusicClip.name;
        Debug.Log($"[CharacterSelectMusicPlayer Start Values] LastPlayedMenuClip: {lastClipName}, This CS Music Clip: {thisClipName}, LastMenuClipTime: {MusicStateManager.LastMenuClipTime}, GameplayMusicActive: {MusicStateManager.GameplayMusicActive}");

        // If gameplay music was active (e.g., perhaps an unusual transition), 
        // reset state and play character select music from beginning.
        if (MusicStateManager.GameplayMusicActive)
        {
            Debug.Log("[CharacterSelectMusicPlayer] GameplayMusic was active. Resetting and playing CS music from beginning.");
            MusicStateManager.GameplayMusicActive = false; // Reset the flag
            MusicStateManager.ClearMenuMusicState();
            
            audioSource.clip = characterSelectMusicClip;
            audioSource.time = 0f;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"[CharacterSelectMusicPlayer] Playing '{characterSelectMusicClip.name}' from beginning.");
        }
        // If there's a stored menu clip, and the clip assigned to this component is the same (by name), resume it.
        else if (MusicStateManager.LastPlayedMenuClip != null && 
                 characterSelectMusicClip != null &&
                 MusicStateManager.LastPlayedMenuClip.name == characterSelectMusicClip.name)
        {
            audioSource.clip = characterSelectMusicClip; // Use the clip assigned to this component
            audioSource.time = MusicStateManager.LastMenuClipTime;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"[CharacterSelectMusicPlayer] Resuming '{characterSelectMusicClip.name}' (matched by name) from {MusicStateManager.LastMenuClipTime}s.");
        }
        // Otherwise, play the character select music from the beginning.
        else
        {
            audioSource.clip = characterSelectMusicClip;
            audioSource.time = 0f;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"[CharacterSelectMusicPlayer] Playing '{characterSelectMusicClip.name}' from beginning (default or different clip).");
        }
        MusicStateManager.GameplayMusicActive = false; // Ensure this is reset
    }

    void OnDisable()
    {
        Debug.Log($"[CharacterSelectMusicPlayer OnDisable] Called. GameplayMusicActive: {MusicStateManager.GameplayMusicActive}");
        if (audioSource != null && characterSelectMusicClip != null) 
        {
            // Check if the currently loaded clip in the AudioSource is the characterSelectMusicClip for this instance.
            bool clipMatches = audioSource.clip != null && audioSource.clip.name == characterSelectMusicClip.name;
            
            if (clipMatches && !MusicStateManager.GameplayMusicActive)
            {
                MusicStateManager.LastPlayedMenuClip = characterSelectMusicClip; // Storing the reference from *this* script
                MusicStateManager.LastMenuClipTime = audioSource.time;
                Debug.Log($"[CharacterSelectMusicPlayer OnDisable] Saved '{characterSelectMusicClip.name}' time: {audioSource.time}s. (Current audiosource clip: {audioSource.clip.name})");
            }
            else
            {
                Debug.Log("[CharacterSelectMusicPlayer OnDisable] Did not save state. Reasons: " +
                          $"MusicStateManager.GameplayMusicActive -> {MusicStateManager.GameplayMusicActive}, " +
                          $"Clip Name Match (audioSource.clip.name == characterSelectMusicClip.name) -> {clipMatches} " +
                          $"(AudioSource Clip: {(audioSource.clip == null ? "null" : audioSource.clip.name)}, CS MusicClip Field: {characterSelectMusicClip.name})");
            }
        }
        else
        {
            Debug.Log($"[CharacterSelectMusicPlayer OnDisable] audioSource field ({(audioSource == null ? "null" : "assigned")}) or characterSelectMusicClip field ({ (characterSelectMusicClip == null ? "null" : "assigned")}) is null. Not saving state.");
        }
    }
} 