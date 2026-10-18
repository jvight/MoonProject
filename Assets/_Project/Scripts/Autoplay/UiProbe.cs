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
        private const string LookHintName = "look-hint";
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
            string glyph = TextOf("prompt", "prompt-glyph-label");
            return glyph.Length > 0 ? glyph : TextOf(LookHintName, "look-hint-glyph-label");
        }

        /// <summary>The key cap text the chip carries whether or not it is showing.</summary>
        public string RawChipGlyph()
        {
            string raw = RawTextOf("prompt-glyph-label");
            return raw.Length > 0 ? raw : RawTextOf("look-hint-glyph-label");
        }

        /// <summary>The word on the prompt chip when it is showing, else empty.</summary>
        public string ChipWord()
        {
            string word = TextOf("prompt", "prompt-word");
            return word.Length > 0 ? word : TextOf(LookHintName, "look-hint-word");
        }

        /// <summary>"E Scan" for a chip showing the key cap E and the word Scan; empty when no chip shows.</summary>
        public string Chip()
        {
            string glyph = ChipGlyph();
            string word = ChipWord();
            return string.IsNullOrEmpty(glyph) && string.IsNullOrEmpty(word) ? string.Empty : (glyph + " " + word).Trim();
        }

        /// <summary>What the prompt chip and the look-hint chip (where a learned verb's chip renders) say, shown or not.</summary>
        public string DescribeChip()
        {
            return DescribeOne("prompt", "prompt-glyph-label", "prompt-word") + "; " +
                   DescribeOne(LookHintName, "look-hint-glyph-label", "look-hint-word");
        }

        private string DescribeOne(string container, string glyphName, string wordName)
        {
            VisualElement chip = _root.Q<VisualElement>(container);
            Label glyph = _root.Q<Label>(glyphName);
            Label word = _root.Q<Label>(wordName);
            if (chip == null)
            {
                return "the HUD has no element named '" + container + "'";
            }

            return container + " display=" + chip.resolvedStyle.display + " opacity=" + chip.resolvedStyle.opacity +
                   " shown=" + IsShown(chip) + " glyph='" + (glyph != null ? glyph.text : "?") + "' word='" +
                   (word != null ? word.text : "?") + "'";
        }

        private string RawTextOf(string label)
        {
            Label text = _root.Q<Label>(label);
            return text != null && text.text != null ? text.text : string.Empty;
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
