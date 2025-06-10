using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Required for Image/CanvasGroup

public class ClientScreenWipeController : MonoBehaviour
{
    public static ClientScreenWipeController Instance { get; private set; }

    [Header("Screen Wipe Settings")]
    [Tooltip("The UI Image for Player 1's playspace wipe effect. Its alpha will be animated.")]
    [SerializeField] private Image player1WipeImage;
    [Tooltip("The UI Image for Player 2's playspace wipe effect. Its alpha will be animated.")]
    [SerializeField] private Image player2WipeImage;

    [Tooltip("CLIENT ANIMATION DURATION for wipe-in. The RoundManager's 'Server Player Reset Delay During Wipe' field MUST BE SET TO THIS VALUE for correct timing of player position reset.")]
    [SerializeField] private float wipeInDuration = 0.4f;

    [Tooltip("CLIENT ANIMATION DURATION for how long the screen stays fully covered by the wipe.")]
    [SerializeField] private float holdDuration = 0.2f;

    [Tooltip("CLIENT ANIMATION DURATION for wipe-out. The RoundManager's 'Screen Wipe Duration' should ideally be this + Wipe In + Hold Duration.")]
    [SerializeField] private float wipeOutDuration = 0.4f;

    private Coroutine _wipeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        bool setupError = false;
        if (player1WipeImage == null)
        {
            Debug.LogError("[ClientScreenWipeController] Player 1 Wipe Image is not assigned in the Inspector!", this);
            setupError = true;
        }
        if (player2WipeImage == null)
        {
            Debug.LogError("[ClientScreenWipeController] Player 2 Wipe Image is not assigned in the Inspector!", this);
            setupError = true;
        }

        if (setupError)
        {
            enabled = false; // Disable if not set up
            return;
        }

        // Ensure they are initially transparent and inactive
        InitializeImage(player1WipeImage);
        InitializeImage(player2WipeImage);
    }

    private void InitializeImage(Image imageToInit)
    {
        if (imageToInit != null)
        {
            Color tempColor = imageToInit.color;
            tempColor.a = 0;
            imageToInit.color = tempColor;
            imageToInit.gameObject.SetActive(false); // Start inactive
        }
    }

    /// <summary>
    /// Starts the screen wipe effect.
    /// The total duration of the wipe effect is determined by wipeInDuration + holdDuration + wipeOutDuration.
    /// </summary>
    public void StartWipeEffect()
    {
        if (player1WipeImage == null || player2WipeImage == null)
        {
            Debug.LogError("[ClientScreenWipeController] Cannot start wipe, one or both Wipe Images are not assigned.", this);
            return;
        }

        if (_wipeCoroutine != null)
        {
            StopCoroutine(_wipeCoroutine);
        }
        _wipeCoroutine = StartCoroutine(WipeAnimationCoroutine());
    }

    private IEnumerator WipeAnimationCoroutine()
    {
        Debug.Log("[ClientScreenWipeController] Starting Wipe In for both playspaces.");
        if(player1WipeImage != null) player1WipeImage.gameObject.SetActive(true);
        if(player2WipeImage != null) player2WipeImage.gameObject.SetActive(true);
        
        // --- Wipe In (Fade to Black/Opaque) ---
        float elapsedTime = 0f;
        // Assuming both images start with the same color properties for simplicity
        Color initialColorAlpha0 = player1WipeImage != null ? player1WipeImage.color : Color.black; // Fallback color
        initialColorAlpha0.a = 0f;
        Color targetColorAlpha1 = initialColorAlpha0;
        targetColorAlpha1.a = 1f;

        while (elapsedTime < wipeInDuration)
        {
            float alpha = elapsedTime / wipeInDuration;
            if (player1WipeImage != null) player1WipeImage.color = Color.Lerp(initialColorAlpha0, targetColorAlpha1, alpha);
            if (player2WipeImage != null) player2WipeImage.color = Color.Lerp(initialColorAlpha0, targetColorAlpha1, alpha);
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time if Time.timeScale might be 0
            yield return null;
        }
        if (player1WipeImage != null) player1WipeImage.color = targetColorAlpha1; // Ensure it's fully opaque
        if (player2WipeImage != null) player2WipeImage.color = targetColorAlpha1; // Ensure it's fully opaque
        Debug.Log("[ClientScreenWipeController] Wipe In Complete. Holding.");

        // --- Hold (Screen Stays Black/Opaque) ---
        if (holdDuration > 0)
        {
            yield return new WaitForSecondsRealtime(holdDuration); // Use unscaled time
        }
        Debug.Log("[ClientScreenWipeController] Hold Complete. Starting Wipe Out.");

        // --- Wipe Out (Fade to Transparent) ---
        elapsedTime = 0f;
        // initialColor is already targetColorAlpha1 (fully opaque)
        Color targetColorAlpha0 = targetColorAlpha1; // Start from opaque
        targetColorAlpha0.a = 0f; // Target transparent

        while (elapsedTime < wipeOutDuration)
        {
            float alpha = elapsedTime / wipeOutDuration;
            if (player1WipeImage != null) player1WipeImage.color = Color.Lerp(targetColorAlpha1, targetColorAlpha0, alpha);
            if (player2WipeImage != null) player2WipeImage.color = Color.Lerp(targetColorAlpha1, targetColorAlpha0, alpha);
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time
            yield return null;
        }
        if (player1WipeImage != null) player1WipeImage.color = targetColorAlpha0; // Ensure it's fully transparent
        if (player2WipeImage != null) player2WipeImage.color = targetColorAlpha0; // Ensure it's fully transparent
        
        if(player1WipeImage != null) player1WipeImage.gameObject.SetActive(false); // Hide it again
        if(player2WipeImage != null) player2WipeImage.gameObject.SetActive(false); // Hide it again
        Debug.Log("[ClientScreenWipeController] Wipe Out Complete.");
        _wipeCoroutine = null;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
} 