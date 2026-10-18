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

        private readonly VisualElement _root;

        public UiProbe(VisualElement root)
        {
            _root = root;
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
