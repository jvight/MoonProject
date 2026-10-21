using System;
using System.Collections.Generic;
using System.Text;
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
        private readonly string[] _all;
        private readonly List<string> _controls = new List<string>();
        private readonly List<string> _labels = new List<string>();
        private readonly int _devices;

        public GlyphLabels(InputReader input, ILocalization localization)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            int actions = 0;
            foreach (RoverAction action in Enum.GetValues(typeof(RoverAction)))
            {
                actions = Math.Max(actions, (int)action + 1);
            }

            _devices = Enum.GetValues(typeof(InputDeviceKind)).Length;
            _actions = new string[actions * _devices];
            _cancel = new string[_devices];
            _all = new string[actions * _devices];
        }

        public string For(RoverAction action, InputDeviceKind device)
        {
            int index = (int)action * _devices + (int)device;
            return _actions[index] ?? (_actions[index] = Resolve(_input.GetBindingControl(action, device),
                _input.GetBindingLabel(action, device)));
        }

        /// <summary>
        /// Every control bound to <paramref name="action"/> on <paramref name="device"/>, joined by a slash in binding
        /// order ("E/LMB"), for a hint that names all the ways to do one thing. Cached per action and device.
        /// </summary>
        public string AllFor(RoverAction action, InputDeviceKind device)
        {
            int index = (int)action * _devices + (int)device;
            if (_all[index] != null)
            {
                return _all[index];
            }

            _controls.Clear();
            _labels.Clear();
            _input.GetBindingControls(action, device, _controls);
            _input.GetBindingLabels(action, device, _labels);
            var joined = new StringBuilder();
            for (int i = 0; i < _labels.Count; i++)
            {
                joined.Append(i > 0 ? "/" : string.Empty);
                joined.Append(Resolve(i < _controls.Count ? _controls[i] : string.Empty, _labels[i]));
            }

            return _all[index] = joined.ToString();
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
            Array.Clear(_all, 0, _all.Length);
        }

        private string Resolve(string control, string fallback)
        {
            return control.Length > 0 && _localization.TryGet(KeyPrefix + control, out string word) ? word : fallback;
        }
    }
}
