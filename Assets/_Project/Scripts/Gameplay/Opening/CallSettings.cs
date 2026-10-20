using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The call from the sky (docs/design/opening-v3-purpose.md §2, <see cref="CallProgress"/>): its sender's identity,
    /// how many of its characters decode at the first charge and at the answer (then one per dock), and where its
    /// point of light stands beside dark Earth.
    /// </summary>
    [Serializable]
    public sealed class CallSettings
    {
        [Tooltip("The sender's identity as it reads in full on 07's face; spaces are not characters to decode.")]
        [SerializeField] private string _identity = "ARK IV";

        [Tooltip("Characters that decode when 07 first charges (A?? ??).")]
        [Range(0, 8)]
        [SerializeField] private int _decodedAtFirstCharge = 1;

        [Tooltip("Characters that decode more at the answer (R8); after it, one more decodes at each dock.")]
        [Range(0, 8)]
        [SerializeField] private int _decodedAtAnswer = 2;

        [Tooltip("Degrees round the world's up from Earth's direction to the call's point of light: beside Earth, on " +
            "the side away from where the Sun breaks out of the eclipse, so its corona never swallows the light.")]
        [Range(-30f, 30f)]
        [SerializeField] private float _yawFromEarth = -9f;

        [Tooltip("Degrees above Earth's direction the point of light stands.")]
        [Range(-15f, 15f)]
        [SerializeField] private float _pitchFromEarth = 6f;

        public string Identity => _identity;

        public int DecodedAtFirstCharge => _decodedAtFirstCharge;

        public int DecodedAtAnswer => _decodedAtAnswer;

        public float YawFromEarth => _yawFromEarth;

        public float PitchFromEarth => _pitchFromEarth;

        /// <summary>Characters to decode in the identity (its spaces left out).</summary>
        public int Total
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _identity.Length; i++)
                {
                    total += _identity[i] == ' ' ? 0 : 1;
                }

                return total;
            }
        }

        /// <summary>Null when the call can play, else the first problem.</summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(_identity) || Total == 0)
            {
                return "the call has no sender's identity to decode";
            }

            return _decodedAtFirstCharge + _decodedAtAnswer <= Total
                ? null
                : "the call decodes more characters at the first charge and the answer than its identity has";
        }
    }
}
