using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>One line of 07's power-on self-test: which step it prints and how long it stays the current line.</summary>
    [Serializable]
    public sealed class PostLine
    {
        [SerializeField] private BootCheckStep _step;

        [Tooltip("Seconds the line is the current one: it types, runs its check's motion, then prints its result.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _seconds = 1f;

        public PostLine(BootCheckStep step, float seconds)
        {
            _step = step;
            _seconds = seconds;
        }

        public BootCheckStep Step => _step;

        public float Seconds => _seconds;
    }
}
