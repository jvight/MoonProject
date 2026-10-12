using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The distance home at the top centre: how it eases, steps, fills its arc and fades near home.</summary>
    [Serializable]
    public sealed class HomeMeterSettings
    {
        [Tooltip("The meter easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.6f, 0.8f, -6f, 0.97f, 1.2f, 0.8f);

        [Tooltip("Metres from home under which the meter fades away (home is in sight).")]
        [Range(5f, 200f)]
        [SerializeField] private float _nearMetres = 40f;

        [Tooltip("Metres at which the arc is full: the far edge of 07's usual roaming.")]
        [Range(50f, 3000f)]
        [SerializeField] private float _fullArcMetres = 800f;

        [Tooltip("The number shows whole steps of this many metres, so it settles instead of flickering.")]
        [Range(1, 50)]
        [SerializeField] private int _stepMetres = 5;

        [Tooltip("The largest number shown (farther reads as this).")]
        [Range(100, 9999)]
        [SerializeField] private int _maxShownMetres = 9995;

        [Tooltip("How quickly the shown distance eases to the true one (per second).")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _easeRate = 4f;

        public RevealSettings Reveal => _reveal;

        public float NearMetres => _nearMetres;

        public float FullArcMetres => Mathf.Max(_fullArcMetres, _nearMetres + 1f);

        public int StepMetres => Mathf.Max(1, _stepMetres);

        public int MaxShownMetres => _maxShownMetres;

        public float EaseRate => _easeRate;
    }
}
