using System.Collections.Generic;
using UnityEngine;
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

        /// <summary>An element this opaque counts as on screen for the one-guidance-element rule (not a fade's tail).</summary>
        private const float GuidanceOpacity = 0.5f;

        /// <summary>
        /// The guidance elements: the objective line, the chips (the prompt and the look-hint shell are one chip to the
        /// player), the unlock card, the call display and the controls card.
        /// </summary>
        private static readonly string[] GuidanceNames =
        {
            "next-line", "prompt", "look-hint", "controls-card", "unlock-card", "call-display",
        };

        public const string ObjectiveLineName = "next-line-text";
        private const string LookHintName = "look-hint";
        public const string FaceDevicePrefix = "seven-device-";

        private readonly VisualElement _root;
        private readonly FadeWatch _lineFade = new FadeWatch();
        private readonly FadeWatch _promptFade = new FadeWatch();
        private readonly FadeWatch _lookHintFade = new FadeWatch();
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

        /// <summary>
        /// Where build card <paramref name="index"/> is on screen, in Input System pixels (origin bottom left), or
        /// false when the card is not there: what a mouse hand aims at.
        /// </summary>
        public bool TryGetBuildCard(int index, out Vector2 screenPoint)
        {
            screenPoint = Vector2.zero;
            VisualElement card = _root.Q<VisualElement>("build-card-" + index);
            if (card == null || !IsShown(card, GuidanceOpacity) || float.IsNaN(card.worldBound.center.x) ||
                _root.layout.width <= 0f)
            {
                return false;
            }

            float scale = Screen.width / _root.layout.width;
            Vector2 centre = card.worldBound.center;
            screenPoint = new Vector2(centre.x * scale, Screen.height - centre.y * scale);
            return true;
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

        /// <summary>
        /// The objective line while it is shown or showing, else empty: a line in its fade-out is leaving, so it is
        /// ignored. Read it once a frame; the first frame it is seen only starts the watch.
        /// </summary>
        public string ObjectiveLineSteady()
        {
            VisualElement shell = _root.Q<VisualElement>("next-line");
            if (!IsShown(shell))
            {
                _lineFade.Reset();
                return string.Empty;
            }

            return _lineFade.Steady(OpacityOf(shell), Time.frameCount) ? ObjectiveLine() : string.Empty;
        }

        /// <summary>
        /// Fills <paramref name="into"/> with the names of the guidance elements on screen now (an objective line only
        /// while it has words); elements the HUD does not have yet are not on screen.
        /// </summary>
        public void GuidanceShown(List<string> into)
        {
            into.Clear();
            for (int i = 0; i < GuidanceNames.Length; i++)
            {
                VisualElement element = _root.Q<VisualElement>(GuidanceNames[i]);
                if (element == null || !IsShown(element, GuidanceOpacity))
                {
                    continue;
                }

                string words = WordsOf(GuidanceNames[i]);
                if (words == null)
                {
                    into.Add(GuidanceNames[i]);
                }
                else if (words.Length > 0)
                {
                    // The two chip shells share one anchor and carry the same words: one chip to the player.
                    bool secondChip = GuidanceNames[i] == "look-hint" && into.Exists(name => name.StartsWith("prompt"));
                    if (!secondChip)
                    {
                        into.Add(GuidanceNames[i] + " '" + words + "'");
                    }
                }
            }
        }

        /// <summary>
        /// What an element says (its words), empty when it says nothing, null for an element judged by its look alone.
        /// A line or chip with no words is not on screen however opaque its shell.
        /// </summary>
        private string WordsOf(string element)
        {
            switch (element)
            {
                case "next-line":
                    return RawTextOf("next-line-text");
                case "prompt":
                    return (RawTextOf("prompt-glyph-label") + " " + RawTextOf("prompt-word")).Trim();
                case "look-hint":
                    return (RawTextOf("look-hint-glyph-label") + " " + RawTextOf("look-hint-word")).Trim();
                default:
                    return null;
            }
        }

        /// <summary>
        /// True when a prompt chip (the world's own or the bottom hint) is on screen with words and its middle lies
        /// inside the view: what a player looking at the screen can read. <paramref name="text"/> is what it says.
        /// </summary>
        public bool ChipInView(out string text)
        {
            return ChipInView(false, out text);
        }

        /// <summary>
        /// <see cref="ChipInView(out string)"/> for a chip that is shown or showing: one in its fade-out is leaving,
        /// so it is ignored. Read it once a frame; the first frame a chip is seen only starts the watch.
        /// </summary>
        public bool ChipSteadyInView(out string text)
        {
            return ChipInView(true, out text);
        }

        private bool ChipInView(bool steady, out string text)
        {
            Rect view = _root.worldBound;
            text = string.Empty;
            bool found = false;
            for (int i = 0; i < 2; i++)
            {
                string name = i == 0 ? "prompt" : LookHintName;
                FadeWatch fade = i == 0 ? _promptFade : _lookHintFade;
                VisualElement element = _root.Q<VisualElement>(name);
                if (element == null || !IsShown(element, GuidanceOpacity))
                {
                    fade.Reset();
                    continue;
                }

                bool leaving = steady && !fade.Steady(OpacityOf(element), Time.frameCount);
                string words = WordsOf(name);
                if (!found && !leaving && !string.IsNullOrEmpty(words) && view.Contains(element.worldBound.center))
                {
                    text = words;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// True while the controls card (the first-minutes card naming WASD, the mouse, Tab and L) is on screen inside
        /// the view; <paramref name="text"/> is every word on it.
        /// </summary>
        public bool ControlsCardInView(out string text)
        {
            return WordsInView("controls-card", out text);
        }

        /// <summary>
        /// True while the build mode's cost chip (the recipe and the keys: "E/LMB Place · Scroll/Z/C Rotate") is on
        /// screen inside the view; <paramref name="text"/> is every word on it.
        /// </summary>
        public bool BuildCostInView(out string text)
        {
            return WordsInView("build-cost", out text);
        }

        private bool WordsInView(string name, out string text)
        {
            text = string.Empty;
            VisualElement card = _root.Q<VisualElement>(name);
            if (card == null || !IsShown(card, GuidanceOpacity) || !_root.worldBound.Contains(card.worldBound.center))
            {
                return false;
            }

            var words = new System.Text.StringBuilder();
            foreach (Label label in card.Query<Label>().ToList())
            {
                words.Append(label.text).Append(' ');
            }

            text = words.ToString().Trim();
            return text.Length > 0;
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

        private static float OpacityOf(VisualElement element)
        {
            float opacity = 1f;
            for (VisualElement e = element; e != null; e = e.parent)
            {
                opacity *= e.resolvedStyle.opacity;
            }

            return opacity;
        }

        private static bool IsShown(VisualElement element)
        {
            return IsShown(element, VisibleOpacity);
        }

        private static bool IsShown(VisualElement element, float minOpacity)
        {
            for (VisualElement e = element; e != null; e = e.parent)
            {
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.opacity < minOpacity
                    || e.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
            }

            return element != null;
        }
    }
}
