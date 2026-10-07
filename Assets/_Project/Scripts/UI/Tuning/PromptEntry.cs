using System;
using UnityEngine;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// One teachable action: the gameplay hint it answers, the button it names and how soon it counts as learned. Its
    /// one word is the localized "hint.&lt;kind&gt;" string.
    /// </summary>
    [Serializable]
    public sealed class PromptEntry
    {
        /// <summary>Uses that retire a prompt unless its entry says otherwise: done twice, it is known.</summary>
        public const int DefaultUsesToRetire = 2;

        [Tooltip("The interaction hint this prompt teaches.")]
        [SerializeField] private InteractionKind _kind;

        [Tooltip("The rover action whose binding the glyph shows.")]
        [SerializeField] private RoverAction _action;

        [Tooltip("Seconds the action must stay available before the prompt appears (no flicker when passing by).")]
        [Range(0f, 20f)]
        [SerializeField] private float _dwellSeconds = 0.5f;

        [Tooltip("Metres above the hint's world position where the prompt floats.")]
        [Range(0f, 4f)]
        [SerializeField] private float _liftMetres = 1f;

        [Tooltip("Times the player does the action before its prompt is retired for good (saved).")]
        [Range(1, 10)]
        [SerializeField] private int _usesToRetire = DefaultUsesToRetire;

        public PromptEntry()
        {
        }

        public PromptEntry(InteractionKind kind, RoverAction action, float dwellSeconds, float liftMetres)
            : this(kind, action, dwellSeconds, liftMetres, DefaultUsesToRetire)
        {
        }

        public PromptEntry(InteractionKind kind, RoverAction action, float dwellSeconds, float liftMetres,
            int usesToRetire)
        {
            _kind = kind;
            _action = action;
            _dwellSeconds = dwellSeconds;
            _liftMetres = liftMetres;
            _usesToRetire = usesToRetire;
        }

        public InteractionKind Kind => _kind;

        public RoverAction Action => _action;

        public float DwellSeconds => _dwellSeconds;

        public float LiftMetres => _liftMetres;

        public int UsesToRetire => _usesToRetire;
    }
}
