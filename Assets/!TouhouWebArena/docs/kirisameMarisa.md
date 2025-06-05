# Kirisame Marisa Overview

This document outlines the specific spellcard implementations and general abilities for Kirisame Marisa.

## Basic Abilities and Controls

Kirisame Marisa's basic actions are controlled via the standard player input scheme, primarily managed by the `PlayerShootingController.cs` component on her prefab.

*   **Movement:** Standard arrow key movement, with focus mode (Left Shift) for slower, more precise movement and hitbox display.
*   **Firing (Z Key):**
    *   **Tap Z:** Fires a quick burst of star-shaped bullet pairs.
    *   **Hold Z:** Fires a continuous stream of star-shaped bullet pairs.
    *   **Sound:** A sound effect (`playerFireSound` in `PlayerShootingController`) plays for each shot fired by the local player.
*   **Spellcard Charging & Activation (X Key):**
    *   **Hold X:** Charges Marisa's spellcard gauge.
    *   **Release X:** Activates a Charge Attack (Level 1) or her currently equipped Spellcard (Level 2+) based on the charge level.
*   **Bullet Impact Sound:** When Marisa's star bullets hit an enemy, a sound effect (`playerBulletHitEnemySound` in `PlayerShootingController`, played via `BulletMovement.cs`) is triggered for the local player.

## Spellcard Details

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