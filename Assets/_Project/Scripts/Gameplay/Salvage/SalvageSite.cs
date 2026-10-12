using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One wreck in the world (docs/features/M3-13): Art's site prefab on its World anchor, its pieces, the heart where
    /// its relics rest, and whether it still answers the sonar. Picked clean, the skeleton stays and it falls silent
    /// (unless a relic still waits in its heart).
    /// </summary>
    public sealed class SalvageSite
    {
        private readonly List<SalvagePiece> _pieces = new List<SalvagePiece>();
        private readonly List<Relic> _relics = new List<Relic>();

        private readonly bool _heartUnderPieces;
        private EmissionGlow _pilotGlow;
        private LandmarkPilotLevel _pilot;
        private Vector3 _shudderRest;
        private float _shudderLeft;
        private float _shudderDuration;
        private float _shudderTime;

        internal SalvageSite(int index, string id, Transform root, float radius, Vector3 heart, AbilityGate gate,
            ComboCounter combo, bool heartUnderPieces)
        {
            _heartUnderPieces = heartUnderPieces;
            Index = index;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Radius = radius;
            Heart = heart;
            Gate = gate ?? throw new ArgumentNullException(nameof(gate));
            Combo = combo ?? throw new ArgumentNullException(nameof(combo));
        }

        /// <summary>Position in the catalog (stable).</summary>
        public int Index { get; }

        /// <summary>Its anchor id ("site.depot", ...): saves, events and relic definitions use it.</summary>
        public string Id { get; }

        /// <summary>The site prefab's instance, standing on the anchor with +Z along its forward.</summary>
        public Transform Root { get; }

        public Vector3 Position => Root.position;

        /// <summary>Radius (m) of the site's flat footprint (its World anchor's).</summary>
        public float Radius { get; }

        /// <summary>The "Heart" node's world position: where its relics rest.</summary>
        public Vector3 Heart { get; }

        /// <summary>The rover ability reaching it needs (ungated for the basin's sites).</summary>
        public AbilityGate Gate { get; }

        public IReadOnlyList<SalvagePiece> Pieces => _pieces;

        /// <summary>The relics resting in its heart.</summary>
        public IReadOnlyList<Relic> Relics => _relics;

        /// <summary>True once it has answered a ping (or a spotter friend found it).</summary>
        public bool Discovered { get; private set; }

        /// <summary>Pieces not yet taken (on the wreck, loose, or on their way into 07).</summary>
        public int Remaining
        {
            get
            {
                int remaining = 0;
                for (int i = 0; i < _pieces.Count; i++)
                {
                    if (_pieces[i].State != SalvagePieceState.Taken)
                    {
                        remaining++;
                    }
                }

                return remaining;
            }
        }

        /// <summary>Every piece is folded into 07 or on its way: only the skeleton stays.</summary>
        public bool IsPickedClean
        {
            get
            {
                for (int i = 0; i < _pieces.Count; i++)
                {
                    SalvagePieceState state = _pieces[i].State;
                    if (state != SalvagePieceState.Breaking && state != SalvagePieceState.Flying &&
                        state != SalvagePieceState.Taken)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// The heart can be dug: always, or for a site whose heart lies under its pieces (M3-16 P10), once the last
        /// piece is cut.
        /// </summary>
        public bool HeartRevealed => !_heartUnderPieces || IsPickedClean;

        /// <summary>
        /// A relic still waits in its heart (not yet lifted free) and the heart is revealed: a hidden heart neither
        /// answers the sonar nor warms the site's answer (M3-16 P10).
        /// </summary>
        public bool HoldsRelic
        {
            get
            {
                if (!HeartRevealed)
                {
                    return false;
                }

                for (int i = 0; i < _relics.Count; i++)
                {
                    if (_relics[i].CanBeLifted)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True once the site was found and its heart can be dug: the landmark's pilot lights.</summary>
        public bool HeartFound => Discovered && HeartRevealed;

        /// <summary>Glow of the landmark pilot (0 without a landmark or before the heart is found).</summary>
        public float PilotLevel => _pilot != null ? _pilot.Level : 0f;

        /// <summary>It answers a ping while something is left to find: a piece to cut or a relic to lift.</summary>
        public bool AnswersSonar => !IsPickedClean || HoldsRelic;

        /// <summary>The salvage melody of this site: consecutive pieces climb a step.</summary>
        internal ComboCounter Combo { get; }

        internal void Add(SalvagePiece piece)
        {
            _pieces.Add(piece ?? throw new ArgumentNullException(nameof(piece)));
        }

        internal void AddRelic(Relic relic)
        {
            _relics.Add(relic != null ? relic : throw new ArgumentNullException(nameof(relic)));
        }

        /// <summary>The piece numbered <paramref name="number"/>, or null.</summary>
        public SalvagePiece Find(int number)
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                if (_pieces[i].Number == number)
                {
                    return _pieces[i];
                }
            }

            return null;
        }

        /// <summary>It answered (or was spotted): its relics count as found too.</summary>
        internal void MarkDiscovered()
        {
            Discovered = true;
            for (int i = 0; i < _relics.Count; i++)
            {
                _relics[i].MarkDiscovered();
            }
        }

        /// <summary>Wires the landmark's pilot lamp (dark until the heart is found).</summary>
        internal void AttachPilot(Renderer lamp, SalvageTuning tuning)
        {
            _pilotGlow = new EmissionGlow(lamp);
            _pilot = new LandmarkPilotLevel(tuning.LandmarkGlow, tuning.LandmarkEase);
            _pilotGlow.Apply(0f);
        }

        internal void StepPilot(float deltaTime)
        {
            if (_pilot != null)
            {
                _pilotGlow.Apply(_pilot.Step(HeartFound, deltaTime));
            }
        }

        /// <summary>A piece cracks free: the wreck shudders for <paramref name="seconds"/> (feel-pass §3.1).</summary>
        internal void Shudder(float seconds)
        {
            if (_shudderLeft <= 0f)
            {
                _shudderRest = Root.position;
            }

            _shudderDuration = seconds;
            _shudderLeft = seconds;
            _shudderTime = 0f;
        }

        /// <summary>The shudder's side-to-side shake, fading out, then the wreck back where it stands.</summary>
        internal void StepShudder(float deltaTime, float amplitude, float frequency)
        {
            if (_shudderLeft <= 0f)
            {
                return;
            }

            _shudderLeft -= deltaTime;
            _shudderTime += deltaTime;
            if (_shudderLeft <= 0f)
            {
                Root.position = _shudderRest;
                return;
            }

            float fade = _shudderLeft / _shudderDuration;
            float shake = Mathf.Sin(_shudderTime * frequency * 2f * Mathf.PI);
            Root.position = _shudderRest + Root.right * (amplitude * fade * shake);
        }

        /// <summary>Shows the settled pilot at once (after a load).</summary>
        internal void SnapPilot()
        {
            if (_pilot != null)
            {
                _pilotGlow.Apply(_pilot.Snap(HeartFound));
            }
        }

        internal void RestoreDiscovered(bool discovered)
        {
            Discovered = discovered;
        }
    }
}
