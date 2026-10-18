namespace MoonProject.UI
{
    /// <summary>
    /// The word in the low look-hint chip (<see cref="LookUpChip"/>): the opening's first prompt
    /// (<see cref="FirstPrompt"/>), "Look up" or "Drive", or the Tab chip (<see cref="HoldHint"/>), "Hold"; or one of the
    /// objective's verb chips (<see cref="ObjectiveChip"/>).
    /// </summary>
    internal enum HintWord
    {
        None = 0,
        LookUp = 1,
        Drive = 2,
        Hold = 3,

        /// <summary>The objective's chips: the key of the verb the current step asks for.</summary>
        Scan = 4,
        Cut = 5,
        Build = 6,
        Fit = 7,
    }
}
