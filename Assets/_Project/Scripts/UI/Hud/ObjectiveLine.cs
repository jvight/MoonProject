using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The objective line's rule (pure, EditMode-tested; docs/design/opening-v3-guidance-audit.md G2, G4). Gameplay says
    /// what the line is and how long it stays (<see cref="NextStepChanged"/>): the next small action right after a
    /// success, held briefly, or a reminder after a stall; an empty key takes it away. The line wants the screen the
    /// moment it is given, for as long as Gameplay keeps it, with its key's glyph first when the key is new. The
    /// player's Guidance setting turns it off.
    /// </summary>
    internal sealed class ObjectiveLine
    {
        /// <summary>The line's localization key; empty when there is nothing to say.</summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>Why the line shows: the next action after a success, or a reminder after a stall.</summary>
        public NextLineReason Reason { get; private set; }

        /// <summary>The key the line asks for, whose glyph it may draw first.</summary>
        public RoverAction Action { get; private set; }

        /// <summary>The line draws its key's glyph first (the key is new, or not used for a while).</summary>
        public bool ShowsKey { get; private set; }

        /// <summary>The line wants the screen (before the guidance slot decides).</summary>
        public bool WantsShown { get; private set; }

        /// <summary>The line is the next action after a success: the one element until it goes.</summary>
        public bool IsNext => WantsShown && Reason == NextLineReason.Next;

        /// <summary>Gameplay gave a new line, or took the line away (an empty key).</summary>
        public void Set(NextStepChanged changed)
        {
            Key = changed.LineKey ?? string.Empty;
            Reason = changed.Reason;
            Action = changed.Key;
            ShowsKey = Key.Length > 0 && changed.ShowsKey;
        }

        /// <param name="enabled">The player's Guidance setting.</param>
        public void Tick(bool enabled)
        {
            WantsShown = enabled && Key.Length > 0;
        }
    }
}
