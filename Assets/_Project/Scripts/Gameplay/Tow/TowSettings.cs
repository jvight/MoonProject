using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's tow (docs/design/opening-v3-purpose.md R6, ruling 4): a physical cable from the winch on 07's back to a
    /// load's hitch, a rope that only pulls, never a beam. How far the cable reaches, how the rope pulls, how a load
    /// caught on the ground is lifted free, and where a load comes home. The battery's price for towing (its cost
    /// multiplier and the speed cap) is in BatteryTuning.
    /// </summary>
    [Serializable]
    public sealed class TowSettings
    {
        [Tooltip("Metres of cable the winch pays out: the load trails this far behind the winch once the rope is taut.")]
        [Range(1.5f, 10f)]
        [SerializeField] private float _cableLength = 4f;

        [Tooltip("Metres (flat) from 07's winch within which a load's hitch can be hooked with the tether key.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _hookReach = 3f;

        [Tooltip("How briskly the taut rope takes up (1/s): how fast it matches the load to 07's speed, and how much " +
                 "catch-up each metre of stretch asks for.")]
        [Range(0.5f, 12f)]
        [SerializeField] private float _stiffness = 3.5f;

        [Tooltip("Fastest a load that fell behind closes in on 07 (m/s, on top of 07's own speed): gentle, so it " +
                 "never surges past 07.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _catchUp = 1.2f;

        [Tooltip("Most acceleration (m/s²) the rope gives the load: a cap, so a sudden stretch never yanks it.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _maxPull = 4f;

        [Tooltip("Share of the rope's flat pull that also lifts the load's front, so it rides over bumps and slopes " +
                 "like a sled instead of digging in.")]
        [Range(0f, 1f)]
        [SerializeField] private float _liftShare = 0.35f;

        [Tooltip("Most of the Moon's gravity the cable may take off the load (0..1): under 1, so the load always " +
                 "keeps weight on its skids and is never lifted off the ground.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float _maxLift = 0.5f;

        [Tooltip("Metres of stretch past the cable's length at which the load counts as caught on the ground.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _snagStretch = 1.2f;

        [Tooltip("Speed (m/s) toward the winch under which a stretched load counts as caught rather than lagging.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _snagSpeed = 0.4f;

        [Tooltip("Seconds a load may stay caught before the winch gives it a lift up and forward to free it.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _snagSeconds = 0.6f;

        [Tooltip("Speed (m/s) of the lift that frees a caught load: up, and a little toward the winch.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _unsnagLift = 1.6f;

        [Tooltip("Metres from the winch at which the cable unhooks the load softly (it fell far behind, caught " +
                 "fast): the load stays where it is, to be hooked again.")]
        [Range(4f, 40f)]
        [SerializeField] private float _letGoDistance = 12f;

        [Tooltip("Metres (flat) from the apron's centre within which a towed load has come home: the winch unhooks " +
                 "it there. Wide enough for the load trailing 07 and its cable as 07 pulls up at the pad, inside the " +
                 "battery bank's reach of the pad, so a bank built on it links.")]
        [Range(1f, 20f)]
        [SerializeField] private float _homeRadius = 9f;

        [Tooltip("Friction of a load's skids on the regolith (0..1): enough that a load never runs away down a basin " +
                 "slope or overruns 07, little enough that the rope draws it at a walk.")]
        [Range(0f, 1f)]
        [SerializeField] private float _skidFriction = 0.45f;

        [Tooltip("Linear damping of a load's body (1/s): the regolith's drag on what slides over it. Light: the skids' " +
                 "friction does the holding back, and drag grows with speed.")]
        [Range(0f, 2f)]
        [SerializeField] private float _loadDrag = 0.1f;

        [Tooltip("Angular damping of a load's body, so it never spins on the cable.")]
        [Range(0f, 10f)]
        [SerializeField] private float _loadAngularDrag = 3f;

        [Tooltip("Speed (m/s) under which an unhooked load counts as resting; it then stands fast where it is.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _restSpeed = 0.15f;

        [Tooltip("Seconds an unhooked load must rest before it stands fast.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _restSeconds = 1f;

        public float CableLength => _cableLength;
        public float HookReach => _hookReach;
        public float Stiffness => _stiffness;
        public float CatchUp => _catchUp;
        public float MaxPull => _maxPull;
        public float LiftShare => _liftShare;
        public float MaxLift => _maxLift;
        public float SnagStretch => _snagStretch;
        public float SnagSpeed => _snagSpeed;
        public float SnagSeconds => _snagSeconds;
        public float UnsnagLift => _unsnagLift;
        public float LetGoDistance => _letGoDistance;
        public float HomeRadius => _homeRadius;
        public float SkidFriction => _skidFriction;
        public float LoadDrag => _loadDrag;
        public float LoadAngularDrag => _loadAngularDrag;
        public float RestSpeed => _restSpeed;
        public float RestSeconds => _restSeconds;
    }
}
