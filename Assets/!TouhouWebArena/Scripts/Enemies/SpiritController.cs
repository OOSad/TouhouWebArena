    // --- IClearable Implementation ---
    /// <summary>
    /// [Server Only] Handles the spirit being cleared by effects like PlayerDeathBomb or Shockwave.
    /// Always triggers the Die sequence regardless of the forceClear flag.
    /// </summary>
    /// <param name="forceClear">Flag indicating if the clear is forced (e.g., by a bomb). Ignored by this implementation; spirits always die when cleared.</param>
    /// <param name="sourceRole">The role of the player causing the clear.</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {
        // ... implementation ...
    }
    // ------------------------------------

    // ... Other methods ...
} 