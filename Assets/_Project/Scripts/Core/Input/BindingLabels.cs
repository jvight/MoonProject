using UnityEngine.InputSystem;

namespace MoonProject.Core.Input
{
    /// <summary>
    /// Names the physical control bound to an action for one kind of device, for glyphs. Two forms:
    /// <list type="bullet">
    /// <item>a short label: "E", "RMB", "A", "LT", "Escape" (an axis or a d-pad direction is named by its control, so
    /// a scroll wheel reads "Scroll" and a d-pad "D-Pad");</item>
    /// <item>a stable control id: "keyboard.escape", "mouse.rightbutton", "gamepad.buttonsouth", which the UI uses to
    /// look up a localized keycap word ("Esc", "LMB", face names).</item>
    /// </list>
    /// Both follow rebinding overrides. They allocate strings: callers cache them.
    /// </summary>
    internal static class BindingLabels
    {
        /// <summary>Binding group of keyboard and mouse bindings in the Controls asset.</summary>
        public const string KeyboardMouseGroup = "Keyboard&Mouse";

        /// <summary>Binding group of gamepad bindings in the Controls asset.</summary>
        public const string GamepadGroup = "Gamepad";

        /// <summary>The short label of the first binding of <paramref name="action"/> for the device, or "".</summary>
        public static string For(InputAction action, InputDeviceKind device)
        {
            string path = FirstPath(action, device);
            return path == null
                ? string.Empty
                : InputControlPath.ToHumanReadableString(path,
                    InputControlPath.HumanReadableStringOptions.OmitDevice |
                    InputControlPath.HumanReadableStringOptions.UseShortNames);
        }

        /// <summary>
        /// The control id of the first binding of <paramref name="action"/> for the device ("keyboard.escape"), or ""
        /// when nothing is bound for it.
        /// </summary>
        public static string ControlId(InputAction action, InputDeviceKind device)
        {
            string path = FirstPath(action, device);
            return path == null
                ? string.Empty
                : path.Replace("<", string.Empty).Replace(">", string.Empty).Replace('/', '.').ToLowerInvariant();
        }

        /// <summary>The physical control path of the first matching binding, or null.</summary>
        private static string FirstPath(InputAction action, InputDeviceKind device)
        {
            InputBinding mask = InputBinding.MaskByGroup(
                device == InputDeviceKind.Gamepad ? GamepadGroup : KeyboardMouseGroup);
            foreach (InputBinding binding in action.bindings)
            {
                if (!binding.isComposite && mask.Matches(binding))
                {
                    return PhysicalControlPath(binding.effectivePath);
                }
            }

            return null;
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
