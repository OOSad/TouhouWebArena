using UnityEngine;

/// <summary>
/// ScriptableObject holding the display information for a character
/// on the Character Select screen.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterSynopsis", menuName = "TouhouWebArena/Character Synopsis Data")]
public class CharacterSynopsisData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("The display name of the character (e.g., Reimu Hakurei).")]
    public string displayName = "Character Name";

    [Tooltip("The character's title (e.g., Wonderful Shrine Maiden of Paradise).")]
    public string characterTitle = "Character Title";

    [Tooltip("The main illustration/portrait for this character.")]
    public Sprite characterIllustration;

    [Tooltip("The identifier name matching CharacterStats.characterName (e.g., HakureiReimu). Used for linking.")]
    public string internalName = "CharacterInternalName";


    [Header("Stats Display")]
    // Using strings for flexibility, could use enums or ints for star ratings later
    [Tooltip("Display text/rating for Normal Speed.")]
    public string normalSpeedStat = "★★★☆☆";

    [Tooltip("Display text/rating for Charge Speed.")]
    public string chargeSpeedStat = "★★★★★";

    [Tooltip("Display text for Scope Style.")]
    public string scopeStyleStat = "Large Circle";

    [Tooltip("Display text/rating for Focused Speed.")]
    public string focusedSpeedStat = "★☆☆☆☆";

    [Tooltip("Display text/rating for Scope Speed.")]
    public string scopeSpeedStat = "★★★☆☆";

    [Tooltip("Display text for the character's unique Special Ability.")]
    public string specialAbilityStat = "Special Ability Description";


    [Header("Attack Info")]
    [Tooltip("Name of the Extra Attack.")]
    public string extraAttackName = "Extra Attack Name";
    [Tooltip("Icon representing the Extra Attack.")]
    public Sprite extraAttackIcon;
    [Tooltip("Short description of the Extra Attack.")]
    [TextArea(3, 5)]
    public string extraAttackDescription = "Extra Attack Description";

    [Tooltip("Name of the Charge Attack.")]
    public string chargeAttackName = "Charge Attack Name";
    [Tooltip("Icon representing the Charge Attack.")]
    public Sprite chargeAttackIcon;
    [Tooltip("Short description of the Charge Attack.")]
    [TextArea(3, 5)]
    public string chargeAttackDescription = "Charge Attack Description";

    // Add any other UI-specific fields needed here
} 