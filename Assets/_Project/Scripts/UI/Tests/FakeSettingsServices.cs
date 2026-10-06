using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>Stand-ins for the Audio and Rover settings services, clamping like the real ones.</summary>
    internal sealed class FakeSettingsServices : IAudioSettings, ILookSettings
    {
        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private float _sensitivity = 1f;

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = Mathf.Clamp(value, 0.25f, 3f);
        }

        public bool InvertY { get; set; }

        public float GetVolume(AudioBus bus)
        {
            return _volumes[(int)bus];
        }

        public void SetVolume(AudioBus bus, float volume)
        {
            _volumes[(int)bus] = Mathf.Clamp01(volume);
        }
    }
}
