using UnityEngine.UIElements;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// Reads what the player reads off the screen, by element name from the game's UIDocument: the objective line,
    /// the prompt chip (its key cap and word) and whether the title is up.
    /// </summary>
    public sealed class UiProbe
    {
        private const float VisibleOpacity = 0.05f;

        public const string ObjectiveLineName = "next-line-text";
        public const string FaceDevicePrefix = "seven-device-";

        private readonly VisualElement _root;
        private VisualElement _face;

        public UiProbe(VisualElement root, VisualElement face)
        {
            _root = root;
            _face = face;
        }

        /// <summary>Gives the probe the face screen's tree once the HUD has built it.</summary>
        public void UseFace(VisualElement face)
        {
            _face = face ?? _face;
        }

        /// <summary>True while the build strip (build mode's menu and placing) shows.</summary>
        public bool BuildStripShown()
        {
            return IsShown(_root.Q<VisualElement>("build-strip"));
        }

        /// <summary>The name of the face screen's focused element, or empty.</summary>
        public string FaceFocus()
        {
            return (_face?.panel?.focusController?.focusedElement as VisualElement)?.name ?? string.Empty;
        }

        /// <summary>The device index of the face screen's focused bubble, or -1 when the focus is elsewhere.</summary>
        public int FaceFocusedDevice()
        {
            string focus = FaceFocus();
            return focus.StartsWith(FaceDevicePrefix, System.StringComparison.Ordinal) &&
                   int.TryParse(focus.Substring(FaceDevicePrefix.Length), out int device)
                ? device
                : -1;
        }

        /// <summary>The objective line when it is showing, else empty.</summary>
        public string ObjectiveLine()
        {
            return TextOf("next-line", "next-line-text");
        }

        /// <summary>The key cap on the prompt chip when it is showing, else empty.</summary>
        public string ChipGlyph()
        {
            return TextOf("prompt", "prompt-glyph-label");
        }

        /// <summary>The key cap text the chip carries whether or not it is showing.</summary>
        public string RawChipGlyph()
        {
            Label glyph = _root.Q<Label>("prompt-glyph-label");
            return glyph != null && glyph.text != null ? glyph.text : string.Empty;
        }

        /// <summary>The word on the prompt chip when it is showing, else empty.</summary>
        public string ChipWord()
        {
            return TextOf("prompt", "prompt-word");
        }

        /// <summary>"E Scan" for a chip showing the key cap E and the word Scan; empty when no chip shows.</summary>
        public string Chip()
        {
            string glyph = ChipGlyph();
            string word = ChipWord();
            return string.IsNullOrEmpty(glyph) && string.IsNullOrEmpty(word) ? string.Empty : (glyph + " " + word).Trim();
        }

        /// <summary>What the chip's elements say whether or not they show, for a failure message.</summary>
        public string DescribeChip()
        {
            VisualElement chip = _root.Q<VisualElement>("prompt");
            Label glyph = _root.Q<Label>("prompt-glyph-label");
            Label word = _root.Q<Label>("prompt-word");
            if (chip == null)
            {
                return "the HUD has no element named 'prompt'";
            }

            return "prompt display=" + chip.resolvedStyle.display + " opacity=" + chip.resolvedStyle.opacity +
                   " shown=" + IsShown(chip) + " glyph='" + (glyph != null ? glyph.text : "?") + "' word='" +
                   (word != null ? word.text : "?") + "'";
        }

        public bool TitleShown()
        {
            return IsShown(_root.Q<VisualElement>("title-screen"));
        }

        private string TextOf(string container, string label)
        {
            VisualElement shell = _root.Q<VisualElement>(container);
            Label text = _root.Q<Label>(label);
            return IsShown(shell) && text != null ? text.text ?? string.Empty : string.Empty;
        }

        private static bool IsShown(VisualElement element)
        {
            for (VisualElement e = element; e != null; e = e.parent)
            {
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.opacity < VisibleOpacity
                    || e.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
            }

            return element != null;
        }
    }
}
