using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// One beat of a player verb (docs/design/feel-pass.md §4): the single contract the feel kit runs on. The domain
    /// that owns the verb's state publishes it; VFX, 07's body, audio, rumble and UI each subscribe once and choose
    /// their response by <see cref="Kind"/> and <see cref="Phase"/>, so a publisher never knows who answers.
    /// </summary>
    public readonly struct FeelBeat
    {
        public FeelBeat(FeelKind kind, FeelPhase phase, Vector3 position, Vector3 normal, float strength,
            float seconds)
        {
            Kind = kind;
            Phase = phase;
            Position = position;
            Normal = normal;
            Strength = Mathf.Clamp01(strength);
            Seconds = Mathf.Max(0f, seconds);
            HasMaterial = false;
            Material = default;
        }

        public FeelBeat(FeelKind kind, FeelPhase phase, Vector3 position, Vector3 normal, float strength,
            float seconds, SalvageMaterial material)
            : this(kind, phase, position, normal, strength, seconds)
        {
            HasMaterial = true;
            Material = material;
        }

        public FeelKind Kind { get; }

        public FeelPhase Phase { get; }

        /// <summary>Where the beat happens in the world (the cut point, the wheels' contact, the dock).</summary>
        public Vector3 Position { get; }

        /// <summary>The surface normal there (unit length), or up when the beat has no surface.</summary>
        public Vector3 Normal { get; }

        /// <summary>
        /// How strong the beat is, 0..1: the landing's impact, the charge reached, or how far a sustained act has come.
        /// </summary>
        public float Strength { get; }

        /// <summary>
        /// How long this phase will last in seconds (the charge-up's length on <see cref="FeelPhase.Anticipate"/>);
        /// 0 when the phase is a moment.
        /// </summary>
        public float Seconds { get; }

        /// <summary>True when <see cref="Material"/> says what was worked (cutting, collecting).</summary>
        public bool HasMaterial { get; }

        /// <summary>The material worked; meaningful only while <see cref="HasMaterial"/> is true.</summary>
        public SalvageMaterial Material { get; }
    }
}
