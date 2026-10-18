using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// What a player sees of UI's HUD and 07's face screen in the real Main scene, read by element name (UI's
    /// GameUI.uxml and FaceInventoryView): the objective line, the prompt chip (what 07 is in place to do), the key
    /// chip under the line (the step's verb, or the opening's first look and drive words), the build strip and the
    /// face screen's focused element. Shown means displayed, visible and not faded out all the way up, with text in it.
    /// </summary>
    public sealed class HudReader
    {
        private const string NextLineText = "next-line-text";
        private const string Prompt = "prompt";
        private const string PromptGlyphLabel = "prompt-glyph-label";
        private const string PromptWord = "prompt-word";
        private const string KeyChipRoot = "look-hint";
        private const string KeyChipGlyphLabel = "look-hint-glyph-label";
        private const string KeyChipWord = "look-hint-word";
        private const string BuildStrip = "build-strip";
        private const string FaceDevicePrefix = "seven-device-";

        /// <summary>Opacity (0..1, through every ancestor) above which an element reads as on screen: the objective
        /// line's dim rest counts.</summary>
        private const float ShownOpacity = 0.1f;

        private readonly VisualElement _hud;
        private readonly VisualElement _face;

        public HudReader(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (UIDocument document in root.GetComponentsInChildren<UIDocument>(true))
                {
                    VisualElement tree = document.rootVisualElement;
                    if (tree == null)
                    {
                        continue;
                    }

                    _hud = _hud ?? (tree.Q(NextLineText) != null ? tree : null);
                    _face = _face ?? (tree.Q(FaceDevicePrefix + 0) != null ? tree : null);
                }
            }

            if (_hud == null || _face == null)
            {
                throw new InvalidOperationException($"The Main scene's UI has {(_hud == null ? "no HUD" : "a HUD")} " +
                                                    $"({NextLineText}) and {(_face == null ? "no" : "a")} face " +
                                                    $"screen ({FaceDevicePrefix}0).");
            }
        }

        /// <summary>The objective line on screen, or empty.</summary>
        public string Line => TextShown(_hud.Q<Label>(NextLineText));

        /// <summary>The prompt chip on screen with its glyph and its word, or empty.</summary>
        public string Chip => ChipShown(Prompt, PromptGlyphLabel, PromptWord);

        /// <summary>The key chip under the objective line with its glyph and its word, or empty.</summary>
        public string KeyChip => ChipShown(KeyChipRoot, KeyChipGlyphLabel, KeyChipWord);

        private string ChipShown(string root, string glyphLabel, string wordLabel)
        {
            if (!Shown(_hud.Q(root)))
            {
                return string.Empty;
            }

            string glyph = TextShown(_hud.Q<Label>(glyphLabel));
            string word = TextShown(_hud.Q<Label>(wordLabel));
            return glyph.Length > 0 && word.Length > 0 ? glyph + " " + word : string.Empty;
        }

        /// <summary>True while the build strip shows (build mode's menu or placing).</summary>
        public bool BuildStripShown => Shown(_hud.Q(BuildStrip));

        /// <summary>The name of the face screen's focused element, or empty.</summary>
        public string FaceFocus => (_face.panel?.focusController?.focusedElement as VisualElement)?.name ??
                                   string.Empty;

        /// <summary>The face screen's device bubble <paramref name="device"/>'s name.</summary>
        public static string FaceDevice(int device)
        {
            return FaceDevicePrefix + device;
        }

        /// <summary>The device index of the face screen's focused bubble, or -1 when the focus is elsewhere (the
        /// hold).</summary>
        public int FaceFocusedDevice
        {
            get
            {
                string focus = FaceFocus;
                return focus.StartsWith(FaceDevicePrefix, StringComparison.Ordinal) &&
                       int.TryParse(focus.Substring(FaceDevicePrefix.Length), out int device)
                    ? device
                    : -1;
            }
        }

        private static string TextShown(Label label)
        {
            return Shown(label) && !string.IsNullOrWhiteSpace(label.text) ? label.text : string.Empty;
        }

        private static bool Shown(VisualElement element)
        {
            if (element == null || element.panel == null)
            {
                return false;
            }

            float opacity = 1f;
            for (VisualElement node = element; node != null; node = node.parent)
            {
                IResolvedStyle style = node.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden)
                {
                    return false;
                }

                opacity *= style.opacity;
            }

            Rect bounds = element.worldBound;
            return opacity > ShownOpacity && bounds.width > 0f && bounds.height > 0f;
        }
    }
}
