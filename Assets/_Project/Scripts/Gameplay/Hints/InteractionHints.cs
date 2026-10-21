using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// <see cref="IInteractionHints"/> read straight from the gameplay systems' state (nothing cached).
    /// </summary>
    public sealed class InteractionHints : IInteractionHints, IInteractionFocus
    {
        private const int MaxFocusPoints = 16;

        /// <summary>Scan points closer than this (squared, m) to the primary one are the primary one.</summary>
        private const float SamePointSqr = 0.0025f;

        private static readonly InteractionKind[] Priority =
        {
            InteractionKind.PickUp, InteractionKind.Build, InteractionKind.Salvage, InteractionKind.Scan,
            InteractionKind.Deconstruct, InteractionKind.OpenBuild, InteractionKind.FitPart, InteractionKind.Tether,
            InteractionKind.Install, InteractionKind.Transmit,
            InteractionKind.Rack,
        };

        private readonly SalvageField _salvage;
        private readonly SealedContainers _containers;
        private readonly ScanField _scans;
        private readonly BuildField _building;
        private readonly BeamWork _beam;
        private readonly Cutter _cutter;
        private readonly PickupReach _pickups;
        private readonly FirstUseSpots _firstUse;
        private readonly TowLine _tow;
        private readonly RelayRepair _relay;
        private readonly SocketRackView _rack;
        private readonly IAnswerStatus _answer;
        private readonly Vector3[] _scanBuffer = new Vector3[MaxFocusPoints];

        /// <param name="beam">
        /// 07's beam work: while new work sleeps (the crawl, docs/features/M3-20) a beam verb's hint shows as not ready.
        /// </param>
        /// <param name="cutter">07's cutter: while it is faulted (opening-v2 §10) a cut's hint shows not ready.</param>
        /// <param name="tow">07's tow cable: a load in hook reach is where the tether key is taught.</param>
        /// <param name="relay">relay.1's repair: its feed socket in reach is where a held part is fitted.</param>
        /// <param name="rack">The socket rack in the world: the outline 07 reads names its part.</param>
        /// <param name="answer">The call's answer: while it can be sent, the mast is where E is held to transmit.</param>
        public InteractionHints(SalvageField salvage, SealedContainers containers, ScanField scans,
            BuildField building, BeamWork beam, Cutter cutter, PickupReach pickups, FirstUseSpots firstUse,
            TowLine tow, RelayRepair relay, SocketRackView rack, IAnswerStatus answer)
        {
            _answer = answer ?? throw new ArgumentNullException(nameof(answer));
            _tow = tow ?? throw new ArgumentNullException(nameof(tow));
            _relay = relay ?? throw new ArgumentNullException(nameof(relay));
            _rack = rack ?? throw new ArgumentNullException(nameof(rack));
            _firstUse = firstUse ?? throw new ArgumentNullException(nameof(firstUse));
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
            _scans = scans ?? throw new ArgumentNullException(nameof(scans));
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _beam = beam ?? throw new ArgumentNullException(nameof(beam));
            _cutter = cutter ?? throw new ArgumentNullException(nameof(cutter));
            _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        }

        public InteractionHint Primary
        {
            get
            {
                for (int i = 0; i < Priority.Length; i++)
                {
                    if (TryGet(Priority[i], out InteractionHint hint))
                    {
                        return hint;
                    }
                }

                return InteractionHint.None;
            }
        }

        /// <summary>
        /// The place of the most relevant action now, then (for a scan) every other target in reach: what the camera
        /// keeps clear of 07's body.
        /// </summary>
        public int CopyFocus(Vector3[] points)
        {
            InteractionHint hint = Primary;
            if (hint.Kind == InteractionKind.None || hint.Kind == InteractionKind.OpenBuild ||
                hint.Kind == InteractionKind.FitPart || points.Length == 0)
            {
                return 0;
            }

            points[0] = hint.Target;
            if (hint.Kind != InteractionKind.Scan)
            {
                return 1;
            }

            int count = _scans.CopyInReach(_scanBuffer);
            int written = 1;
            for (int i = 0; i < count && written < points.Length; i++)
            {
                if ((_scanBuffer[i] - hint.Position).sqrMagnitude > SamePointSqr)
                {
                    points[written++] = _scanBuffer[i];
                }
            }

            return written;
        }

        public bool TryGet(InteractionKind kind, out InteractionHint hint)
        {
            if (_building.State != BuildModeState.Closed &&
                kind != InteractionKind.Build && kind != InteractionKind.Deconstruct)
            {
                hint = InteractionHint.None;
                return false;
            }

            switch (kind)
            {
                case InteractionKind.PickUp:
                    bool waiting = _pickups.TryGet(Time.frameCount, out Vector3 at, out string noun);
                    hint = waiting ? new InteractionHint(kind, at, true, noun) : InteractionHint.None;
                    return waiting;
                case InteractionKind.Build:
                    bool weld = _building.TryGetWeldCandidate(out Vector3 weldPoint);
                    hint = weld
                        ? new InteractionHint(kind, weldPoint, _building.IsWelding || _beam.MayStart, null, 0, 0,
                            _building.IsWelding ? _building.Controller.ActiveWeldProgress01 : 0f)
                        : InteractionHint.None;
                    return weld;
                case InteractionKind.Deconstruct:
                    bool fold = _building.TryGetDeconstructCandidate(out Vector3 foldPoint);
                    hint = fold
                        ? new InteractionHint(kind, foldPoint, true, null, 0, 0,
                            _building.Controller.DeconstructProgress01)
                        : InteractionHint.None;
                    return fold;
                case InteractionKind.Salvage:
                    SalvagePiece piece = _salvage.Cutting ?? _salvage.Candidate;
                    SealedContainer sealedOne = _containers.Candidate;
                    bool cuts = _cutter.Ready && _beam.MayStart;
                    float cut = _salvage.IsCutting || _salvage.IsBeamCharging ? _salvage.Progress : 0f;
                    hint = piece != null
                        ? new InteractionHint(kind, piece.CutPosition, _salvage.Cutting != null || cuts, null, 0, 0,
                            cut)
                        : sealedOne != null
                            ? new InteractionHint(kind, sealedOne.CutPoint.position, cuts, null, 0, 0, cut)
                            : InteractionHint.None;
                    return piece != null || sealedOne != null;
                case InteractionKind.Scan:
                    ScanFragment fragment = _scans.Candidate;
                    _scans.TryGetSetStep(fragment, out int step, out int count);
                    ScanFragment held = _scans.Scanning;
                    float scanned = held != null && held == fragment ? held.Progress : 0f;
                    hint = fragment != null
                        ? new InteractionHint(kind, fragment.Point, true, null, step, count, scanned)
                        : InteractionHint.None;
                    return fragment != null;
                case InteractionKind.OpenBuild:
                    bool atApron = _firstUse.TryGetOpenBuild(out Vector3 apron);
                    hint = atApron ? FirstUse(kind, apron) : InteractionHint.None;
                    return atApron;
                case InteractionKind.FitPart:
                    bool atRover = _firstUse.TryGetFitPart(out Vector3 rover);
                    hint = atRover ? new InteractionHint(kind, rover, true) : InteractionHint.None;
                    return atRover;
                case InteractionKind.Tether:
                    bool hookable = _tow.TryGetHookable(out Vector3 hitch);
                    hint = hookable ? FirstUse(kind, hitch) : InteractionHint.None;
                    return hookable;
                case InteractionKind.Install:
                    bool atSocket = _relay.TryGetInstall(out Vector3 socket);
                    hint = atSocket ? FirstUse(kind, socket) : InteractionHint.None;
                    return atSocket;
                case InteractionKind.Transmit:
                    bool sendable = _answer.Phase == AnswerPhase.Ready || _answer.Phase == AnswerPhase.Transmitting;
                    hint = sendable ? FirstUse(kind, _answer.MastPoint, _answer.HoldProgress01) : InteractionHint.None;
                    return sendable;
                case InteractionKind.Rack:
                    bool reads = _rack.TryGetRead(out Vector3 bay, out string bayId);
                    hint = reads ? new InteractionHint(kind, bay, true, bayId) : InteractionHint.None;
                    return reads;
                default:
                    hint = InteractionHint.None;
                    return false;
            }
        }

        /// <summary>
        /// A key taught at <paramref name="target"/>: its chip stands there, or on 07 while it is off screen; a hold's
        /// chip carries how far the hold has come.
        /// </summary>
        private InteractionHint FirstUse(InteractionKind kind, Vector3 target, float holdProgress = 0f)
        {
            Vector3 chip = _firstUse.Place(target, out bool onRover);
            return new InteractionHint(kind, chip, true, target, onRover, null, 0, 0, holdProgress);
        }
    }
}
