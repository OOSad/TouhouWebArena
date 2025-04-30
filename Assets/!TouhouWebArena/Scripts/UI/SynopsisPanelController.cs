using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the display of character information on a synopsis panel (Character Select screen).
/// Takes data from a <see cref="CharacterSynopsisData"/> ScriptableObject and updates assigned UI elements.
/// Assumes child UI elements (Text, Images) are assigned in the Inspector.
/// </summary>
public class SynopsisPanelController : MonoBehaviour
{
    [Header("UI Element References")]
    [Tooltip("The main illustration/portrait for the character.")]
    [SerializeField] private Image illustrationImage;
    [Tooltip("Icon representing the Extra Attack.")]
    [SerializeField] private Image extraAttackIconImage;
    [Tooltip("Icon representing the Charge Attack.")]
    [SerializeField] private Image chargeAttackIconImage;

    [Tooltip("Text field for the character's title.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("Text field for the character's display name.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("Label for Normal Speed stat.")]
    [SerializeField] private TMP_Text normalSpeedLabelText; // e.g., "Normal Speed"
    [Tooltip("Value for Normal Speed stat (e.g., ★★★☆☆).")]
    [SerializeField] private TMP_Text normalSpeedValueText;
    [Tooltip("Label for Charge Speed stat.")]
    [SerializeField] private TMP_Text chargeSpeedLabelText; // e.g., "Charge Speed"
    [Tooltip("Value for Charge Speed stat (e.g., ★★★★☆).")]
    [SerializeField] private TMP_Text chargeSpeedValueText;
    [Tooltip("Label for Scope Style stat.")]
    [SerializeField] private TMP_Text scopeStyleLabelText;  // e.g., "Scope Style"
    [Tooltip("Value for Scope Style stat (e.g., Homing).")]
    [SerializeField] private TMP_Text scopeStyleValueText;
    [Tooltip("Label for Extra Attack.")]
    [SerializeField] private TMP_Text extraAttackLabelText;   // e.g., "Extra Attack"
    [Tooltip("Name of the Extra Attack.")]
    [SerializeField] private TMP_Text extraAttackNameText;
    [Tooltip("Label for Charge Attack.")]
    [SerializeField] private TMP_Text chargeAttackLabelText;  // e.g., "Charge Attack"
    [Tooltip("Name of the Charge Attack.")]
    [SerializeField] private TMP_Text chargeAttackNameText;
    [Tooltip("Descriptive text for the Extra Attack.")]
    [SerializeField] private TMP_Text extraAttackDescriptionText;
    [Tooltip("Descriptive text for the Charge Attack.")]
    [SerializeField] private TMP_Text chargeAttackDescriptionText;


    /// <summary>
    /// Updates all assigned UI elements on this panel using data from the provided ScriptableObject.
    /// If null data is provided, it attempts to clear the fields (sets text to empty, disables images).
    /// Handles mirroring the main illustration if specified.
    /// </summary>
    /// <param name="data">The <see cref="CharacterSynopsisData"/> containing the information to display. Can be null.</param>
    /// <param name="mirrorIllustration">If true, the illustration's RectTransform scale.x will be set to -1.</param>
    public void UpdateDisplay(CharacterSynopsisData data, bool mirrorIllustration = false)
    {
        // Check if data is provided
        bool hasData = data != null;

        // Update Basic Info
        if (illustrationImage != null)
        {
            illustrationImage.sprite = hasData ? data.characterIllustration : null;
            illustrationImage.enabled = hasData && data.characterIllustration != null;
            if (illustrationImage.enabled)
            {
                // Apply mirroring
                Vector3 scale = illustrationImage.rectTransform.localScale;
                scale.x = mirrorIllustration ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                illustrationImage.rectTransform.localScale = scale;
            }
        }
        if (titleText != null) titleText.text = hasData ? data.characterTitle : "";
        if (nameText != null) nameText.text = hasData ? data.displayName : "";

        // Update Stats (Using Labels for context if needed, but primarily updating values)
        // You might want to hide Labels too if there's no value, depending on desired look
        if (normalSpeedValueText != null) normalSpeedValueText.text = hasData ? data.normalSpeedStat : "";
        if (chargeSpeedValueText != null) chargeSpeedValueText.text = hasData ? data.chargeSpeedStat : "";
        if (scopeStyleValueText != null) scopeStyleValueText.text = hasData ? data.scopeStyleStat : "";

        // Update Attack Info
        if (extraAttackNameText != null) extraAttackNameText.text = hasData ? data.extraAttackName : "";
        if (extraAttackDescriptionText != null) extraAttackDescriptionText.text = hasData ? data.extraAttackDescription : "";
        if (extraAttackIconImage != null)
        {
            extraAttackIconImage.sprite = hasData ? data.extraAttackIcon : null;
            extraAttackIconImage.enabled = hasData && data.extraAttackIcon != null;
        }

        if (chargeAttackNameText != null) chargeAttackNameText.text = hasData ? data.chargeAttackName : "";
        if (chargeAttackDescriptionText != null) chargeAttackDescriptionText.text = hasData ? data.chargeAttackDescription : "";
        if (chargeAttackIconImage != null)
        {
            chargeAttackIconImage.sprite = hasData ? data.chargeAttackIcon : null;
            chargeAttackIconImage.enabled = hasData && data.chargeAttackIcon != null;
        }

        // Add final log to confirm execution and active state
        Debug.Log($"[SynopsisPanelController] UpdateDisplay finished for {(hasData ? data.displayName : "NULL data")}. Panel active in hierarchy: {gameObject.activeInHierarchy}", this);
    }

    // Removed ShowPanel() and HidePanel() methods as they just wrapped SetActive()

} 