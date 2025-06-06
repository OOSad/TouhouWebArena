using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // Required for List

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicPlayer : NetworkBehaviour
{
    [Tooltip("A list of music clips to be randomly played during gameplay.")]
    public List<AudioClip> gameplayMusicClips = new List<AudioClip>();

    private AudioSource audioSource;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[GMP OnNetworkSpawn] GameObject: {gameObject.name}, IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}", this);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("[GMP OnNetworkSpawn] AudioSource component not found! Music will not play.", this);
            enabled = false;
            return;
        }

        // Signal that gameplay music is now taking precedence.
        // This is done on all instances (server and clients) as soon as gameplay starts.
        MusicStateManager.GameplayMusicActive = true;
        // We don't need to clear LastPlayedMenuClip/Time here, 
        // as MainMenuMusic will handle that if it starts fresh.

        if (IsServer)
        {
            if (gameplayMusicClips == null || gameplayMusicClips.Count == 0)
            {
                Debug.LogWarning("[GMP OnNetworkSpawn - Server] No gameplay music clips assigned. Cannot select music.", this);
                return;
            }

            int randomIndex = Random.Range(0, gameplayMusicClips.Count);
            Debug.Log($"[GMP OnNetworkSpawn - Server] Choosing random music index: {randomIndex}", this);
            PlayGameplayMusicClientRpc(randomIndex);

            // If this is a dedicated server (not a host), ensure its own AudioSource is stopped.
            if (IsServer && !IsHost && audioSource != null)
            {
                Debug.Log("[GMP OnNetworkSpawn - Server] Dedicated server, ensuring its AudioSource is stopped.", this);
                audioSource.Stop(); 
            }
        }
    }

    [ClientRpc]
    private void PlayGameplayMusicClientRpc(int musicIndex)
    {
        Debug.Log($"[GMP PlayGameplayMusicClientRpc] Received for index: {musicIndex}. IsClient: {IsClient}, IsHost: {IsHost}", this);
        
        // Ensure AudioSource is available (it should be from OnNetworkSpawn for clients too)
        if (audioSource == null) 
        {
             audioSource = GetComponent<AudioSource>(); // Attempt to get it again
             if (audioSource == null) 
             {
                Debug.LogError("[GMP PlayGameplayMusicClientRpc] Client missing AudioSource component. Cannot play music.", this);
                return;
             }
        }

        // The old menu/char select music players should be destroyed by scene change.
        // The MusicStateManager.GameplayMusicActive flag prevents them from saving state if they weren't.

        if (gameplayMusicClips == null || gameplayMusicClips.Count == 0)
        {
            Debug.LogWarning("[GMP PlayGameplayMusicClientRpc] No gameplay music clips assigned on client.", this);
            return;
        }

        if (musicIndex < 0 || musicIndex >= gameplayMusicClips.Count)
        {
            Debug.LogError($"[GMP PlayGameplayMusicClientRpc] Received invalid music index: {musicIndex}. Clip count: {gameplayMusicClips.Count}", this);
            return;
        }

        AudioClip clipToPlay = gameplayMusicClips[musicIndex];

        if (clipToPlay != null)
        {
            Debug.Log($"[GMP PlayGameplayMusicClientRpc] Playing gameplay music: {clipToPlay.name} (Index: {musicIndex})", this);
            audioSource.clip = clipToPlay;
            audioSource.time = 0f; // Gameplay music always starts from beginning
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            Debug.LogError($"[GMP PlayGameplayMusicClientRpc] Selected clip at index {musicIndex} is null. Check the list.", this);
        }
    }
    
    // No OnDestroy needed here to save state, as gameplay music doesn't resume.
} 