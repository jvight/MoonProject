using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tower's story parts (docs/features/M3-16 P1): a tower level with a part needs it besides materials, so power
    /// is earned by a find. A part waits where it lies until 07 picks it up (<see cref="PickUp"/>, publishing
    /// <see cref="StoryPartFound"/>), rides on 07's back, and is fed to the tower's port (<see cref="Deliver"/>,
    /// publishing <see cref="StoryPartDelivered"/>); only then can its level be built. Parts are kept in level order.
    /// </summary>
    public sealed class TowerParts : IStoryPartGate
    {
        private readonly EventBus _events;
        private readonly string _towerId;
        private readonly TowerPart[] _parts;
        private readonly TowerPartState[] _states;

        /// <param name="towerId">The radio tower's upgrade id: the only upgrade whose levels need parts.</param>
        public TowerParts(EventBus events, string towerId, IReadOnlyList<TowerPart> parts)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _towerId = string.IsNullOrEmpty(towerId)
                ? throw new ArgumentException("The tower's upgrade id is missing.", nameof(towerId))
                : towerId;
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            _parts = new TowerPart[parts.Count];
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i] = parts[i];
            }

            Array.Sort(_parts, (a, b) => a.TowerLevel.CompareTo(b.TowerLevel));
            string problem = Problem(_parts);
            if (problem != null)
            {
                throw new ArgumentException(problem, nameof(parts));
            }

            _states = new TowerPartState[_parts.Length];
        }

        public int Count => _parts.Length;

        /// <summary>The part at <paramref name="index"/> (level order).</summary>
        public TowerPart PartAt(int index)
        {
            return _parts[index];
        }

        public TowerPartState StateOf(int index)
        {
            return _states[index];
        }

        public bool NeedsPart(string upgradeId, int level)
        {
            if (upgradeId != _towerId)
            {
                return false;
            }

            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i].TowerLevel == level)
                {
                    return _states[i] != TowerPartState.Delivered;
                }
            }

            return false;
        }

        /// <summary>The lowest-level part riding on 07, or -1.</summary>
        public int FirstCarried()
        {
            return Array.IndexOf(_states, TowerPartState.Carried);
        }

        /// <summary>07 picked up the waiting part at <paramref name="index"/> at <paramref name="position"/>.</summary>
        public void PickUp(int index, Vector3 position)
        {
            if (_states[index] != TowerPartState.Waiting)
            {
                return;
            }

            _states[index] = TowerPartState.Carried;
            _events.Publish(new StoryPartFound(_parts[index].PartId, _parts[index].TowerLevel, position, false));
        }

        /// <summary>
        /// The carried part at <paramref name="index"/> went into the port's hopper at <paramref name="port"/>.
        /// </summary>
        public void Deliver(int index, Vector3 port)
        {
            if (_states[index] != TowerPartState.Carried)
            {
                return;
            }

            _states[index] = TowerPartState.Delivered;
            _events.Publish(new StoryPartDelivered(_parts[index].PartId, _parts[index].TowerLevel, port));
        }

        public TowerPartsSaveData Capture()
        {
            var carried = new List<string>();
            var delivered = new List<string>();
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_states[i] == TowerPartState.Carried)
                {
                    carried.Add(_parts[i].PartId);
                }
                else if (_states[i] == TowerPartState.Delivered)
                {
                    delivered.Add(_parts[i].PartId);
                }
            }

            return new TowerPartsSaveData { carried = carried.ToArray(), delivered = delivered.ToArray() };
        }

        /// <summary>Puts each part where the save says, silently (a loaded game); unknown ids are ignored.</summary>
        public void Restore(TowerPartsSaveData data)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                string id = _parts[i].PartId;
                _states[i] = data != null && Array.IndexOf(data.delivered ?? Array.Empty<string>(), id) >= 0
                    ? TowerPartState.Delivered
                    : data != null && Array.IndexOf(data.carried ?? Array.Empty<string>(), id) >= 0
                        ? TowerPartState.Carried
                        : TowerPartState.Waiting;
            }
        }

        /// <summary>
        /// Tells Rover which parts ride on 07 as the game starts (<see cref="StoryPartFound"/> with Restored, at no
        /// particular position), so they are on its back at once.
        /// </summary>
        public void Announce()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_states[i] == TowerPartState.Carried)
                {
                    _events.Publish(new StoryPartFound(_parts[i].PartId, _parts[i].TowerLevel, Vector3.zero, true));
                }
            }
        }

        private static string Problem(TowerPart[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                TowerPart part = parts[i];
                if (string.IsNullOrEmpty(part.PartId) || part.TowerLevel < 1)
                {
                    return $"Tower part {i} needs its model id and a tower level of 1 or more.";
                }

                if (part.Source == TowerPartSource.SiteHeart && string.IsNullOrEmpty(part.SiteId))
                {
                    return $"Tower part '{part.PartId}' lies in a site's heart but names no site.";
                }

                for (int j = 0; j < i; j++)
                {
                    if (parts[j].TowerLevel == part.TowerLevel || parts[j].PartId == part.PartId)
                    {
                        return $"Tower part '{part.PartId}' shares its level or id with another: one part per level.";
                    }
                }
            }

            return null;
        }
    }
}
