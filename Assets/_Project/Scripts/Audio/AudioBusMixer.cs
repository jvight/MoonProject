using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Plain C# volume buses. <see cref="Effective"/> multiplies Master with the bus, so players read one number per
    /// frame. <see cref="Version"/> changes whenever a volume changes.
    /// </summary>
    public sealed class AudioBusMixer
    {
        public const int BusCount = 4;

        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };

        /// <summary>Incremented on every volume change.</summary>
        public int Version { get; private set; }

        /// <summary>The bus's own volume (0..1), not multiplied by Master.</summary>
        public float GetVolume(AudioBus bus)
        {
            return _volumes[IndexOf(bus)];
        }

        /// <summary>Sets a bus volume, clamped to 0..1.</summary>
        public void SetVolume(AudioBus bus, float volume)
        {
            int index = IndexOf(bus);
            float clamped = Mathf.Clamp01(volume);
            if (_volumes[index] == clamped)
            {
                return;
            }

            _volumes[index] = clamped;
            Version++;
        }

        /// <summary>Master x bus (Master alone for <see cref="AudioBus.Master"/>).</summary>
        public float Effective(AudioBus bus)
        {
            int index = IndexOf(bus);
            float master = _volumes[(int)AudioBus.Master];
            return index == (int)AudioBus.Master ? master : master * _volumes[index];
        }

        private static int IndexOf(AudioBus bus)
        {
            int index = (int)bus;
            if (index < 0 || index >= BusCount)
            {
                throw new ArgumentOutOfRangeException(nameof(bus), bus, "Unknown audio bus.");
            }

            return index;
        }
    }
}
