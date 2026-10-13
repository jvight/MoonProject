using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// <see cref="ITechTree"/> over what the game already holds (docs/features/M3-21 §4): the tower's levels are
    /// built as they land, the relay network once its first mast is lit, and each kit is decoded once 07 has scanned
    /// its blueprint's set and built once the bay fitted it. Nothing of its own is saved. <see cref="Refresh"/>
    /// publishes <see cref="TechNodeChanged"/> for each node that moved on; <see cref="Announce"/> (after a save is
    /// applied) takes the state silently and publishes it once per known node with Restored = true. Nothing allocates
    /// per frame.
    /// </summary>
    public sealed class TechTree : ITechTree
    {
        private readonly EventBus _events;
        private readonly Func<int> _towerLevel;
        private readonly Func<int> _litMasts;
        private readonly Func<string, bool> _decoded;
        private readonly Func<string, int> _fitted;
        private readonly string[] _kits;
        private readonly TechNodeState[] _states;

        /// <param name="towerLevel">The tower level on show.</param>
        /// <param name="litMasts">Relay masts lit (linked home).</param>
        /// <param name="decoded">True once a kit's blueprint is learned (by the bay's upgrade id).</param>
        /// <param name="fitted">A kit's fitted level (by the bay's upgrade id).</param>
        /// <param name="kits">The kit nodes this version holds; a kit node left out stays unknown.</param>
        /// <exception cref="ArgumentException">A kit names a station node, no upgrade, or repeats another.</exception>
        public TechTree(EventBus events, Func<int> towerLevel, Func<int> litMasts, Func<string, bool> decoded,
            Func<string, int> fitted, IReadOnlyList<TechKit> kits)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _towerLevel = towerLevel ?? throw new ArgumentNullException(nameof(towerLevel));
            _litMasts = litMasts ?? throw new ArgumentNullException(nameof(litMasts));
            _decoded = decoded ?? throw new ArgumentNullException(nameof(decoded));
            _fitted = fitted ?? throw new ArgumentNullException(nameof(fitted));
            int count = Enum.GetValues(typeof(TechNodeId)).Length;
            _kits = new string[count];
            _states = new TechNodeState[count];
            foreach (TechKit kit in kits ?? throw new ArgumentNullException(nameof(kits)))
            {
                int node = (int)kit.Node;
                if (node < 0 || node >= count || IsStation(kit.Node) || string.IsNullOrEmpty(kit.KitId) ||
                    _kits[node] != null || Array.IndexOf(_kits, kit.KitId) >= 0)
                {
                    throw new ArgumentException($"Tech kit '{kit.KitId}' on {kit.Node} is not one kit on one kit " +
                                                "node.", nameof(kits));
                }

                _kits[node] = kit.KitId;
            }
        }

        public int KnownCount { get; private set; }

        public TechNodeState StateOf(TechNodeId node)
        {
            return _states[(int)node];
        }

        /// <summary>The bay's upgrade id of kit node <paramref name="node"/>, or null (a station node).</summary>
        public string KitOf(TechNodeId node)
        {
            return _kits[(int)node];
        }

        /// <summary>Publishes each node that moved on since the last call.</summary>
        public void Refresh()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                TechNodeState state = Derive(i);
                if (state == _states[i])
                {
                    continue;
                }

                _states[i] = state;
                Count();
                _events.Publish(new TechNodeChanged((TechNodeId)i, state, false));
            }
        }

        /// <summary>Takes a loaded game's state silently and tells everyone each known node (no moments).</summary>
        public void Announce()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i] = Derive(i);
            }

            Count();
            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i] != TechNodeState.Unknown)
                {
                    _events.Publish(new TechNodeChanged((TechNodeId)i, _states[i], true));
                }
            }
        }

        private static bool IsStation(TechNodeId node)
        {
            return node == TechNodeId.TowerL1 || node == TechNodeId.TowerL2 || node == TechNodeId.TowerL3 ||
                   node == TechNodeId.RelayNetwork;
        }

        private TechNodeState Derive(int index)
        {
            var node = (TechNodeId)index;
            switch (node)
            {
                case TechNodeId.TowerL1: return Built(_towerLevel() >= 1);
                case TechNodeId.TowerL2: return Built(_towerLevel() >= 2);
                case TechNodeId.TowerL3: return Built(_towerLevel() >= 3);
                case TechNodeId.RelayNetwork: return Built(_litMasts() > 0);
            }

            string kit = _kits[index];
            return kit == null ? TechNodeState.Unknown
                : _fitted(kit) > 0 ? TechNodeState.Built
                : _decoded(kit) ? TechNodeState.Decoded
                : TechNodeState.Unknown;
        }

        private static TechNodeState Built(bool built)
        {
            return built ? TechNodeState.Built : TechNodeState.Unknown;
        }

        private void Count()
        {
            int known = 0;
            for (int i = 0; i < _states.Length; i++)
            {
                known += _states[i] != TechNodeState.Unknown ? 1 : 0;
            }

            KnownCount = known;
        }
    }
}
