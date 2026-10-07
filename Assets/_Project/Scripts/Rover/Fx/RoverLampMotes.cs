using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// A few dozen tiny, warm dust motes hanging in 07's headlamp beam near the lens (VISION pillar 6: "dust motes hang
    /// in 07's lamp light"). A world-space particle system under the lamp lets motes appear in the beam and drift on
    /// slow, curling air; every frame each mote's opacity is set from how much of the beam reaches it
    /// (<see cref="LampMoteLight"/>), so motes that drift out of the light, or that 07 turns away from, simply fade.
    /// They fade away as 07 picks up speed and settle back into the light when it stops. Fixed memory, no
    /// allocations. Ticked by <see cref="RoverController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverLampMotes : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>A spot light's angles span the whole cone; the beam test needs the half-angles.</summary>
        private const float HalfAngle = 0.5f;

        private const float AlphaMax = 255f;

        /// <summary>The low bits of a particle's random seed give each mote its own glint phase (0..1).</summary>
        private const uint PhaseMask = 0xFFFF;

        private const float PhaseRange = 65536f;

        [Tooltip("Rover effects tuning (Assets/_Project/Data/Tuning/RoverFxTuning.asset), 'Lamp motes'.")]
        [SerializeField] private RoverFxTuning _tuning;

        [Tooltip("The motes' particle system under the headlamp (world space, cone volume along the lamp's +Z).")]
        [SerializeField] private ParticleSystem _motes;

        [Tooltip("The warm headlamp spot light whose beam the motes catch.")]
        [SerializeField] private Light _headlamp;

        private RoverController _rover;
        private ParticleSystemRenderer _renderer;
        private ParticleSystem.Particle[] _particles;

        /// <summary>How visible the motes are as a whole right now (0..1), eased with 07's speed.</summary>
        public float Visibility { get; private set; }

        /// <summary>The motes' particle system, for tests and tooling.</summary>
        public ParticleSystem System => _motes;

        /// <summary>Validates the wiring and applies the tuning. Returns false (and logs) when broken.</summary>
        public bool Initialize(RoverController rover)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _rover = rover;
            Configure();
            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = Require(_tuning != null, "RoverFxTuning is not assigned.")
                & Require(_motes != null, "Mote particle system is not assigned.")
                & Require(_headlamp != null, "Headlamp light is not assigned.");
            if (_motes != null)
            {
                _renderer = _motes.GetComponent<ParticleSystemRenderer>();
                ok &= Require(_renderer != null && _renderer.sharedMaterial != null, "Motes need a renderer material.");
            }

            return ok;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverLampMotes)}: {message}", this);
            }

            return condition;
        }

        /// <summary>
        /// Applies the tuned count, life, size, drift and wander, and makes new motes start invisible: their opacity
        /// is only ever set from the light they are in.
        /// </summary>
        private void Configure()
        {
            RoverFxTuning tuning = _tuning;
            ParticleSystem.MainModule main = _motes.main;
            main.maxParticles = tuning.MoteCount;
            main.startLifetime = tuning.MoteLifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(tuning.MoteMinSize, tuning.MoteMaxSize);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, tuning.MoteDrift);
            Color tint = main.startColor.color;
            tint.a = 0f;
            main.startColor = tint;

            ParticleSystem.EmissionModule emission = _motes.emission;
            emission.rateOverTime = tuning.MoteCount / tuning.MoteLifetime;

            ParticleSystem.ShapeModule shape = _motes.shape;
            shape.angle = tuning.MoteSpread;
            shape.length = tuning.MoteReach;

            ParticleSystem.NoiseModule noise = _motes.noise;
            noise.strength = tuning.MoteWander;
            noise.frequency = tuning.MoteWanderFrequency;

            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            float brightness = tuning.MoteBrightness;
            block.SetVector(BaseColorId, new Vector4(brightness, brightness, brightness, 1f));
            _renderer.SetPropertyBlock(block);

            _particles = new ParticleSystem.Particle[tuning.MoteCount];
            _motes.Play();
        }

        public void Tick(float deltaTime)
        {
            float target = LampMoteLight.SpeedVisibility(_tuning, _rover.Speed);
            Visibility = Smoothing.Damp(Visibility, target, _tuning.MoteVisibilityHalfLife, deltaTime);

            int count = _motes.GetParticles(_particles);
            if (count == 0)
            {
                return;
            }

            Transform lamp = _headlamp.transform;
            Vector3 lens = lamp.position;
            Vector3 axis = lamp.forward;
            float cosOuter = Mathf.Cos(_headlamp.spotAngle * HalfAngle * Mathf.Deg2Rad);
            float cosInner = Mathf.Cos(_headlamp.innerSpotAngle * HalfAngle * Mathf.Deg2Rad);
            float strength = _tuning.MoteOpacity * Visibility;
            float time = Time.time;
            for (int i = 0; i < count; i++)
            {
                ref ParticleSystem.Particle mote = ref _particles[i];
                float age = 1f - mote.remainingLifetime / mote.startLifetime;
                float phase = (mote.randomSeed & PhaseMask) / PhaseRange;
                float alpha = strength
                    * LampMoteLight.Beam(_tuning, mote.position - lens, axis, cosOuter, cosInner)
                    * LampMoteLight.Life(_tuning, age)
                    * LampMoteLight.Glint(_tuning, time, phase);
                Color32 color = mote.startColor;
                color.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * AlphaMax);
                mote.startColor = color;
            }

            _motes.SetParticles(_particles, count);
        }
    }
}
