using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace MoonProject.Testing
{
    /// <summary>
    /// Diagnostic for input that "never arrives" in PlayMode runs. Off unless the environment variable
    /// <see cref="EnvironmentVariable"/> is "1" when play mode starts (e.g. <c>MOON_INPUT_PROBE=1 python
    /// tools/unity_batch.py tests ...</c>). Each input system an InputTestFixture installs is watched once. The probe
    /// logs "[inputprobe]" lines:
    /// - every device change except usage changes, with the stack that caused it (resets, disables, removals);
    /// - every state and delta event for gamepads and keyboards, with frame and time;
    /// - any frame where a gamepad stick drops to zero with no event that frame, which is a state wipe that bypassed
    ///   the event stream.
    /// </summary>
    public sealed class TestRunInputProbe : MonoBehaviour
    {
        /// <summary>Set to "1" to install the probe for the next play-mode session.</summary>
        public const string EnvironmentVariable = "MOON_INPUT_PROBE";

        private const string Tag = "[inputprobe] ";

        private readonly List<InputSettings> _watched = new List<InputSettings>();
        private readonly Dictionary<int, Vector2> _lastStick = new Dictionary<int, Vector2>();
        private readonly HashSet<int> _eventThisFrame = new HashSet<int>();
        private Action<InputDevice, InputDeviceChange> _onDeviceChange;
        private Action<InputEventPtr, InputDevice> _onEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Create()
        {
            if (Environment.GetEnvironmentVariable(EnvironmentVariable) != "1")
            {
                return;
            }

            var host = new GameObject("[TestRunInputProbe]") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(host);
            host.AddComponent<TestRunInputProbe>();
            Debug.Log(Tag + "installed");
        }

        private void Awake()
        {
            _onDeviceChange = OnDeviceChange;
            _onEvent = OnEvent;
        }

        private void Update()
        {
            Watch();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Gamepad gamepad))
                {
                    continue;
                }

                Vector2 stick = gamepad.rightStick.ReadValue() + gamepad.leftStick.ReadValue();
                if (_lastStick.TryGetValue(device.deviceId, out Vector2 last) && last != Vector2.zero &&
                    stick == Vector2.zero && !_eventThisFrame.Contains(device.deviceId))
                {
                    Debug.Log($"{Tag}f={Time.frameCount} t={Time.realtimeSinceStartup:F3} {device} sticks dropped to " +
                              $"zero from {last} with no event this frame; enabled={device.enabled} " +
                              $"focused={Application.isFocused}\n{Environment.StackTrace}");
                }

                _lastStick[device.deviceId] = stick;
            }

            _eventThisFrame.Clear();
        }

        /// <summary>Subscribes once to each input system (identified by its settings object) a fixture installs.</summary>
        private void Watch()
        {
            InputSettings settings = InputSystem.settings;
            if (settings == null || _watched.Contains(settings))
            {
                return;
            }

            _watched.Add(settings);
            InputSystem.onDeviceChange += _onDeviceChange;
            InputSystem.onEvent += _onEvent;
            Debug.Log($"{Tag}f={Time.frameCount} t={Time.realtimeSinceStartup:F3} watching input system #{_watched.Count} " +
                      $"(background {settings.backgroundBehavior}, editor {settings.editorInputBehaviorInPlayMode}, " +
                      $"update {settings.updateMode}); edit-mode window " +
                      $"{TestRunInputGuard.ExitEditModeTime:F3}..{TestRunInputGuard.EnterPlayModeTime:F3}");
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.UsageChanged)
            {
                return;
            }

            Debug.Log($"{Tag}f={Time.frameCount} t={Time.realtimeSinceStartup:F3} {device} {change} " +
                      $"enabled={device.enabled} focused={Application.isFocused}\n{Environment.StackTrace}");
        }

        private void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!(device is Gamepad) && !(device is Keyboard))
            {
                return;
            }

            _eventThisFrame.Add(device.deviceId);
            string detail = string.Empty;
            if (device is Gamepad gamepad && (eventPtr.IsA<StateEvent>() || eventPtr.IsA<DeltaStateEvent>()))
            {
                Vector2 right = gamepad.rightStick.ReadValueFromEventAsObject(eventPtr) is Vector2 value
                    ? value
                    : Vector2.zero;
                detail = $" rightStick->{right}";
            }

            Debug.Log($"{Tag}f={Time.frameCount} t={Time.realtimeSinceStartup:F3} {device} event {eventPtr.type}" +
                      $" time={eventPtr.time:F3}{detail}");
        }
    }
}
