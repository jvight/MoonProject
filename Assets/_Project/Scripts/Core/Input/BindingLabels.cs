using UnityEngine.InputSystem;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Names the physical control bound to an action for one kind of device, for glyphs: "E", "RMB", "A", "LT",
    /// "Esc". An axis or a d-pad direction is named by its control, so a scroll wheel reads "Scroll" and a d-pad
    /// "D-Pad". Follows rebinding overrides. Allocates a string: callers cache it.
    /// </summary>
    internal static class BindingLabels
    {
        /// <summary>Binding group of keyboard and mouse bindings in the Controls asset.</summary>
        public const string KeyboardMouseGroup = "Keyboard&Mouse";

        /// <summary>Binding group of gamepad bindings in the Controls asset.</summary>
        public const string GamepadGroup = "Gamepad";

        /// <summary>
        /// The first binding of <paramref name="action"/> for <paramref name="device"/>, or "" if none.
        /// </summary>
        public static string For(InputAction action, InputDeviceKind device)
        {
            InputBinding mask = InputBinding.MaskByGroup(
                device == InputDeviceKind.Gamepad ? GamepadGroup : KeyboardMouseGroup);
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite || !mask.Matches(binding))
                {
                    continue;
                }

                return InputControlPath.ToHumanReadableString(PhysicalControlPath(binding.effectivePath),
                    InputControlPath.HumanReadableStringOptions.OmitDevice |
                    InputControlPath.HumanReadableStringOptions.UseShortNames);
            }

            return string.Empty;
        }

        /// <summary>
        /// "&lt;Mouse&gt;/scroll/y" becomes "&lt;Mouse&gt;/scroll": the device plus its first control.
        /// </summary>
        private static string PhysicalControlPath(string path)
        {
            int device = path.IndexOf('/');
            int control = device < 0 ? -1 : path.IndexOf('/', device + 1);
            return control < 0 ? path : path.Substring(0, control);
        }
    }
}
