using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>The keys the build menu's foot row names.</summary>
    internal enum BuildHint
    {
        /// <summary>Pick a card: a click or the place key.</summary>
        Pick = 0,

        /// <summary>Leave build mode: the build key again or back.</summary>
        Leave = 1,
    }

    /// <summary>
    /// The build menu's key hints (pure, EditMode-tested), the way <see cref="FaceKeyHints"/> names the face's: the
    /// keycap word of each hint's real controls on the active device, as a localization key, and the word that says
    /// what it does.
    /// </summary>
    internal static class BuildKeyHints
    {
        public const int Count = 2;

        /// <summary>The localization key of the keycap for <paramref name="hint"/> on <paramref name="device"/>.</summary>
        public static string CapKey(BuildHint hint, InputDeviceKind device)
        {
            bool pad = device == InputDeviceKind.Gamepad;
            switch (hint)
            {
                case BuildHint.Pick:
                    return pad ? "glyph.gamepad.buttonwest" : UiKeys.KeyMouseOrE;
                default:
                    return pad ? UiKeys.KeyPadYOrB : UiKeys.KeyBOrEscape;
            }
        }

        /// <summary>The localization key of the one word beside the keycap.</summary>
        public static string WordKey(BuildHint hint)
        {
            return hint == BuildHint.Pick ? UiKeys.BuildHintPick : UiKeys.BuildHintLeave;
        }
    }
}
