using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// An <see cref="IStationReach"/> the test drives: home first (lit, with the radio's 60 m clear radius), then
    /// masts with a 110 m reach that the test lights. Allocation-free queries, like the real one.
    /// </summary>
    public sealed class FakeStationReach : IStationReach
    {
        public const float HomeRadius = 60f;
        public const float MastReach = 110f;

        private readonly List<RelayNode> _nodes = new List<RelayNode>();

        public FakeStationReach(Vector3 home)
        {
            _nodes.Add(new RelayNode("home", home, true));
        }

        public int NodeCount => _nodes.Count;

        public int LitCount
        {
            get
            {
                int lit = 0;
                for (int i = 0; i < _nodes.Count; i++)
                {
                    lit += _nodes[i].Lit ? 1 : 0;
                }

                return lit;
            }
        }

        public RelayNode GetNode(int index)
        {
            return _nodes[index];
        }

        public bool IsInReach(Vector3 position)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                float radius = i == 0 ? HomeRadius : MastReach;
                if (_nodes[i].Lit && SignalField.HorizontalDistance(position, _nodes[i].Position) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        public float DistanceToNearestNode(Vector3 position)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].Lit)
                {
                    nearest = Mathf.Min(nearest, SignalField.HorizontalDistance(position, _nodes[i].Position));
                }
            }

            return nearest;
        }

        public void AddMast(string id, Vector3 position)
        {
            _nodes.Add(new RelayNode(id, position, false));
        }

        public void Light(string id)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (string.Equals(_nodes[i].Id, id, StringComparison.Ordinal))
                {
                    _nodes[i] = new RelayNode(id, _nodes[i].Position, true);
                }
            }
        }
    }
}
