using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Shockwave : MonoBehaviour
{
    [Header("Expansion Settings")]
    [SerializeField] private float duration = 0.3f; // How long the shockwave lasts (Reverted)
    [SerializeField] private float maxRadius = 2.0f; // The final radius the collider/visual reaches
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Controls speed of expansion

    [Header("Target Tags")]
    [SerializeField] private string enemyBulletTag = "EnemyBullet"; // Make sure your enemy bullets have this tag!

    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    private float initialRadius;
    private Vector3 initialScale;
    private float startTime;
    private Color initialColor;
    private Color endColor;

    private Collider2D sourceCollider = null; // Optional: To prevent hitting the fairy that spawned it

    void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        initialRadius = circleCollider.radius; // Store the initial radius set in the prefab
        initialScale = transform.localScale;
        initialColor = spriteRenderer.color;
        endColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f); // Fade out alpha
    }

    void Start()
    {
        startTime = Time.time;
        StartCoroutine(ExpandAndFade());
    }

    // Optional: Call this immediately after instantiating if you want to prevent self-collision
    public void SetSourceCollider(Collider2D source)
    {
        sourceCollider = source;
    }

    private IEnumerator ExpandAndFade()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float curveProgress = expansionCurve.Evaluate(progress); // Use curve for non-linear expansion

            // Scale collider radius
            circleCollider.radius = Mathf.Lerp(initialRadius, maxRadius, curveProgress);

            // Scale visual (assuming uniform scaling)
            // Calculate scale factor needed to match collider radius if sprite is unit size
            // If sprite base size isn't 1 unit diameter, adjust calculation
            float scaleFactor = circleCollider.radius / (initialRadius / initialScale.x); // Assumes uniform initial scale
            transform.localScale = new Vector3(scaleFactor, scaleFactor, initialScale.z);

            // Fade out sprite
            spriteRenderer.color = Color.Lerp(initialColor, endColor, progress); 

            yield return null; // Wait for next frame
        }

        // Ensure final state and destroy
        circleCollider.radius = maxRadius;
        float finalScaleFactor = maxRadius / (initialRadius / initialScale.x);
        transform.localScale = new Vector3(finalScaleFactor, finalScaleFactor, initialScale.z);
        spriteRenderer.color = endColor;

        Destroy(gameObject);
    }

    // Shockwave collision logic runs locally on all clients where it exists
    void OnTriggerEnter2D(Collider2D other)
    {
        // Optional: Prevent hitting the source fairy
        if (sourceCollider != null && other == sourceCollider)
        {
            return;
        }

        // Check for enemy bullets
        if (!string.IsNullOrEmpty(enemyBulletTag) && other.CompareTag(enemyBulletTag))
        {
            NetworkObject bulletNetworkObject = other.GetComponent<NetworkObject>();
            if (bulletNetworkObject != null)
            {
                // Request the server destroy this bullet
                // We need a way to send this request. A dedicated NetworkManager script
                // or making the shockwave temporarily network-aware might be needed.
                // Simpler temporary solution: Assume bullets hit by shockwaves aren't networked
                // or handle their destruction via another mechanism (e.g., lifetime).
                // For now, just destroy locally if NOT networked.
                if (!bulletNetworkObject.IsSpawned) Destroy(other.gameObject);
                else Debug.LogWarning("Shockwave hit networked bullet - destruction needs server request.");
            }
            else
            {
                // Destroy non-networked bullets locally
                Destroy(other.gameObject);
            }
        }
        // Check for other fairies - REMOVED BLOCK
        // else if (!string.IsNullOrEmpty(fairyTag) && other.CompareTag(fairyTag))
        // {
        //     // Try to get the Fairy component (which is now a NetworkBehaviour)
        //     Fairy fairy = other.GetComponent<Fairy>();
        //     if (fairy != null)
        //     {
        //         // Request the fairy takes damage via its ServerRpc
        //         // fairy.RequestDamage(fairyDamage); // Old direct call

        //         // NEW: Start a coroutine to apply damage after a delay
        //         // StartCoroutine(ApplyDamageAfterDelay(fairy, 0.15f)); // Reverted delay (must be < duration)
        //     }
        // }
    }

    // NEW COROUTINE - REMOVED BLOCK
    // private IEnumerator ApplyDamageAfterDelay(Fairy targetFairy, float delay)
    // {
    //     // --- Logging Start ---
    //     GameObject targetGo = targetFairy != null ? targetFairy.gameObject : null; // Store initial GO ref
    //     ulong targetNetId = targetFairy != null ? targetFairy.NetworkObjectId : 0; // Store initial NetId
    //     int shockwaveInstanceId = gameObject.GetInstanceID(); // ID of this shockwave
    //     Debug.Log($"[Shockwave {shockwaveInstanceId}] Starting ApplyDamageAfterDelay for Fairy NetId:{targetNetId}. Delay: {delay}s");
    //     // --- Logging End ---

    //     yield return new WaitForSeconds(delay);

    //     // --- Logging Start ---
    //     Debug.Log($"[Shockwave {shockwaveInstanceId}] Finished waiting {delay}s for Fairy NetId:{targetNetId}. Checking target validity...");
    //     // --- Logging End ---

    //     // Check if the target fairy still exists before applying damage
    //     // Check both the component reference AND the original GameObject reference
    //     bool targetIsValid = targetFairy != null && targetGo != null; 

    //     if (targetIsValid)
    //     {
    //         Debug.Log($"[Shockwave {shockwaveInstanceId}] Target Fairy NetId:{targetNetId} is valid. Requesting damage."); // Existing log
    //         // ---- ADD THIS LOG ----
    //         Debug.Log($"[Shockwave {shockwaveInstanceId}] >>> Calling RequestDamage on Fairy NetId:{targetNetId}");
    //         // ----------------------
    //         targetFairy.RequestDamage(fairyDamage);
    //     }
    //     else 
    //     {
    //         Debug.LogWarning($"[Shockwave {shockwaveInstanceId}] Target Fairy NetId:{targetNetId} is NO LONGER VALID after {delay}s delay. Damage not applied.");
    //     }
    // }
} 