using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The soft centre reticle shown only while aiming the tether.</summary>
    [Serializable]
    public sealed class ReticleSettings
    {
        [Tooltip("The reticle easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.25f, 0.3f, 0f, 0.7f, 1.4f, 0.6f);

        [Tooltip("Scale of the ring while a relic is in the aim cone (it gently opens around the target).")]
        [Range(1f, 2f)]
        [SerializeField] private float _hoverScale = 1.35f;

        [Tooltip("Seconds for the ring to brighten when a target is hovered, and to dim again.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _hoverFadeSeconds = 0.25f;

        [Tooltip("Spring frequency (Hz) of the hover expansion.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _hoverSpringFrequency = 1.6f;

        [Tooltip("Spring damping ratio of the hover expansion.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _hoverSpringDamping = 0.55f;

        public RevealSettings Reveal => _reveal;

        public float HoverScale => _hoverScale;

        public float HoverFadeSeconds => _hoverFadeSeconds;

        public float HoverSpringFrequency => _hoverSpringFrequency;

        public float HoverSpringDamping => _hoverSpringDamping;
    }
}
