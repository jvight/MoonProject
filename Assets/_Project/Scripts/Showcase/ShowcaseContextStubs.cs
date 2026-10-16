using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Minimal in-memory <see cref="ISaveService"/> stub for the Showcase scene. Performs zero disk I/O and reports
    /// <see cref="SaveLoadResult.NoSave"/> so <see cref="MoonProject.World.SkyCycleSystem"/> initializes cleanly from
    /// <see cref="GameContext"/> without touching the player's save file.
    /// </summary>
    public sealed class ShowcaseInMemorySaveService : ISaveService
    {
        private readonly Dictionary<string, ISaveSection> _sections =
            new Dictionary<string, ISaveSection>(StringComparer.Ordinal);

        public event Action Saved;

        public string FilePath => string.Empty;

        public bool IsLoaded => true;

        public SaveLoadResult LoadResult => SaveLoadResult.NoSave;

        public int RegisteredSectionCount => _sections.Count;

        public IDisposable Register(ISaveSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new ArgumentException("Save section key must not be empty.", nameof(section));
            }

            if (_sections.ContainsKey(section.Key))
            {
                throw new InvalidOperationException($"Duplicate save section key '{section.Key}'.");
            }

            _sections.Add(section.Key, section);
            return new RegistrationToken(_sections, section.Key);
        }

        public bool SaveNow()
        {
            Saved?.Invoke();
            return false;
        }

        public string PutAway()
        {
            _sections.Clear();
            return null;
        }

        private sealed class RegistrationToken : IDisposable
        {
            private readonly Dictionary<string, ISaveSection> _owner;
            private readonly string _key;
            private bool _disposed;

            public RegistrationToken(Dictionary<string, ISaveSection> owner, string key)
            {
                _owner = owner;
                _key = key;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.Remove(_key);
            }
        }
    }

    /// <summary>
    /// Minimal <see cref="IRoverState"/> stub registered in the Showcase <see cref="GameContext"/> so
    /// <see cref="MoonProject.World.SkyCycleSystem"/> samples local terrain-horizon sunlight at the currently focused
    /// exhibit without logging a missing-service error.
    /// </summary>
    public sealed class ShowcaseFocusState : IRoverState
    {
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;

        public ShowcaseFocusState(Vector3 initialPosition)
        {
            _position = initialPosition;
        }

        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;
        public Vector3 Velocity => Vector3.zero;
        public float Speed => 0f;
        public float NormalizedSpeed => 0f;
        public Vector2 DriveInput => Vector2.zero;
        public bool IsGrounded => true;
        public float AirTime => 0f;
        public Vector3 GroundNormal => Vector3.up;

        public void SetFocusPose(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
        }
    }
}
