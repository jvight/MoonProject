using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>The keys the face screen's foot row names.</summary>
    internal enum FaceHint
    {
        Close = 0,
        Move = 1,
        Select = 2,
    }

    /// <summary>
    /// The face screen's key hints (pure, EditMode-tested): the keycap word of each hint's real control on the active
    /// device, as a localization key (<see cref="GlyphLabels.KeyPrefix"/> words), and the word that says what it does.
    /// </summary>
    internal static class FaceKeyHints
    {
        public const int Count = 3;

        /// <summary>The "glyph.&lt;control&gt;" key of the control for <paramref name="hint"/> on <paramref name="device"/>.</summary>
        public static string CapKey(FaceHint hint, InputDeviceKind device)
        {
            bool pad = device == InputDeviceKind.Gamepad;
            switch (hint)
            {
                case FaceHint.Close:
                    return pad ? "glyph.gamepad.select" : "glyph.keyboard.tab";
                case FaceHint.Move:
                    return pad ? "glyph.gamepad.dpad" : "glyph.keyboard.wasd";
                default:
                    return pad ? "glyph.gamepad.buttonsouth" : "glyph.keyboard.space";
            }
        }

        /// <summary>The localization key of the one word beside the keycap.</summary>
        public static string WordKey(FaceHint hint)
        {
            switch (hint)
            {
                case FaceHint.Close:
                    return UiKeys.FaceClose;
                case FaceHint.Move:
                    return UiKeys.FaceMove;
                default:
                    return UiKeys.FaceSelect;
            }
        }
    }
}
