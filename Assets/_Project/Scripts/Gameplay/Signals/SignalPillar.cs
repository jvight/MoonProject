using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's amber pillar over what she picked up (design ruling 3's language: the sonar pillar's shape, warm and
    /// persistent): a tall soft pillar, nothing on the ground (0.5.5 §2). It rises slowly where it is shown, breathes
    /// while it stands, never expires, and fades out once hidden. Allocation-free after construction.
    /// </summary>
    internal sealed class SignalPillar
    {
        private readonly ITerrainQuery _terrain;
        private readonly Transform _pillar;
        private readonly GlowRenderer _pillarGlow;
        private float _level;
        private bool _shown;

        public SignalPillar(string name, Transform parent, BellTuning tuning, ITerrainQuery terrain,
            Material pillarMaterial, Mesh pillarMesh)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            MeshRenderer pillar = GlowObject.Create("Pillar", root.transform, pillarMesh, pillarMaterial);
            _pillar = pillar.transform;
            _pillar.localScale = new Vector3(tuning.PillarRadius, tuning.PillarHeight, tuning.PillarRadius);
            _pillarGlow = new GlowRenderer(pillar);
        }

        /// <summary>Where it stands (valid once shown).</summary>
        public Vector3 Position { get; private set; }

        /// <summary>It is rising or standing (not fading or gone).</summary>
        public bool Shown => _shown;

        /// <summary>0 dark .. 1 fully risen.</summary>
        public float Level => _level;

        public float Intensity => _pillarGlow.Intensity;

        /// <summary>Rises from dark at <paramref name="groundPosition"/>.</summary>
        public void Show(Vector3 groundPosition)
        {
            _level = 0f;
            Position = SurfaceRules.OnSurface(_terrain, groundPosition.x, groundPosition.z);
            _pillar.position = Position;
            _shown = true;
        }

        /// <summary>Its target was found: it fades out where it stands.</summary>
        public void Hide()
        {
            _shown = false;
        }

        public void Tick(float now, float deltaTime, BellTuning tuning)
        {
            float rate = _shown ? deltaTime / tuning.PillarRise : -deltaTime / tuning.PillarFade;
            _level = Mathf.Clamp01(_level + rate);
            float breath = 1f - tuning.PillarBreathDepth *
                (1f - MarkerEnvelope.Breath(now, tuning.PillarBreathPeriod));
            float glow = Ease.InOutSine(_level) * breath;
            _pillarGlow.Apply(tuning.PillarGlow * glow);
        }
    }
}
