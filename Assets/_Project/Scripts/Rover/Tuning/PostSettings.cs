using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's power-on self-test, seen first on the close-up of its face (the inventory's framing) and then wide: how
    /// long the push-in (or the settle after a reload), the tube's power-on, the maker's mark, the load, each line, the
    /// ready hold, the two camera moves and the ready performance take (seconds), the motions on 07 for each check, the
    /// long-sleep dust, the light and the wide shot.
    /// </summary>
    [Serializable]
    public sealed class PostSettings
    {
        [Header("The wake on the face (s)")]
        [Tooltip("New Game on a fresh title: the one eased move from the title's shot onto 07's dark face while the " +
                 "title fades and 07 lifts its head. Unhurried: the wake's beginning has to breathe.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _pushInSeconds = 1f;

        [Tooltip("After the reload a New Game over a save needs: on 07's dark face while the cover opens (UI's " +
                 "vignette) and 07 lifts its head.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _darkSeconds = 0.5f;

        [Tooltip("The tube wakes on 07's face: a dot, a line, then the full raster with a warm bloom, a couple of " +
                 "irregular flickers as the tube catches.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _powerOnSeconds = 1.2f;

        [Tooltip("The maker's mark draws itself stroke by stroke at a plotter's pace, its wordmark types under it, " +
                 "holds at full brightness long enough to read, then dims away with an afterglow (about 1.6 s of " +
                 "strokes, the wordmark at ~14 characters a second, a hold of 1.3 s or more, a 0.5 s dim; the shares " +
                 "are UI's BootFaceSettings).")]
        [Range(1f, 6f)]
        [SerializeField] private float _logoSeconds = 5f;

        [Tooltip("The machine loads its field profile in five discrete steps, each a jump after an irregular dwell " +
                 "(UI's BootFaceSettings shapes them), never a smooth bar.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _loadSeconds = 2.4f;

        [Header("The end of the wake (s)")]
        [Tooltip("After the last line: the readout holds its success, then gives way to the eyes. Longer than the " +
                 "readout's own linger on the face (UI), so the eyes are back before the camera moves.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _readySeconds = 0.6f;

        [Tooltip("Seconds into the ready hold when 07's eyes come back to its face, after the readout's linger.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _readyEyesSeconds = 0.4f;

        [Tooltip("The one eased move that pulls the camera back and out diagonally to the wide front three-quarter.")]
        [Range(1f, 4f)]
        [SerializeField] private float _pullBackSeconds = 2f;

        [Tooltip("The ready performance seen wide: a double blink, the headlamps clicked off and on (the last dust " +
                 "settles off as they come back) and a glance at the lander's beacon.")]
        [Range(1.5f, 5f)]
        [SerializeField] private float _performSeconds = 1.6f;

        [Tooltip("The one eased move round to the chase frame behind 07, after which control is the player's.")]
        [Range(1f, 4f)]
        [SerializeField] private float _moveBehindSeconds = 2.4f;

        [Header("The lines")]
        [Tooltip("The lines 07's self-test prints on its face, in step order, Carrier first and the Lamps check " +
                 "included (07's lamps come on in it), each with its seconds. A check left out does not run: its " +
                 "motion and sounds do not play and its part sheds all its dust in the ready performance.")]
        [SerializeField] private PostLine[] _lines = DefaultLines();

        [Header("Fast-forward")]
        [Tooltip("Seconds into the lines before a key press can fast-forward them, so no press, not even one held " +
                 "over from New Game, skips the wake on its first frame.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _fastForwardAfterSeconds = 0.6f;

        [Tooltip("How many times faster the remaining lines run once a key fast-forwards them.")]
        [Range(2f, 12f)]
        [SerializeField] private float _fastForwardScale = 6f;

        [Header("The motions")]
        [Tooltip("How far (deg) the neck sweeps to each side in the head check: small, so the face stays toward the close-up.")]
        [Range(2f, 45f)]
        [SerializeField] private float _headSweepDegrees = 10f;

        [Tooltip("How far (deg) the head nods down.")]
        [Range(2f, 25f)]
        [SerializeField] private float _headNodDegrees = 5f;

        [Tooltip("How far (deg) each wheel twitches forward and back.")]
        [Range(2f, 40f)]
        [SerializeField] private float _wheelTwitchDegrees = 14f;

        [Tooltip("The motor's shudder: the chassis' vertical shake (m).")]
        [Range(0f, 0.04f)]
        [SerializeField] private float _shudderMetres = 0.012f;

        [Tooltip("The motor's creep: how far (m) the chassis lurches forward.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _lurchMetres = 0.06f;

        [Tooltip("How far (deg) the cutter's turret kicks sideways on each ignition attempt.")]
        [Range(0f, 25f)]
        [SerializeField] private float _turretKickDegrees = 10f;

        [Tooltip("How far (0..1) the solar wing jerks open before it stalls.")]
        [Range(0.2f, 0.9f)]
        [SerializeField] private float _wingJerkOpen = 0.5f;

        [Tooltip("How high (m) the antenna rises in its check.")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _antennaRiseMetres = 0.12f;

        [Tooltip("The cell lamps fill to this share of full (the battery's 50%).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _batteryFill = 0.5f;

        public PostSettings()
        {
        }

        /// <summary>Settings whose self-test prints <paramref name="lines"/>; every other value is its default.</summary>
        public PostSettings(PostLine[] lines)
        {
            _lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }

        public float HeadSweepDegrees => _headSweepDegrees;

        public float HeadNodDegrees => _headNodDegrees;

        public float WheelTwitchDegrees => _wheelTwitchDegrees;

        public float ShudderMetres => _shudderMetres;

        public float LurchMetres => _lurchMetres;

        public float TurretKickDegrees => _turretKickDegrees;

        public float WingJerkOpen => _wingJerkOpen;

        public float AntennaRiseMetres => _antennaRiseMetres;

        public float BatteryFill => _batteryFill;

        public float DarkSeconds => _darkSeconds;

        public float PushInSeconds => _pushInSeconds;

        public float PowerOnSeconds => _powerOnSeconds;

        public float LogoSeconds => _logoSeconds;

        public float LoadSeconds => _loadSeconds;

        public float ReadySeconds => _readySeconds;

        public float ReadyEyesSeconds => Mathf.Min(_readyEyesSeconds, _readySeconds);

        public float PullBackSeconds => _pullBackSeconds;

        public float PerformSeconds => _performSeconds;

        public float MoveBehindSeconds => _moveBehindSeconds;

        public float WideDistance => _wideDistance;

        public float WideSideYaw => _wideSideYaw;

        public float WideElevation => _wideElevation;

        public float WideLookHeight => _wideLookHeight;

        [Header("The long-sleep dust")]
        [Tooltip("The lunar-dust amount of a part under the full coat (the material's own amount is the clean state).")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _dustCoatAmount = 4f;

        [Tooltip("How much coat (0..1 of a part) has to fall before a puff of dust drops off it.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _dustPuffStep = 0.15f;

        [Tooltip("Grains in each puff that drops where coat falls off a part.")]
        [Range(1, 30)]
        [SerializeField] private int _dustPuffGrains = 6;

        [Tooltip("When each part sheds its coat: during which check, over which share of it, and how much is left after.")]
        [SerializeField] private PostDustSegment[] _dustSegments =
        {
            new PostDustSegment(DustZone.Head, BootCheckStep.Head, 0f, 0.8f, 0.4f),
            new PostDustSegment(DustZone.Wheels, BootCheckStep.Wheels, 0.05f, 0.95f, 0.35f),
            new PostDustSegment(DustZone.Body, BootCheckStep.Motor, 0.2f, 0.9f, 0.5f),
            new PostDustSegment(DustZone.Turret, BootCheckStep.Cutter, 0.1f, 0.9f, 0.3f),
            new PostDustSegment(DustZone.Wing, BootCheckStep.SolarWing, 0.1f, 0.9f, 0.2f),
            new PostDustSegment(DustZone.Antenna, BootCheckStep.Antenna, 0f, 0.7f, 0.3f),
        };

        [Header("The light")]
        [Tooltip("The cool earthshine-like rim that lets the body read in the eclipse.")]
        [SerializeField] private Color _rimColor = new Color(0.55f, 0.68f, 1f);

        [Tooltip("Rim intensity at full.")]
        [Min(0f)]
        [SerializeField] private float _rimIntensity = 6f;

        [Range(2f, 20f)]
        [SerializeField] private float _rimRange = 9f;

        [Range(20f, 120f)]
        [SerializeField] private float _rimSpotAngle = 70f;

        [Tooltip("How far (m) toward Earth the rim stands from 07, and how high (m).")]
        [Range(1f, 8f)]
        [SerializeField] private float _rimDistance = 4f;

        [Range(0f, 6f)]
        [SerializeField] private float _rimHeight = 2.2f;

        [Tooltip("The height (m) above 07's ground point the rim aims at.")]
        [Range(0f, 2f)]
        [SerializeField] private float _rimAimHeight = 0.8f;

        [Tooltip("The warm fill that comes off 07's own screen.")]
        [SerializeField] private Color _fillColor = new Color(1f, 0.78f, 0.5f);

        [Min(0f)]
        [SerializeField] private float _fillIntensity = 1.4f;

        [Range(0.5f, 6f)]
        [SerializeField] private float _fillRange = 3f;

        [Tooltip("How far (m) in front of the screen the fill sits.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _fillOffset = 0.5f;

        [Tooltip("Seconds after the face screen flickers on over which the fill grows in.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _fillRampSeconds = 2.5f;

        [Header("The wide shot for the ready performance (the close-up on the face is the inventory's own)")]
        [Tooltip("How far (m) the camera stands from the point it looks at once it has pulled back.")]
        [Range(2f, 9f)]
        [SerializeField] private float _wideDistance = 4.6f;

        [Tooltip("How far (deg) round from 07's front the wide shot stands: further round than the face shot, so " +
                 "the pull-back moves out diagonally.")]
        [Range(0f, 80f)]
        [SerializeField] private float _wideSideYaw = 50f;

        [Tooltip("How far (deg) above the point it looks at the wide shot stands.")]
        [Range(-10f, 25f)]
        [SerializeField] private float _wideElevation = 8f;

        [Tooltip("The wide shot's field of view (deg).")]
        [Range(20f, 60f)]
        [SerializeField] private float _wideFov = 32f;

        [Tooltip("Height (m) above 07's ground point the wide shot looks at: the middle of the body, so all of 07 " +
                 "is in frame.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _wideLookHeight = 0.8f;

        [Header("The start's glance")]
        [Tooltip("How far (deg) at most the head turns from the camera toward the lander's beacon at the start.")]
        [Range(5f, 45f)]
        [SerializeField] private float _beaconGlanceYaw = 30f;

        [Tooltip("How far (deg) at most the head lifts or lowers toward the beacon.")]
        [Range(0f, 15f)]
        [SerializeField] private float _beaconGlancePitch = 8f;

        public float DustCoatAmount => _dustCoatAmount;

        public float DustPuffStep => _dustPuffStep;

        public int DustPuffGrains => _dustPuffGrains;

        /// <summary>When <paramref name="zone"/> sheds its coat.</summary>
        public PostDustSegment DustSegmentOf(DustZone zone)
        {
            for (int i = 0; i < _dustSegments.Length; i++)
            {
                if (_dustSegments[i].Zone == zone)
                {
                    return _dustSegments[i];
                }
            }

            throw new InvalidOperationException($"PostSettings has no dust segment for {zone}.");
        }

        public Color RimColor => _rimColor;

        public float RimIntensity => _rimIntensity;

        public float RimRange => _rimRange;

        public float RimSpotAngle => _rimSpotAngle;

        public float RimDistance => _rimDistance;

        public float RimHeight => _rimHeight;

        public float RimAimHeight => _rimAimHeight;

        public Color FillColor => _fillColor;

        public float FillIntensity => _fillIntensity;

        public float FillRange => _fillRange;

        public float FillOffset => _fillOffset;

        public float FillRampSeconds => _fillRampSeconds;

        public float WideFov => _wideFov;

        public float BeaconGlanceYaw => _beaconGlanceYaw;

        public float BeaconGlancePitch => _beaconGlancePitch;

        /// <summary>The lines 07's self-test prints, in their order.</summary>
        public int LineCount => _lines.Length;

        public float FastForwardAfterSeconds => _fastForwardAfterSeconds;

        public float FastForwardScale => _fastForwardScale;

        /// <summary>The step line <paramref name="index"/> prints.</summary>
        public BootCheckStep LineStep(int index)
        {
            return _lines[index].Step;
        }

        /// <summary>Seconds line <paramref name="index"/> takes on the face.</summary>
        public float LineSeconds(int index)
        {
            return _lines[index].Seconds;
        }

        /// <summary>True when the self-test prints <paramref name="step"/>'s line (and runs its check).</summary>
        public bool Runs(BootCheckStep step)
        {
            return IndexOf(step) >= 0;
        }

        /// <summary>Seconds the line <paramref name="step"/> takes on the face, 0 when it is not printed.</summary>
        public float SecondsOf(BootCheckStep step)
        {
            int index = IndexOf(step);
            return index >= 0 ? _lines[index].Seconds : 0f;
        }

        /// <summary>Null when the lines are usable, else the first problem.</summary>
        public string Validate()
        {
            if (_lines == null || _lines.Length == 0)
            {
                return "the self-test has no lines";
            }

            if (_lines[0] == null || _lines[0].Step != BootCheckStep.Carrier)
            {
                return "the self-test's first line must be the Carrier (it clears the face's log)";
            }

            for (int i = 1; i < _lines.Length; i++)
            {
                if (_lines[i] == null || _lines[i].Step <= _lines[i - 1].Step)
                {
                    return $"the self-test's line {i} is missing or out of step order";
                }
            }

            return Runs(BootCheckStep.Lamps) ? null : "the self-test needs its Lamps line: 07's lamps come on in it";
        }

        private int IndexOf(BootCheckStep step)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                if (_lines[i].Step == step)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// The opening's lines, every check: who 07 is and why it woke, how long it slept, its agenda, then each part
        /// checked in a fast, dense run (the lamps long enough to come on, the cutter's missing igniter a beat
        /// longer), the battery, the assembly and the start.
        /// </summary>
        private static PostLine[] DefaultLines()
        {
            return new[]
            {
                new PostLine(BootCheckStep.Carrier, 0.7f),
                new PostLine(BootCheckStep.Resume, 0.7f),
                new PostLine(BootCheckStep.Agenda, 1f),
                new PostLine(BootCheckStep.Lens, 0.4f),
                new PostLine(BootCheckStep.Lamps, 1.2f),
                new PostLine(BootCheckStep.Head, 0.5f),
                new PostLine(BootCheckStep.Wheels, 0.6f),
                new PostLine(BootCheckStep.Motor, 0.5f),
                new PostLine(BootCheckStep.Cutter, 1.2f),
                new PostLine(BootCheckStep.SolarWing, 0.6f),
                new PostLine(BootCheckStep.Antenna, 0.6f),
                new PostLine(BootCheckStep.Battery, 0.6f),
                new PostLine(BootCheckStep.Assembly, 0.4f),
                new PostLine(BootCheckStep.Begin, 0.4f),
            };
        }
    }

    /// <summary>When one part of 07 sheds its long-sleep dust.</summary>
    [Serializable]
    public sealed class PostDustSegment
    {
        [SerializeField] private DustZone _zone;
        [SerializeField] private BootCheckStep _step;
        [Range(0f, 1f)]
        [SerializeField] private float _start;
        [Range(0f, 1f)]
        [SerializeField] private float _end = 0.9f;
        [Tooltip("Share of the coat left on the part after its check; the rest settles off in the ready performance.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rest = 0.3f;

        public PostDustSegment(DustZone zone, BootCheckStep step, float start, float end, float rest)
        {
            _zone = zone;
            _step = step;
            _start = start;
            _end = end;
            _rest = rest;
        }

        public DustZone Zone => _zone;

        public BootCheckStep Step => _step;

        public float Start => _start;

        public float End => _end;

        public float Rest => _rest;
    }
}
