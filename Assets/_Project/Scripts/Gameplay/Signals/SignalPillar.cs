using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's amber pillar over what she picked up (design ruling 3's language: the sonar pillar's shape, warm and
    /// persistent): a tall soft pillar and a breathing ring on the ground. It rises slowly where it is shown, breathes
    /// while it stands, never expires, and fades out once hidden. Allocation-free after construction.
    /// </summary>
    internal sealed class SignalPillar : IDisposable
    {
        private readonly SonarTuning _sonar;
        private readonly ITerrainQuery _terrain;
        private readonly Transform _pillar;
        private readonly GlowRenderer _pillarGlow;
        private readonly GlowRenderer _ringGlow;
        private readonly TerrainRing _ring;
        private float _level;
        private bool _shown;

        public SignalPillar(string name, Transform parent, SonarTuning sonar, ITerrainQuery terrain,
            Material pillarMaterial, Material ringMaterial, Mesh pillarMesh)
        {
            _sonar = sonar != null ? sonar : throw new ArgumentNullException(nameof(sonar));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            MeshRenderer pillar = GlowObject.Create("Pillar", root.transform, pillarMesh, pillarMaterial);
            _pillar = pillar.transform;
            _pillar.localScale = new Vector3(sonar.PillarRadius, sonar.PillarHeight, sonar.PillarRadius);
            _pillarGlow = new GlowRenderer(pillar);
            _ring = new TerrainRing(sonar.SiteRingSegments);
            _ringGlow = new GlowRenderer(GlowObject.Create("Ring", root.transform, _ring.Mesh, ringMaterial));
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
            Vector2 ring = _sonar.SiteRing;
            _ring.Rebuild(_terrain, Position, ring.x - ring.y * 0.5f, ring.x + ring.y * 0.5f, _sonar.RingLift);
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
            _ringGlow.Apply(tuning.PillarRingGlow * glow);
        }

        public void Dispose()
        {
            if (_ring.Mesh != null)
            {
                Object.Destroy(_ring.Mesh);
            }
        }
    }
}
