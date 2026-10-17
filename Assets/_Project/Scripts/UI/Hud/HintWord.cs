namespace MoonProject.UI
{
    /// <summary>
    /// The word in the low look-hint chip (<see cref="LookUpChip"/>): the opening's first prompt
    /// (<see cref="FirstPrompt"/>), "Look up" or "Drive", or the Tab chip (<see cref="HoldHint"/>), "Hold".
    /// </summary>
    internal enum HintWord
    {
        None = 0,
        LookUp = 1,
        Drive = 2,
        Hold = 3,
    }
}
