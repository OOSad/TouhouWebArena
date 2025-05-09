using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the display of character information on a synopsis panel.
/// Takes data from a CharacterSynopsisData ScriptableObject and updates UI elements.
/// </summary>
public class SynopsisPanelController : MonoBehaviour
{
    [Header("UI Element References")]
    [SerializeField] private Image illustrationImage; // Renamed from "Image" for clarity
    [SerializeField] private Image extraAttackIconImage;
    [SerializeField] private Image chargeAttackIconImage;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text nameText;

    [SerializeField] private TMP_Text normalSpeedLabelText;
    [SerializeField] private TMP_Text normalSpeedValueText;
    [SerializeField] private TMP_Text chargeSpeedLabelText;
    [SerializeField] private TMP_Text chargeSpeedValueText;
    [SerializeField] private TMP_Text scopeStyleLabelText;
    [SerializeField] private TMP_Text scopeStyleValueText;
    [SerializeField] private TMP_Text focusedSpeedLabelText;
    [SerializeField] private TMP_Text focusedSpeedValueText;
    [SerializeField] private TMP_Text scopeSpeedLabelText;
    [SerializeField] private TMP_Text scopeSpeedValueText;
    [SerializeField] private TMP_Text specialAbilityLabelText;
    [SerializeField] private TMP_Text specialAbilityValueText;

    [SerializeField] private TMP_Text extraAttackLabelText;
    [SerializeField] private TMP_Text extraAttackNameText;
    [SerializeField] private TMP_Text extraAttackDescriptionText;

    [SerializeField] private TMP_Text chargeAttackLabelText;
    [SerializeField] private TMP_Text chargeAttackNameText;
    [SerializeField] private TMP_Text chargeAttackDescriptionText;

    /// <summary>
    /// Updates all the UI elements on the panel with data from the provided ScriptableObject.
    /// </summary>
    /// <param name="data">The CharacterSynopsisData to display.</param>
    /// <param name="role">The role of the player (default: None).</param>
    public void UpdateDisplay(CharacterSynopsisData data, PlayerRole role = PlayerRole.None)
    {
        if (data == null)
        {
            // Debug.LogWarning("[SynopsisPanelController] UpdateDisplay called with null data.");
            gameObject.SetActive(false); // Hide if no data
            return;
        }

        gameObject.SetActive(true); // Ensure panel is active if data is provided

        // Update Text Fields
        if (titleText) titleText.text = data.characterTitle;
        if (nameText) nameText.text = data.displayName;

        // Assuming labels don't change, only update value fields
        if (normalSpeedValueText) normalSpeedValueText.text = data.normalSpeedStat;
        if (chargeSpeedValueText) chargeSpeedValueText.text = data.chargeSpeedStat;
        if (scopeStyleValueText) scopeStyleValueText.text = data.scopeStyleStat;
        if (focusedSpeedValueText) focusedSpeedValueText.text = data.focusedSpeedStat;
        if (scopeSpeedValueText) scopeSpeedValueText.text = data.scopeSpeedStat;
        if (specialAbilityValueText) specialAbilityValueText.text = data.specialAbilityStat;

        if (extraAttackNameText) extraAttackNameText.text = data.extraAttackName;
        if (extraAttackDescriptionText) extraAttackDescriptionText.text = data.extraAttackDescription;

        if (chargeAttackNameText) chargeAttackNameText.text = data.chargeAttackName;
        if (chargeAttackDescriptionText) chargeAttackDescriptionText.text = data.chargeAttackDescription;

        // Update Images (with null checks for sprites)
        if (illustrationImage)
        {
            illustrationImage.sprite = data.characterIllustration;
            illustrationImage.enabled = (data.characterIllustration != null); // Hide image if no sprite
        }

        if (extraAttackIconImage)
        {
            extraAttackIconImage.sprite = data.extraAttackIcon;
            extraAttackIconImage.enabled = (data.extraAttackIcon != null);
        }

        if (chargeAttackIconImage)
        {
            chargeAttackIconImage.sprite = data.chargeAttackIcon;
            chargeAttackIconImage.enabled = (data.chargeAttackIcon != null);
        }
        
        // Add final log to confirm execution and active state
        // Debug.Log($"[SynopsisPanelController] UpdateDisplay finished for {(data != null ? data.displayName : "NULL data")}. Panel active in hierarchy: {gameObject.activeInHierarchy}", this);
    }

    // Removed ShowPanel() and HidePanel() methods as they just wrapped SetActive()

} 