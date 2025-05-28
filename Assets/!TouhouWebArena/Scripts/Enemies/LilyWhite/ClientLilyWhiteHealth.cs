using UnityEngine;
using System.Collections;

public class ClientLilyWhiteHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 75;

    [Header("Damage Flash")]
    [Tooltip("The color the sprite flashes when taking damage.")]
    [SerializeField] private Color _flashColor = Color.white; // Lily is white, so flashing red might be better
    [Tooltip("How long the flash color lasts in seconds.")]
    [SerializeField] private float _flashDuration = 0.1f;
    [Tooltip("How strong the flash color tint is (0=no tint, 1=full color).")]
    [Range(0f, 1f)] [SerializeField] private float _flashIntensity = 0.7f;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _flashCoroutine;
    private ClientLilyWhiteController _lilyWhiteController;

    private int _currentHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogWarning($"[ClientLilyWhiteHealth on {gameObject.name}] SpriteRenderer not found in children.");
        }

        _lilyWhiteController = GetComponent<ClientLilyWhiteController>();
        if (_lilyWhiteController == null)
        {
            Debug.LogError($"[ClientLilyWhiteHealth on {gameObject.name}] ClientLilyWhiteController not found on this GameObject.");
        }
    }

    void OnEnable()
    {
        _currentHealth = maxHealth;
        if (_spriteRenderer != null) _spriteRenderer.color = Color.white; // Assuming default is white
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
    }

    public void Initialize() // Simple init for now
    {
        _currentHealth = maxHealth;
         if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
    }

    public void TakeDamage(int amount, ulong attackerOwnerClientId)
    {
        if (!IsAlive || _lilyWhiteController == null) return;

        FlashEffect();

        _currentHealth -= amount;
        // Debug.Log($"[ClientLilyWhiteHealth] {gameObject.name} took {amount} damage from Client {attackerOwnerClientId}, health is now {_currentHealth}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            // Notify controller to handle despawn
            _lilyWhiteController.HandleDeath(); 
        }
    }
    
    private void FlashEffect()
    {
        if (_spriteRenderer == null || !gameObject.activeInHierarchy) return;

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
             // Ensure color is reset if interrupted, consider original color if not always white
            _spriteRenderer.color = Color.white; 
        }
        _flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        Color originalColor = Color.white; // Assuming Lily's default sprite color is pure white
        if(_spriteRenderer != null) originalColor = _spriteRenderer.color;

        Color targetFlashColor = Color.Lerp(originalColor, _flashColor, _flashIntensity);
        if(_spriteRenderer != null) _spriteRenderer.color = targetFlashColor;

        yield return new WaitForSeconds(_flashDuration);

        if(_spriteRenderer != null) _spriteRenderer.color = originalColor; // Reset to original color
        _flashCoroutine = null;
    }
    
    // Called by spellcard/bomb clear effects if Lily White should be instantly despawned by them
    public void ForceReturnToPoolByClear() 
    {
        if (!IsAlive || _lilyWhiteController == null) return;
        
        // Debug.Log($"[ClientLilyWhiteHealth] {gameObject.name} ForceReturnedToPoolByClear.");
        _currentHealth = 0; // Mark as dead
        _lilyWhiteController.HandleDeath(); // Tell controller to despawn
    }
} 