using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>The keys the face screen's foot row names.</summary>
    internal enum FaceHint
    {
        /// <summary>Fit a part into a device: drag it there, or select and press the place key.</summary>
        Fit = 0,

        Close = 1,

        /// <summary>Close the screen and open the build menu.</summary>
        Build = 2,

        /// <summary>Flip the face to the databank page (Q / E).</summary>
        Databank = 3,
    }

    /// <summary>
    /// The face screen's key hints (pure, EditMode-tested): the keycap word of each hint's real control on the active
    /// device, as a localization key (<see cref="GlyphLabels.KeyPrefix"/> words), and the word that says what it does.
    /// </summary>
    internal static class FaceKeyHints
    {
        public const int Count = 4;

        /// <summary>The localization key of the keycap for <paramref name="hint"/> on <paramref name="device"/>.</summary>
        public static string CapKey(FaceHint hint, InputDeviceKind device)
        {
            bool pad = device == InputDeviceKind.Gamepad;
            switch (hint)
            {
                case FaceHint.Fit:
                    return pad ? "glyph.gamepad.buttonsouth" : UiKeys.KeyDragOrEnter;
                case FaceHint.Close:
                    return pad ? "glyph.gamepad.select" : "glyph.keyboard.tab";
                case FaceHint.Build:
                    return pad ? "glyph.gamepad.buttonnorth" : UiKeys.KeyB;
                default:
                    return pad ? UiKeys.KeyShoulders : UiKeys.KeyQe;
            }
        }

        /// <summary>The localization key of the one word beside the keycap.</summary>
        public static string WordKey(FaceHint hint)
        {
            switch (hint)
            {
                case FaceHint.Fit:
                    return UiKeys.FaceFit;
                case FaceHint.Close:
                    return UiKeys.FaceClose;
                case FaceHint.Build:
                    return UiKeys.FaceBuild;
                default:
                    return DatabankText.TitleKey;
            }
        }
    }
}
