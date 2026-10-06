using System;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The text of every glyph the UI shows, read once per action and device from the real bindings
    /// (<see cref="InputReader.GetBindingLabel"/>) and cached, so prompts never build strings per frame.
    /// </summary>
    internal sealed class GlyphLabels
    {
        private readonly InputReader _input;
        private readonly string[] _actions;
        private readonly string[] _cancel;
        private readonly int _devices;

        public GlyphLabels(InputReader input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            int actions = Enum.GetValues(typeof(RoverAction)).Length;
            _devices = Enum.GetValues(typeof(InputDeviceKind)).Length;
            _actions = new string[actions * _devices];
            _cancel = new string[_devices];
        }

        public string For(RoverAction action, InputDeviceKind device)
        {
            int index = (int)action * _devices + (int)device;
            return _actions[index] ?? (_actions[index] = _input.GetBindingLabel(action, device));
        }

        public string Cancel(InputDeviceKind device)
        {
            int index = (int)device;
            return _cancel[index] ?? (_cancel[index] = _input.Menu.GetCancelLabel(device));
        }
    }
}
