using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouhouWebArena; // For PlayerRole

/// <summary>
/// Manages the display of spellcard activation banners for players.
/// Should be placed on a UI manager object that also has a NetworkObject component.
/// Listens for a ClientRpc from the server to show the appropriate banner.
/// </summary>
public class SpellcardBannerDisplay : NetworkBehaviour
{
    // --- Singleton Pattern ---
    public static SpellcardBannerDisplay Instance { get; private set; }
    // -----------------------

    [System.Serializable]
    public struct CharacterBannerInfo
    {
        public string characterInternalName; // Matches CharacterStats/PlayerData
        public Sprite bannerSprite;
    }

    [Header("Banner UI Elements")]
    [SerializeField] private Image player1BannerImage;
    [SerializeField] private Image player2BannerImage;
    [SerializeField] private float displayDuration = 1.5f;

    [Header("Character Banners")]
    [Tooltip("Map character internal names to their banner sprites.")]
    [SerializeField] private List<CharacterBannerInfo> characterBanners;

    private Dictionary<string, Sprite> bannerLookup;
    private Coroutine p1HideCoroutine;
    private Coroutine p2HideCoroutine;

    void Awake()
    {
        // --- Singleton Setup ---
        if (Instance != null && Instance != this)
        { 
            Debug.LogWarning($"Duplicate SpellcardBannerDisplay instance found. Destroying duplicate on {gameObject.name}. Ensure only one instance exists.", this);
            Destroy(gameObject);
            return; 
        }
        Instance = this;
        // Optional: DontDestroyOnLoad(gameObject); if it needs to persist across scenes
        // -----------------------

        // Build the dictionary for faster lookups
        bannerLookup = characterBanners.ToDictionary(info => info.characterInternalName, info => info.bannerSprite);

        // Ensure banners are hidden initially
        if (player1BannerImage != null) player1BannerImage.gameObject.SetActive(false);
        if (player2BannerImage != null) player2BannerImage.gameObject.SetActive(false);
    }

    // Called by the server via ClientRpc
    [ClientRpc]
    public void ShowBannerClientRpc(PlayerRole casterRole, string characterName, ClientRpcParams clientRpcParams = default)
    {
        // --- Safety Check (optional, RPCs should only run on clients) ---
        // if (!IsClient) return;
        // ---------------------------------------------------------------

        // Find the correct banner and sprite
        Image targetBanner = null;
        Coroutine existingCoroutine = null;

        if (casterRole == PlayerRole.Player1)
        {
            targetBanner = player1BannerImage;
            existingCoroutine = p1HideCoroutine;
        }
        else if (casterRole == PlayerRole.Player2)
        {
            targetBanner = player2BannerImage;
            existingCoroutine = p2HideCoroutine;
        }

        if (targetBanner == null)
        {
            Debug.LogError($"ShowBannerClientRpc: Target banner image for role {casterRole} is not assigned!", this);
            return;
        }

        if (bannerLookup.TryGetValue(characterName, out Sprite bannerSprite))
        {
            targetBanner.sprite = bannerSprite;
            targetBanner.gameObject.SetActive(true);

            // Stop any existing hide coroutine for this banner
            if (existingCoroutine != null)
            {
                StopCoroutine(existingCoroutine);
            }

            // Start new hide coroutine
            Coroutine newCoroutine = StartCoroutine(HideBannerAfterDelay(targetBanner));
            if (casterRole == PlayerRole.Player1) p1HideCoroutine = newCoroutine;
            else if (casterRole == PlayerRole.Player2) p2HideCoroutine = newCoroutine;
        }
        else
        {
            Debug.LogWarning($"ShowBannerClientRpc: No banner sprite found for character '{characterName}'", this);
            // Optionally show a default banner or do nothing
            targetBanner.gameObject.SetActive(false); // Ensure it's hidden if sprite not found
        }
    }

    private IEnumerator HideBannerAfterDelay(Image bannerImage)
    {
        yield return new WaitForSeconds(displayDuration);
        if (bannerImage != null)
        {
            bannerImage.gameObject.SetActive(false);
        }
        // Clear the stored coroutine reference
        if (bannerImage == player1BannerImage) p1HideCoroutine = null;
        else if (bannerImage == player2BannerImage) p2HideCoroutine = null;
    }
} 