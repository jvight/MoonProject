using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A structure 07 has taken down, coming apart on screen (the reverse of the print): the standing view eases down,
    /// splits into two or three pieces that float to 07 and shrink as they arrive under a soft cool beam, and a small
    /// puff of dust marks where it stood. The model side (removal, the full refund) is already done when this starts;
    /// this is only the picture, so it runs once and ends by itself.
    /// </summary>
    public sealed class TakeApart
    {
        private readonly BuildingTuning _tuning;
        private readonly TakeApartMotion _motion;
        private readonly Transform _root;
        private readonly Transform[] _pivots;
        private readonly Vector3[] _starts;
        private readonly Vector3[] _scales;
        private readonly Vector3 _basePosition;
        private readonly Vector3 _centre;
        private readonly Func<Vector3> _destination;
        private readonly Action<GameObject> _destroy;
        private float _elapsed;

        /// <param name="view">The removed structure's view: it is taken over and destroyed when the run ends.</param>
        /// <param name="destination">Where the pieces go (07's body), read each frame.</param>
        /// <param name="destroy">How the view is destroyed at the end (the engine's Destroy when null).</param>
        public TakeApart(StructureView view, BuildingTuning tuning, Func<Vector3> destination,
            Action<GameObject> destroy = null)
        {
            _destroy = destroy ?? (go => UnityEngine.Object.Destroy(go));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _destination = destination ?? throw new ArgumentNullException(nameof(destination));
            _root = view != null ? view.transform : throw new ArgumentNullException(nameof(view));
            _basePosition = _root.position;
            var parts = new List<Transform>();
            foreach (Transform child in _root)
            {
                parts.Add(child);
            }

            int count = Mathf.Max(1, Mathf.Min(tuning.TakeApartPieces, parts.Count));
            _motion = new TakeApartMotion(tuning.TakeApartEaseShare, count);
            _pivots = new Transform[count];
            _starts = new Vector3[count];
            _scales = new Vector3[count];
            Vector3 sum = Vector3.zero;
            for (int piece = 0; piece < count; piece++)
            {
                int from = piece * parts.Count / count;
                int to = (piece + 1) * parts.Count / count;
                Vector3 centre = Vector3.zero;
                int measured = 0;
                for (int i = from; i < to; i++)
                {
                    centre += CentreOf(parts[i]);
                    measured++;
                }

                centre = measured > 0 ? centre / measured : _basePosition;
                var pivot = new GameObject("TakeApartPiece" + piece).transform;
                pivot.SetParent(_root, false);
                pivot.position = centre;
                for (int i = from; i < to; i++)
                {
                    parts[i].SetParent(pivot, true);
                }

                _pivots[piece] = pivot;
                _starts[piece] = centre;
                _scales[piece] = pivot.localScale;
                sum += centre;
            }

            _centre = sum / count;
        }

        /// <summary>The view's centre in the world (where the beam lands).</summary>
        public Vector3 Centre => _centre;

        public bool IsDone { get; private set; }

        /// <summary>True while the beam should be reaching out.</summary>
        public bool BeamOut => !IsDone && !_motion.BeamRetracting(_elapsed / Duration);

        private float Duration => Mathf.Max(0.1f, _tuning.TakeApartSeconds);

        /// <summary>
        /// Advances the take-apart. True on the frame it ends (the view is destroyed there and <paramref name="dust"/>
        /// puffs where it stood, by <paramref name="dust"/> (position, grains, strength)); false before and after.
        /// </summary>
        public bool Step(float deltaTime, Action<Vector3, int, float> dust)
        {
            if (IsDone)
            {
                return false;
            }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / Duration);
            Vector3 sunk = Vector3.down * (_motion.Sink01(t) * _tuning.TakeApartSinkMetres);
            _root.position = _basePosition + sunk;
            Vector3 target = _destination();
            for (int i = 0; i < _pivots.Length; i++)
            {
                float flight = _motion.Flight01(i, t);
                float scale = _motion.Scale(i, t);
                _pivots[i].position = Vector3.Lerp(_starts[i] + sunk, target, flight) +
                                      Vector3.up * TakeApartMotion.Arc(flight, _tuning.TakeApartArcMetres);
                _pivots[i].localScale = _scales[i] * scale;
                _pivots[i].gameObject.SetActive(scale > TakeApartMotion.ArrivalScale * 1.05f);
            }

            if (t < 1f)
            {
                return false;
            }

            IsDone = true;
            dust(_basePosition, _tuning.TakeApartDustCount, _tuning.TakeApartDustStrength);
            _destroy(_root.gameObject);
            return true;
        }

        /// <summary>Ends the picture now (a second take-apart begins): the view goes with its dust.</summary>
        public void Finish(Action<Vector3, int, float> dust)
        {
            if (IsDone)
            {
                return;
            }

            _elapsed = Duration;
            Step(0f, dust);
        }

        private static Vector3 CentreOf(Transform part)
        {
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return part.position;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }
    }
}
