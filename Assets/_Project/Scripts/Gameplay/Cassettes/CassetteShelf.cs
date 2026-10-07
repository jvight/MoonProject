using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tape rack at Bell's corner (docs/features/M3-05 "Cassette shelf"): its slots fill in the order 07 collected
    /// the tapes, each a copy of the tape's own chunky model standing upright with its label out, so progress shows at
    /// the base (DESIGN pillar 7). A newly collected tape settles into its slot with a soft overshoot; a loaded game
    /// shows its tapes already in place. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CassetteShelf : MonoBehaviour
    {
        [Tooltip("The rack's Slot_0..7 empties (+Y up, +Z label facing), in fill order.")]
        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();

        private BellTuning _tuning;
        private RadioProgram _radio;
        private CassetteCatalog _catalog;
        private Transform[] _tapes = Array.Empty<Transform>();
        private Vector3[] _scales = Array.Empty<Vector3>();
        private int[] _slotTape = Array.Empty<int>();
        private float[] _settleStart = Array.Empty<float>();
        private int _shown;
        private bool _initialized;

        /// <summary>Tapes standing on the shelf.</summary>
        public int Shown => _shown;

        /// <summary>The slot <paramref name="slot"/>'s tape, or null while it is empty.</summary>
        public Transform TapeIn(int slot)
        {
            return slot < _shown ? _tapes[_slotTape[slot]] : null;
        }

        internal void Wire(Transform[] slots)
        {
            _slots = slots;
        }

        internal bool Initialize(RadioProgram radio, CassetteCatalog catalog, BellTuning tuning)
        {
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            string problem = tuning == null ? "BellTuning is not assigned."
                : _slots.Length < catalog.Cassettes.Count
                    ? $"{_slots.Length} shelf slots for {catalog.Cassettes.Count} cassettes."
                    : NullSlot();
            if (problem != null)
            {
                Debug.LogError($"{nameof(CassetteShelf)}: {problem}", this);
                enabled = false;
                return false;
            }

            _tuning = tuning;
            IReadOnlyList<CassetteDefinition> cassettes = catalog.Cassettes;
            _tapes = new Transform[cassettes.Count];
            _scales = new Vector3[cassettes.Count];
            for (int i = 0; i < cassettes.Count; i++)
            {
                GameObject tape = Instantiate(cassettes[i].Prefab, transform);
                tape.name = "ShelfTape_" + cassettes[i].Id;
                SetLayer(tape.transform, Layers.Prop);
                tape.SetActive(false);
                _tapes[i] = tape.transform;
                _scales[i] = tape.transform.localScale;
            }

            _slotTape = new int[_slots.Length];
            _settleStart = new float[_slots.Length];
            _initialized = true;
            return true;
        }

        /// <summary>After a load: every owned tape stands in its slot already.</summary>
        internal void Sync()
        {
            while (_shown < _radio.OwnedTapeCount && _shown < _slots.Length)
            {
                Place(float.NegativeInfinity);
            }
        }

        private string NullSlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    return $"Slot_{i} is not wired.";
                }
            }

            return null;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            if (_shown < _radio.OwnedTapeCount && _shown < _slots.Length)
            {
                Place(now);
            }

            for (int slot = 0; slot < _shown; slot++)
            {
                float settle = (now - _settleStart[slot]) / _tuning.ShelfSettle;
                if (settle < 1f)
                {
                    int tape = _slotTape[slot];
                    _tapes[tape].localScale = _scales[tape] * Ease.OutBack(settle, _tuning.ShelfOvershoot);
                }
            }
        }

        private void Place(float now)
        {
            int tape = IndexOf(_radio.GetOwnedTape(_shown));
            Transform slot = _slots[_shown];
            _slotTape[_shown] = tape;
            _settleStart[_shown] = now;
            _tapes[tape].SetPositionAndRotation(slot.position + slot.up * _tuning.ShelfLift, slot.rotation);
            _tapes[tape].localScale = float.IsNegativeInfinity(now) ? _scales[tape] : Vector3.zero;
            _tapes[tape].gameObject.SetActive(true);
            _shown++;
        }

        private int IndexOf(string id)
        {
            IReadOnlyList<CassetteDefinition> cassettes = _catalog.Cassettes;
            for (int i = 0; i < cassettes.Count; i++)
            {
                if (string.Equals(cassettes[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"The radio owns '{id}', which is not in the cassette catalog.");
        }

        private static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayer(root.GetChild(i), layer);
            }
        }
    }
}
