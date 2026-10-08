using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How relics behave as objects: low-gravity physics, the highlight halo, their idle and display motion, and the
    /// gentle return when one drifts out of reach. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RelicTuning : ScriptableObject
    {
        [Header("Physics")]
        [Tooltip("Bounciness of a loose relic: low-gravity hops are a reward, so a little spring is welcome.")]
        [Range(0f, 1f)] [SerializeField] private float _bounciness = 0.35f;

        [Tooltip("Friction of a loose relic on the dust.")]
        [Range(0f, 1.5f)] [SerializeField] private float _friction = 0.55f;

        [Tooltip("Linear damping of a loose relic (air-less moon, but it should settle).")]
        [Range(0f, 2f)] [SerializeField] private float _linearDamping = 0.08f;

        [Tooltip("Angular damping of a loose relic: it tumbles a little, never spins wildly.")]
        [Range(0f, 5f)] [SerializeField] private float _angularDamping = 0.6f;

        [Tooltip("Share of a relic's height sunk in the dust of its site's heart; the rest peeks out, so the heart " +
                 "reads up close and the dig is a short, gentle one.")]
        [Range(0.2f, 1f)] [SerializeField] private float _buriedShare = 0.7f;

        [Header("Halo")]
        [Tooltip("Scale of the halo shell around the relic's meshes.")]
        [Range(1.01f, 1.5f)] [SerializeField] private float _haloScale = 1.09f;

        [Tooltip("Seconds (time constant) for the halo to brighten or dim.")]
        [Range(0f, 2f)] [SerializeField] private float _haloEase = 0.15f;

        [Tooltip("Halo brightness while a partly lifted relic waits in the dust.")]
        [Range(0f, 3f)] [SerializeField] private float _surfacingGlow = 0.35f;

        [Tooltip("Halo brightness of a relic on display: a gentle, proud glow.")]
        [Range(0f, 3f)] [SerializeField] private float _displayGlow = 0.3f;

        [Header("Motion")]
        [Tooltip("Bob (m) of a partly lifted relic waiting for the beam.")]
        [Range(0f, 0.2f)] [SerializeField] private float _waitingBob = 0.03f;

        [Tooltip("Bob frequency (Hz) of a waiting or displayed relic.")]
        [Range(0.05f, 2f)] [SerializeField] private float _bobFrequency = 0.3f;

        [Tooltip("Turn speed (degrees per second) of a relic on display.")]
        [Range(0f, 60f)] [SerializeField] private float _displaySpin = 9f;

        [Tooltip("Bob (m) of a relic on display.")]
        [Range(0f, 0.1f)] [SerializeField] private float _displayBob = 0.012f;

        [Header("Never out of reach")]
        [Tooltip("Seconds a loose relic may rest off the drivable floor before it floats back.")]
        [Range(0f, 30f)] [SerializeField] private float _returnDelay = 3f;

        [Tooltip("Metres below the surface at which a relic counts as lost under it and returns at once.")]
        [Range(0.2f, 10f)] [SerializeField] private float _belowSurfaceLimit = 1.5f;

        [Tooltip("Metres inside the drivable edge a returning relic is set down.")]
        [Range(0f, 30f)] [SerializeField] private float _returnMargin = 6f;

        [Tooltip("Height (m) above the ground a returning relic floats at.")]
        [Range(0f, 5f)] [SerializeField] private float _returnHover = 1.2f;

        [Tooltip("Seconds for a returning relic to float back.")]
        [Range(0.5f, 10f)] [SerializeField] private float _returnDuration = 3.5f;

        public float Bounciness => _bounciness;
        public float Friction => _friction;
        public float LinearDamping => _linearDamping;
        public float AngularDamping => _angularDamping;
        public float BuriedShare => _buriedShare;
        public float HaloScale => _haloScale;
        public float HaloEase => _haloEase;
        public float SurfacingGlow => _surfacingGlow;
        public float DisplayGlow => _displayGlow;
        public float WaitingBob => _waitingBob;
        public float BobFrequency => _bobFrequency;
        public float DisplaySpin => _displaySpin;
        public float DisplayBob => _displayBob;
        public float ReturnDelay => _returnDelay;
        public float BelowSurfaceLimit => _belowSurfaceLimit;
        public float ReturnMargin => _returnMargin;
        public float ReturnHover => _returnHover;
        public float ReturnDuration => _returnDuration;
    }
}
