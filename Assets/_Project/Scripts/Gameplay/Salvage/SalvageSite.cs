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

        internal SalvageSite(int index, string id, Transform root, float radius, Vector3 heart, AbilityGate gate,
            ComboCounter combo)
        {
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

        /// <summary>A relic still waits in its heart (not yet lifted free).</summary>
        public bool HoldsRelic
        {
            get
            {
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

        internal void RestoreDiscovered(bool discovered)
        {
            Discovered = discovered;
        }
    }
}
