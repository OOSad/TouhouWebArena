# Hakurei Reimu Spellcard Details

This document outlines the specific spellcard implementations for Hakurei Reimu.

## Level 2: Spirit Sign "Yin-Yang Sign"

*   **Asset:** `HakureiReimuLevel2Spellcard.asset`
*   **Description:** Two circles of alternating circle bullets and oval bullets appear. The outer circle bullets simply go forward relative to their heading. The inner circle bullets home in on the opponent's last known position after a short delay. 

## Level 3: Spirit Sign "Fantasy Seal"

*   **Asset:** `HakureiReimuLevel3Spellcard.asset`
*   **Description:** Two circles of talisman bullets appear. The outer circle talismans simply go forward relative to their heading. The inner circle talismans home in on the opponent's last known position after a short delay, and then home again after travelling a certain distance.

## Level 4: Spirit Sign "Hakurei Illusion"

*   **Asset:** `HakureiReimuLevel4Spellcard.asset`
*   **Illusion Prefab:** HakureiReimuIllusion.prefab
*   **Key Behaviors:** Utilizes the `DoubleHoming` bullet behavior for its homing talismans.
*   **Attack Pool:**
    *   **Orient Pattern Towards Target: True for Claw with Talismans, False for all other patterns**
        **Perform Movement During Attack: False for all patterns**
        **Pattern: "Claw with Talismans"**
        *   Actions: Three lines of circle bullets appear, aimed at the opponent player. The first line is a straight line relative to the player, and the second and third have a 20 degree and -20 degree offset relative to the player, giving the appearance of a "claw". There is additionally a line of talisman bullets superimposed on the first line, also travelling straight, but at a different speed than the circle bullets. 

        **Pattern: "Criss-Cross Circles"**
        *   Actions: Two circles of circle bullets appear from the center of the illusion. The circles expand outwards at a fixed speed. Additionally, the circles rotate around the radius of the circle in different directions, giving the illusion of a criss-cross pattern. 

        **Pattern: "Three Circles of Talismans"**
        *   Actions: Three circles of talismans appear from the center of the illusion. The circles expand outwards at a fixed speed. 


        **Pattern: "Two Homing Circles of Talismans"**
        *   Actions: Two circles of talismans appear from the center of the illusion. The circles travel a short distance and then home in on the opponent's last known position.

        **Pattern: "Yin Yang Orb Shower"**
        *   Actions: A shower of around 5 to 7 Yin Yang orbs appear from the area around the illusion. They travel upwards at a random direction, left or right, almost as if tossed up a short distance. They then fall down towards the bottom of the screen. This pattern is not implemented yet, as it was deemed that the Extra Attack system does not play well with the spellcard system yet.


       