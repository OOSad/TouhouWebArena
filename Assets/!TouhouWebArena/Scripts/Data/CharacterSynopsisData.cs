using UnityEngine;

/// <summary>
/// ScriptableObject holding the display information for a character
/// on the Character Select screen's synopsis panel.
/// Contains fields for names, stats, attack details, and visual assets.
/// Referenced by <see cref="CharacterSelector"/> and used by <see cref="SynopsisPanelController"/>.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterSynopsis", menuName = "TouhouWebArena/Character Synopsis Data")]
public class CharacterSynopsisData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("The display name of the character shown in the UI (e.g., Reimu Hakurei).")]
    public string displayName = "Character Name";

    [Tooltip("The character's title shown in the UI (e.g., Wonderful Shrine Maiden of Paradise).")]
    public string characterTitle = "Character Title";

    [Tooltip("The main illustration/portrait displayed on the synopsis panel.")]
    public Sprite characterIllustration;

    [Tooltip("The unique identifier name matching CharacterStats.characterName (e.g., HakureiReimu). Crucial for linking data.")]
    public string internalName = "CharacterInternalName";


    [Header("Stats Display (Text/Ratings)")]
    [Tooltip("Text or star rating (e.g., ★★★☆☆) for Normal Speed.")]
    public string normalSpeedStat = "★★★☆☆";

    [Tooltip("Text or star rating (e.g., ★★★☆☆) for Charge Speed.")]
    public string chargeSpeedStat = "★★★★★";

    [Tooltip("Text description of the Scope Style (e.g., Homing, Wide).")]
    public string scopeStyleStat = "Large Circle";

    [Tooltip("Text or star rating (e.g., ★★★☆☆) for Focused Speed.")]
    public string focusedSpeedStat = "★☆☆☆☆";

    [Tooltip("Text or star rating (e.g., ★★★☆☆) for Scope Speed.")]
    public string scopeSpeedStat = "★★★☆☆";

    [Tooltip("Text description of the character's unique Special Ability/Passive.")]
    public string specialAbilityStat = "Special Ability Description";


    [Header("Attack Info Display")]
    [Tooltip("Display name of the Extra Attack.")]
    public string extraAttackName = "Extra Attack Name";
    [Tooltip("Icon representing the Extra Attack.")]
    public Sprite extraAttackIcon;
    [Tooltip("Short description of the Extra Attack for the synopsis panel.")]
    [TextArea(3, 5)]
    public string extraAttackDescription = "Extra Attack Description";

    [Tooltip("Display name of the Charge Attack.")]
    public string chargeAttackName = "Charge Attack Name";
    [Tooltip("Icon representing the Charge Attack.")]
    public Sprite chargeAttackIcon;
    [Tooltip("Short description of the Charge Attack for the synopsis panel.")]
    [TextArea(3, 5)]
    public string chargeAttackDescription = "Charge Attack Description";

    // Removed Spellcard Info Section

    // Removed Voice Lines Section

    // Add any other UI-specific fields needed here
} 