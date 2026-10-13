using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The short note card: a crew crate's packing note (docs/features/M3-25 T4) and what a scan found.</summary>
    [Serializable]
    public sealed class CrateNoteSettings
    {
        [Tooltip("The note easing in and out: slow and soft, rising a little.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.9f, 1.4f, 12f, 0.97f, 1f, 0.7f);

        [Tooltip("Seconds the note rests before reading time is added.")]
        [Range(1f, 8f)]
        [SerializeField] private float _baseSeconds = 3.5f;

        [Tooltip("Reading seconds added for each character of the note.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _secondsPerCharacter = 0.055f;

        [Tooltip("Longest the note stays, however long it is.")]
        [Range(4f, 20f)]
        [SerializeField] private float _maxSeconds = 12f;

        [Tooltip("Metal gameplay pays when a scan finishes a known blueprint's fragment; the scan note says it.")]
        [Range(1, 10)]
        [SerializeField] private int _knownMetal = 3;

        public RevealSettings Reveal => _reveal;

        public int KnownMetal => _knownMetal;

        /// <summary>Seconds a note of <paramref name="characters"/> characters rests.</summary>
        public float HoldSeconds(int characters)
        {
            return Mathf.Min(_maxSeconds, _baseSeconds + characters * _secondsPerCharacter);
        }
    }
}
