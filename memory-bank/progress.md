# Progress
 
- Memory bank initialized based on provided documentation. 
- Implemented fixes for Lily White's stage bullet issues:
    - Bullets now return to the pool upon hitting StageWalls.
    - Bullets fired by Lily White are now clearable by opposing player shockwaves (both spellcard and fairy death) due to corrected clearing logic and dynamic bullet owner role assignment.

**What's Left:**
- Thoroughly test Lily White's behavior with both players to confirm all clearing mechanisms work as expected.
- Continue implementing remaining enemy behaviors and patterns. 