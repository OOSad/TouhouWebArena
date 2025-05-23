# Kirisame Marisa Spellcard Details

This document outlines the specific spellcard implementations for Kirisame Marisa.

## Level 2: Magic Sign "Stardust"

*   **Asset:** `KirisameMarisaLevel2Spellcard.asset`
*   **Description:** Six lines of eight bullets, evenly spaced out, appear on the outer edge of the opponent's screen. The bullets travel diagonally downwards, and each bullet is slightly faster than the previous one.

## Level 3: Magic Sign "Stardust Reverie"

*   **Asset:** `KirisameMarisaLevel3Spellcard.asset`
*   **Description:** Similar to the Level 2 spellcard, but with a different bullet pattern: instead of six lines of bullets, there are three lines of bullets on each side of the opponent's field. The bullets travel diagonally downwards, and each bullet is slightly faster than the previous one.

## Level 4: Magic Sign "Illusion Star"

*   **Asset:** `KirisameMarisaLevel4Spellcard.asset`
*   **Illusion Prefab:** `KirisameMarisaIllusion.prefab` 
*   **Attack Pool:** Contains several `CompositeAttackPattern`s representing different phases or attacks used by the illusion.
    *   **Orient Pattern Towards Target: True for Four Lines Of Twenty Bullets, False for all other patterns**
    *   **Perform Movement During Attack: True for Four Lines Of Twenty Bullets, False for all other patterns**
    *   **Pattern: "Four Lines Of Twenty Bullets"**
        *   Movement: Performs short horizontal movement during the attack.
        *   Actions: 20 sequential `SpellcardAction`s firing lines of 4 star bullets aimed at the player, using `intraActionDelay` for rapid fire. This isn't exactly the same as the original yet.

    *   **Pattern: "Eleven Circles Of Stars And Circles"**
        *   Movement: None.
        *   Actions: 11 simultaneous `SpellcardAction`s creating circles of alternating star/circle bullets. Uses `skipEveryNth = 4` for gaps and varying `angle` to rotate circles. This isn't exactly the same as the original yet, as the original used just two `SpellcardAction`s to create the circles, and the circles were created by spiraling in on themselves.

    *   **Pattern: "Yellow Star Barrage"**
        *   Movement: None.
        *   Actions: 1 `SpellcardAction` firing a large number (`Count`) of star bullets using `Point` formation, `intraActionDelay` for rapid fire, and the `DelayedRandomTurn` behavior with a wide `spreadAngle`.

        **Pattern: "Two Green Lasers"**
        *   Movement: None.
        *   Actions: Two lasers similar to Marisa's Extra Attacks (Earthlight Ray) appear on two preset locations on the opponent's side of the field. The lasers are always perfectly straight and last double the amount of time as normal Extra Attack lasers. This is not implemented yet, as the Extra Attack system does not play well with the spellcard system yet.

        **Pattern: "Earthlight Ray Barrage"**
        *   Movement: None.
        *   Actions: Many Extra Attack lasers (between 5 and 7) are triggered at once. This is not implemented yet, as the Extra Attack system does not play well with the spellcard system yet.

*   **Key Behaviors Used:**
    *   `DelayedRandomTurn` 