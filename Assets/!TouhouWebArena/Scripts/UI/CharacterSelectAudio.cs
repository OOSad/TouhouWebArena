using UnityEngine;

/// <summary>
/// Handles playing audio feedback sounds for the Character Selection screen.
/// Requires an AudioSource component on the same GameObject.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CharacterSelectAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Sound played when navigating between character buttons.")]
    [SerializeField] private AudioClip navigateSound;
    [Tooltip("Sound played when confirming a character selection.")]
    [SerializeField] private AudioClip confirmSound;

    /// <summary>Cached reference to the AudioSource component.</summary>
    private AudioSource uiAudioSource;

    private void Awake()
    {
        // Get the required AudioSource component
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null)
        {
            Debug.LogError("[CharacterSelectAudio] AudioSource component not found! Audio will not play.", this);
            enabled = false; // Disable component if AudioSource is missing
        }
    }

    /// <summary>
    /// Plays the navigation sound effect if assigned.
    /// </summary>
    public void PlayNavigateSound()
    {
        PlaySound(navigateSound);
    }

    /// <summary>
    /// Plays the confirmation sound effect if assigned.
    /// </summary>
    public void PlayConfirmSound()
    {
        PlaySound(confirmSound);
    }

    /// <summary>
    /// Plays the given audio clip using the cached AudioSource, if available.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && uiAudioSource.isActiveAndEnabled && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
        // else: Optionally log warning if clip is null, but might be intentional
    }
} 