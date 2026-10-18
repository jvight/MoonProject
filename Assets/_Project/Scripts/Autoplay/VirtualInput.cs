using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// The bot's hands: a virtual keyboard and mouse added to the Input System, so the game reads its input actions
    /// exactly as it does from a player, and nothing goes through the operating system. The real devices are
    /// disabled for the run, so whoever sits at the machine cannot disturb it (or be disturbed by it), and the
    /// window keeps taking input while it is in the background.
    /// </summary>
    public sealed class VirtualInput : IDisposable
    {
        private readonly Keyboard _keyboard;
        private readonly Mouse _mouse;
        private readonly HashSet<Key> _held = new HashSet<Key>();
        private readonly List<InputDevice> _disabled = new List<InputDevice>();
        private readonly Key[] _keyBuffer = new Key[16];
        private readonly InputSettings.BackgroundBehavior _backgroundBehavior;
        private bool _leftHeld;

        public VirtualInput()
        {
            _backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Keyboard || device is Mouse || device is Gamepad)
                {
                    InputSystem.DisableDevice(device);
                    _disabled.Add(device);
                }
            }

            _keyboard = InputSystem.AddDevice<Keyboard>("AutoplayKeyboard");
            _mouse = InputSystem.AddDevice<Mouse>("AutoplayMouse");
        }

        public bool IsHeld(Key key)
        {
            return _held.Contains(key);
        }

        /// <summary>Holds <paramref name="key"/> down (or lets it go) from the next input update.</summary>
        public void Set(Key key, bool down)
        {
            if (down ? !_held.Add(key) : !_held.Remove(key))
            {
                return;
            }

            Send();
        }

        public void ReleaseAll()
        {
            if (_held.Count == 0)
            {
                return;
            }

            _held.Clear();
            Send();
        }

        /// <summary>Moves the mouse by a number of pixels in this frame (a look movement).</summary>
        public void Look(Vector2 pixels)
        {
            if (pixels == Vector2.zero)
            {
                return;
            }

            InputSystem.QueueDeltaStateEvent(_mouse.delta, pixels);
        }

        public void SetLeftButton(bool down)
        {
            if (_leftHeld == down)
            {
                return;
            }

            _leftHeld = down;
            InputSystem.QueueDeltaStateEvent(_mouse.leftButton, down ? 1f : 0f);
        }

        public void MoveMouseTo(Vector2 screenPosition)
        {
            InputSystem.QueueDeltaStateEvent(_mouse.position, screenPosition);
        }

        public void Dispose()
        {
            ReleaseAll();
            SetLeftButton(false);
            InputSystem.RemoveDevice(_keyboard);
            InputSystem.RemoveDevice(_mouse);
            foreach (InputDevice device in _disabled)
            {
                InputSystem.EnableDevice(device);
            }

            InputSystem.settings.backgroundBehavior = _backgroundBehavior;
        }

        private void Send()
        {
            int count = 0;
            foreach (Key key in _held)
            {
                if (count == _keyBuffer.Length)
                {
                    break;
                }

                _keyBuffer[count++] = key;
            }

            var state = new KeyboardState();
            for (int i = 0; i < count; i++)
            {
                state.Set(_keyBuffer[i], true);
            }

            InputSystem.QueueStateEvent(_keyboard, state);
        }
    }
}
