using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kestrel-3's loose trail bits (docs/features/M3-13), the only loose salvage in the world: each lies half-sunk in
    /// the dust along the trail anchor's forward, tilted, with a cyan glint that reads from afar. Driving within the
    /// magnet radius lifts one off; it spirals into 07's cargo socket and arrives (one at a time, with the shared
    /// cadence) to be folded into the stock by <see cref="SalvageField"/>. Built once; a frame allocates nothing.
    /// </summary>
    internal sealed class SalvageTrail : IDisposable
    {
        /// <summary>Degrees between consecutive bits' resting yaw (the golden angle: no two look alike).</summary>
        private const float GoldenAngle = 137.50776f;

        private readonly SalvageTuning _tuning;
        private readonly Transform[] _bits;
        private readonly SalvageMaterial[] _material;
        private readonly Vector3[] _rest;
        private readonly Quaternion[] _restRotation;
        private readonly Vector3[] _scale;
        private readonly Vector3[] _flightStart;
        private readonly float[] _time;
        private readonly float[] _duration;
        private readonly BitState[] _state;

        private enum BitState : byte
        {
            Waiting = 0,
            Flying = 1,
            Taken = 2,
        }

        private SalvageTrail(SalvageTuning tuning, int count, PickupGlints glints)
        {
            _tuning = tuning;
            _bits = new Transform[count];
            _material = new SalvageMaterial[count];
            _rest = new Vector3[count];
            _restRotation = new Quaternion[count];
            _scale = new Vector3[count];
            _flightStart = new Vector3[count];
            _time = new float[count];
            _duration = new float[count];
            _state = new BitState[count];
            Glints = glints;
            Combo = new ComboCounter(tuning.ChainWindow);
        }

        public int Count => _bits.Length;

        /// <summary>The bits' horizon glints (tests read how many were drawn).</summary>
        public PickupGlints Glints { get; }

        /// <summary>The trail's melody: a run of bits climbs a step each.</summary>
        public ComboCounter Combo { get; }

        /// <summary>The nearest waiting bit within the glance radius after the last step, or -1.</summary>
        public int Nearest { get; private set; } = -1;

        /// <summary>
        /// Lays <paramref name="bits"/> along <paramref name="anchor"/>'s forward under <paramref name="parent"/>. Null
        /// (with the problem) when a bit has no mesh to sink into the dust.
        /// </summary>
        public static SalvageTrail Lay(Transform parent, IReadOnlyList<SalvageTrailBit> bits, WorldAnchor anchor,
            ITerrainQuery terrain, SalvageTuning tuning, Material glint, GlintTuning glints, out string problem)
        {
            var trail = new SalvageTrail(tuning, bits.Count,
                new PickupGlints(parent, glint, glints, Mathf.Max(1, bits.Count), Layers.Pickup));
            Vector3 forward = anchor.Forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            for (int i = 0; i < bits.Count; i++)
            {
                SalvageTrailBit bit = bits[i];
                Vector3 along = anchor.Position + forward * bit.Distance;
                Vector3 ground = SurfaceRules.OnSurface(terrain, along.x, along.z);
                float tilt = tuning.TrailTilt;
                Quaternion rotation = Quaternion.Euler(tilt * Mathf.Sin(i), GoldenAngle * i, tilt * Mathf.Cos(i));
                GameObject piece = UnityEngine.Object.Instantiate(bit.Prefab, ground, rotation, parent);
                piece.name = bit.Prefab.name;
                SalvageSiteBuilder.SetLayer(piece.transform, Layers.Pickup);
                Renderer look = piece.GetComponentInChildren<Renderer>();
                if (look == null)
                {
                    problem = $"trail bit '{bit.Prefab.name}' has no mesh";
                    trail.Dispose();
                    return null;
                }

                // Sink the bit's share of its height into the dust (its bounds are world-aligned as it stands).
                Bounds bounds = look.bounds;
                float bottom = ground.y - tuning.TrailSink * bounds.size.y;
                trail._rest[i] = ground + Vector3.up * (bottom - bounds.min.y);
                piece.transform.position = trail._rest[i];
                trail._bits[i] = piece.transform;
                trail._material[i] = bit.Material;
                trail._restRotation[i] = rotation;
                trail._scale[i] = piece.transform.localScale;
            }

            problem = null;
            return trail;
        }

        public bool IsWaiting(int index)
        {
            return _state[index] == BitState.Waiting;
        }

        public bool IsTaken(int index)
        {
            return _state[index] == BitState.Taken;
        }

        public Vector3 Position(int index)
        {
            return _rest[index];
        }

        public SalvageMaterial Material(int index)
        {
            return _material[index];
        }

        /// <summary>A saved bit is already in 07's stock: it is gone from the trail.</summary>
        public void MarkTaken(int index)
        {
            _state[index] = BitState.Taken;
            _bits[index].gameObject.SetActive(false);
        }

        /// <summary>
        /// Glints the waiting bits, lifts off those within the magnet radius of <paramref name="rover"/> and flies the
        /// rest in. True when a bit arrives at the cargo socket this frame (its index in <paramref name="arrived"/>);
        /// <paramref name="launched"/> counts the bits that lifted off.
        /// </summary>
        public bool Step(float now, float deltaTime, Vector3 rover, Vector3 socket, Vector3 camera,
            PickupCadence cadence, out int arrived, out int launched)
        {
            arrived = -1;
            launched = 0;
            float magnetSq = _tuning.MagnetRadius * _tuning.MagnetRadius;
            float nearestSq = _tuning.GlanceRadius * _tuning.GlanceRadius;
            Nearest = -1;
            Glints.Begin(camera, now);
            for (int i = 0; i < _bits.Length; i++)
            {
                switch (_state[i])
                {
                    case BitState.Waiting:
                    {
                        Glints.Add(_rest[i], i * GoldenAngle);
                        float distanceSq = SurfaceRules.HorizontalDistanceSquared(_rest[i], rover);
                        if (distanceSq <= magnetSq)
                        {
                            Launch(i, socket);
                            launched++;
                        }
                        else if (distanceSq < nearestSq)
                        {
                            nearestSq = distanceSq;
                            Nearest = i;
                        }

                        break;
                    }

                    case BitState.Flying:
                        if (Fly(i, socket, now, deltaTime, cadence))
                        {
                            arrived = i;
                        }

                        break;
                }
            }

            Glints.End();
            return arrived >= 0;
        }

        private void Launch(int index, Vector3 socket)
        {
            _state[index] = BitState.Flying;
            _flightStart[index] = _rest[index];
            _time[index] = 0f;
            _duration[index] = PickupFlight.Duration(Vector3.Distance(_rest[index], socket), _tuning.FlightDuration,
                _tuning.FlightPerMetre);
        }

        /// <returns>True when the bit arrived (and is gone).</returns>
        private bool Fly(int index, Vector3 socket, float now, float deltaTime, PickupCadence cadence)
        {
            _time[index] += deltaTime;
            float progress = _time[index] / _duration[index];
            if (progress >= 1f && cadence.TryClaim(now))
            {
                MarkTaken(index);
                return true;
            }

            float direction = (index & 1) == 0 ? 1f : -1f;
            _bits[index].SetPositionAndRotation(
                PickupFlight.Evaluate(_flightStart[index], socket, progress, index * GoldenAngle, direction,
                    _tuning.FlightLift, _tuning.SpiralRadius, _tuning.SpiralTurns),
                Quaternion.Euler(0f, _time[index] * _tuning.FlightSpin, 0f) * _restRotation[index]);
            _bits[index].localScale = _scale[index] *
                                      Mathf.Lerp(1f, _tuning.ArrivalScale, Ease.InOutSine(Mathf.Clamp01(progress)));
            return false;
        }

        public void Dispose()
        {
            Glints.Dispose();
        }
    }
}
