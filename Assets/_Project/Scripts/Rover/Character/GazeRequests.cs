using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The gaze requests behind <c>IRoverRig.SetGazeTarget</c>: one per owner; the highest priority wins and ties go
    /// to the request that started most recently. Updating an owner's target keeps its place in that order, so two
    /// equal requests refreshed every frame never flip-flop. No allocations while within the initial capacity.
    /// </summary>
    public sealed class GazeRequests
    {
        private const int DefaultCapacity = 8;

        private readonly List<Request> _requests;
        private long _nextSequence;

        public GazeRequests(int capacity = DefaultCapacity)
        {
            _requests = new List<Request>(capacity);
        }

        /// <summary>Active requests.</summary>
        public int Count => _requests.Count;

        /// <summary>Adds or updates <paramref name="owner"/>'s request.</summary>
        public void Set(object owner, Vector3 worldPoint, int priority)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            int index = IndexOf(owner);
            if (index >= 0)
            {
                _requests[index] = new Request(owner, worldPoint, priority, _requests[index].Sequence);
                return;
            }

            _requests.Add(new Request(owner, worldPoint, priority, _nextSequence++));
        }

        /// <summary>Withdraws <paramref name="owner"/>'s request; no-op when it has none.</summary>
        public void Clear(object owner)
        {
            int index = IndexOf(owner);
            if (index >= 0)
            {
                _requests.RemoveAt(index);
            }
        }

        /// <summary>The winning request's target, if any request is active.</summary>
        public bool TryGetTop(out Vector3 worldPoint)
        {
            return TryGetTop(out worldPoint, out _, out _);
        }

        /// <summary>The winning request's target and priority, if any request is active.</summary>
        public bool TryGetTop(out Vector3 worldPoint, out int priority)
        {
            return TryGetTop(out worldPoint, out priority, out _);
        }

        /// <summary>The winning request's target, priority, and owner, if any request is active.</summary>
        public bool TryGetTop(out Vector3 worldPoint, out int priority, out object owner)
        {
            int best = -1;
            for (int i = 0; i < _requests.Count; i++)
            {
                if (best < 0 || Outranks(_requests[i], _requests[best]))
                {
                    best = i;
                }
            }

            if (best >= 0)
            {
                worldPoint = _requests[best].Point;
                priority = _requests[best].Priority;
                owner = _requests[best].Owner;
                return true;
            }

            worldPoint = Vector3.zero;
            priority = 0;
            owner = null;
            return false;
        }

        private static bool Outranks(Request candidate, Request current)
        {
            int candRank = EffectivePriority(candidate);
            int currRank = EffectivePriority(current);
            if (candRank != currRank)
            {
                return candRank > currRank;
            }

            bool candCutter = candidate.Owner is ICutterAimOwner && candidate.Priority >= FocusPriority;
            bool currCutter = current.Owner is ICutterAimOwner && current.Priority >= FocusPriority;
            if (candCutter != currCutter)
            {
                return candCutter;
            }

            return candidate.Sequence > current.Sequence;
        }

        private const int FocusPriority = 2;
        private const int CutterFocusEffectivePriority = 4;

        private static int EffectivePriority(Request request)
        {
            if (request.Owner is ICutterAimOwner && request.Priority >= FocusPriority)
            {
                return Mathf.Max(request.Priority, CutterFocusEffectivePriority);
            }

            return request.Priority;
        }

        private int IndexOf(object owner)
        {
            for (int i = 0; i < _requests.Count; i++)
            {
                if (ReferenceEquals(_requests[i].Owner, owner))
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly struct Request
        {
            public Request(object owner, Vector3 point, int priority, long sequence)
            {
                Owner = owner;
                Point = point;
                Priority = priority;
                Sequence = sequence;
            }

            public object Owner { get; }

            public Vector3 Point { get; }

            public int Priority { get; }

            public long Sequence { get; }
        }
    }
}
