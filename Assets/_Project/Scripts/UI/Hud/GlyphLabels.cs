using System;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The text of every glyph the UI shows, read once per action, device and language and cached, so prompts never
    /// build strings per frame. A control with a keycap word in the string tables ("glyph.&lt;control id&gt;", e.g.
    /// glyph.keyboard.escape = "Esc", glyph.gamepad.buttonsouth = "A") shows that word, localized; any other control
    /// shows its short name from the real binding ("E").
    /// </summary>
    internal sealed class GlyphLabels
    {
        public const string KeyPrefix = "glyph.";

        private readonly InputReader _input;
        private readonly ILocalization _localization;
        private readonly string[] _actions;
        private readonly string[] _cancel;
        private readonly int _devices;

        public GlyphLabels(InputReader input, ILocalization localization)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            int actions = Enum.GetValues(typeof(RoverAction)).Length;
            _devices = Enum.GetValues(typeof(InputDeviceKind)).Length;
            _actions = new string[actions * _devices];
            _cancel = new string[_devices];
        }

        public string For(RoverAction action, InputDeviceKind device)
        {
            int index = (int)action * _devices + (int)device;
            return _actions[index] ?? (_actions[index] = Resolve(_input.GetBindingControl(action, device),
                _input.GetBindingLabel(action, device)));
        }

        public string Cancel(InputDeviceKind device)
        {
            int index = (int)device;
            return _cancel[index] ?? (_cancel[index] = Resolve(_input.Menu.GetCancelControl(device),
                _input.Menu.GetCancelLabel(device)));
        }

        /// <summary>Forgets every label (the language changed; keycap words are localized).</summary>
        public void Clear()
        {
            Array.Clear(_actions, 0, _actions.Length);
            Array.Clear(_cancel, 0, _cancel.Length);
        }

        private string Resolve(string control, string fallback)
        {
            return control.Length > 0 && _localization.TryGet(KeyPrefix + control, out string word) ? word : fallback;
        }
    }
}
