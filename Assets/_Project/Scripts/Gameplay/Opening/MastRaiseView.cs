using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The antenna mast's raise, seen (docs/design/opening-v3-plan.md 3.3, <see cref="MastRaise"/>): Art's mast is
    /// authored raised, with its lattice in <c>Mast_Seg1..N</c> (each pivoted at its own base). When the mast is built
    /// live every segment starts lowered, telescoped down into the cabinet, and lifts home in order along the mast's
    /// own up as <see cref="MastRaise.Lift01"/> says, giving a little at the lock. A mast built in an earlier session,
    /// and one whose raise is done, stands exactly as authored. Allocation-free per frame.
    /// </summary>
    public sealed class MastRaiseView : IDisposable
    {
        private const string SegmentPrefix = "Mast_Seg";

        private readonly BuildField _building;
        private readonly MastRaise _raise;
        private readonly CallReplySettings _settings;
        private readonly IDisposable _built;
        private Transform[] _segments = Array.Empty<Transform>();
        private Vector3[] _rest = Array.Empty<Vector3>();
        private Vector3[] _lift = Array.Empty<Vector3>();
        private bool _bound;

        public MastRaiseView(EventBus events, BuildField building, MastRaise raise, CallReplySettings settings)
        {
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _raise = raise ?? throw new ArgumentNullException(nameof(raise));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _built = events.Subscribe<StructureBuilt>(OnBuilt);
        }

        /// <summary>The segments are bound and being driven (tests).</summary>
        public bool IsBound => _bound;

        /// <summary>Metres segment <paramref name="index"/> sits below its authored place now (tests).</summary>
        public float Lowered(int index)
        {
            return _bound ? Vector3.Dot(_rest[index] - _segments[index].localPosition, _lift[index].normalized) : 0f;
        }

        public void Step()
        {
            if (!_bound)
            {
                if (_raise.Active)
                {
                    Bind(_raise.InstanceId);
                }

                if (!_bound)
                {
                    return;
                }
            }

            if (!_raise.Active && _raise.Done)
            {
                Place(true);
                _bound = false;
                return;
            }

            Place(false);
        }

        public void Dispose()
        {
            _built.Dispose();
        }

        private void OnBuilt(StructureBuilt built)
        {
            if (built.Restored || !string.Equals(built.StructureId, StructureIds.AntennaMast, StringComparison.Ordinal))
            {
                return;
            }

            Bind(built.InstanceId);
            if (_bound)
            {
                Place(false);
            }
        }

        private void Bind(int instanceId)
        {
            if (!_building.TryGetView(instanceId, out StructureView view))
            {
                return;
            }

            int count = _settings.Segments;
            var segments = new Transform[count];
            var rest = new Vector3[count];
            var lift = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Transform segment = Find(view.transform, SegmentPrefix + (i + 1).ToString());
                if (segment == null)
                {
                    Debug.LogError($"{nameof(MastRaiseView)}: the antenna mast has no '{SegmentPrefix}{i + 1}' node " +
                                   "(Art's AntennaMast, run Art/Structures).", view);
                    return;
                }

                segments[i] = segment;
                rest[i] = segment.localPosition;
                lift[i] = segment.parent.InverseTransformDirection(view.transform.up);
            }

            _segments = segments;
            _rest = rest;
            _lift = lift;
            _bound = true;
        }

        /// <summary>Puts every segment where the raise says; <paramref name="home"/> sets them exactly as authored.</summary>
        private void Place(bool home)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] == null)
                {
                    // The mast was taken down while it was being raised: its nodes are gone, there is nothing to drive.
                    _bound = false;
                    return;
                }

                if (home)
                {
                    _segments[i].localPosition = _rest[i];
                    continue;
                }

                float lowered = _settings.SegmentTravelMetres * (i + 1) * (1f - _raise.Lift01(i));
                float give = _raise.SettleGive(i) * _settings.SettleMetres;
                _segments[i].localPosition = _rest[i] - _lift[i] * (lowered + give);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
