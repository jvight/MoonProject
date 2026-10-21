namespace MoonProject.Core
{
    /// <summary>Why the objective line shows (docs/design/opening-v3-guidance-audit.md G2, G4).</summary>
    public enum NextLineReason
    {
        /// <summary>Right after a success: the next small action, held briefly (G2).</summary>
        Next = 0,

        /// <summary>A safety net: no progress for a while, so the current step's line comes back (G4).</summary>
        Reminder = 1,
    }
}
