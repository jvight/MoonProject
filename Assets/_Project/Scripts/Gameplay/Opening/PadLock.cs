using System;
using System.Globalization;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The dock lock on a charging pad 07 built (docs/design/opening-v2.md §5: DockLock rebound to the built pad, the
    /// owner's favourite charge moment): the pad's four <c>TotemBar_n</c> are the lock's walls, sunk at rest and rising
    /// staggered as 07 starts charging on it, and its four <c>Stage_n</c> lamps fill one per quarter of the charge,
    /// tinting toward gold when full. The timing and the beats are <see cref="DockLock"/>'s, unchanged, but they begin
    /// once 07 has eased onto the pad's centre line and heading (the rover does the easing, asked by
    /// <see cref="RoverDockChanged"/>); a clear push on the throttle once it has settled lets 07 go at once; charging never waits on any of it. Reads the pad's nodes once each time
    /// 07 comes to a different pad; allocation-free per frame otherwise.
    /// </summary>
    public sealed class PadLock
    {
        private const string BarPrefix = "TotemBar_";
        private const string StagePrefix = "Stage_";

        /// <summary>Metres above the pad's centre a cast down for its deck starts.</summary>
        private const float DockProbeHeight = 3f;

        private readonly EventBus _events;
        private readonly DockLock _lock;
        private readonly BaseTuning _tuning;
        private readonly BuildField _building;
        private readonly BatteryModel _battery;
        private readonly IRoverState _rover;
        private readonly float _radius;
        private readonly Transform[] _bars = new Transform[DockLock.Walls];
        private readonly Vector3[] _barRest = new Vector3[DockLock.Walls];
        private readonly EmissionGlow[] _stages = new EmissionGlow[DockLock.Walls];
        private readonly float[] _stageLevels = new float[DockLock.Walls];
        private int _pad = -1;
        private bool _docked;
        private bool _settled;
        private Vector3 _dockPoint;
        private float _dockYaw;

        /// <param name="radius">Metres around a pad's centre within which 07 is on it.</param>
        public PadLock(EventBus events, BaseTuning tuning, BuildField building, BatteryModel battery,
            IRoverState rover, float radius)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _radius = radius;
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _lock = new DockLock(events, tuning);
        }

        /// <summary>07 stands on a pad it built (powered or not).</summary>
        public bool OnPad { get; private set; }

        /// <summary>The lock's walls are up (rising, locked or lowering) on the pad 07 is on.</summary>
        public bool Raised => _lock.Raised;

        /// <summary>The bar <paramref name="wall"/> of the pad 07 is on, or null.</summary>
        public Transform Bar(int wall)
        {
            return _pad >= 0 ? _bars[wall] : null;
        }

        public void Step(float deltaTime)
        {
            int pad = PadUnder(out StructurePose pose);
            Vector3 centre = pose.Position;
            OnPad = pad >= 0;
            if (pad != _pad && !_lock.Raised)
            {
                Release();
                Rest();
                _pad = pad >= 0 && Read(pad) ? pad : -1;
            }

            if (_pad < 0)
            {
                return;
            }

            bool charging = _battery.IsCharging && _battery.Charger == ChargerKind.Pad && pad == _pad;
            bool driveOff = _settled && Mathf.Abs(_rover.DriveInput.y) > _tuning.DockDriveOff;
            StepDock(charging && !driveOff, pose);
            _lock.Step(pad == _pad, charging && !driveOff && (_settled || _lock.Raised), _battery.Charge,
                _battery.Cells, centre, deltaTime);
            Color tint = Color.Lerp(_tuning.StripCharging, _tuning.StripFull, _lock.Sweep);
            for (int i = 0; i < DockLock.Walls; i++)
            {
                float height = _lock.WallHeight(i);
                _bars[i].localPosition = _barRest[i] + Vector3.down * (_tuning.WallSink * (1f - height));
                float lit = Mathf.Clamp01(_lock.RingFill * DockLock.Walls - i);
                float target = height <= 0f ? 0f : Mathf.Lerp(_tuning.StripUnlit, _tuning.StripLit, lit);
                _stageLevels[i] = Damp.Toward(_stageLevels[i], target, _tuning.StripEase, deltaTime);
                _stages[i].Apply(tint, _stageLevels[i]);
            }
        }

        /// <summary>
        /// Holds 07 on the pad while it charges: asks the rover to ease it onto the pad's centre line and heading, then
        /// notes when it has settled there; lets it go when charging stops or the wheel is touched.
        /// </summary>
        private void StepDock(bool dock, StructurePose pose)
        {
            if (!dock)
            {
                Release();
                return;
            }

            if (!_docked)
            {
                if (!TryDockPoint(pose.Position, out _dockPoint))
                {
                    return;
                }

                _docked = true;
                _dockYaw = Mathf.Repeat(pose.Yaw + _tuning.DockHeadingOffset, 360f);
                _events.Publish(new RoverDockChanged(true, _dockPoint, Quaternion.Euler(0f, _dockYaw, 0f)));
            }

            Vector3 off = _rover.Position - _dockPoint;
            off.y = 0f;
            float turned = Mathf.Abs(Mathf.DeltaAngle(_rover.Rotation.eulerAngles.y, _dockYaw));
            _settled = off.magnitude <= _tuning.DockSettledMetres && turned <= _tuning.DockSettledDegrees;
        }

        /// <summary>07 goes free of the dock (when it held it).</summary>
        private void Release()
        {
            if (!_docked)
            {
                return;
            }

            _docked = false;
            _settled = false;
            _events.Publish(new RoverDockChanged(false, _dockPoint, Quaternion.Euler(0f, _dockYaw, 0f)));
        }

        /// <summary>The top of the pad at its centre, where 07's pivot rests; false (logged) when there is none.</summary>
        private bool TryDockPoint(Vector3 centre, out Vector3 point)
        {
            Vector3 from = centre + Vector3.up * DockProbeHeight;
            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, DockProbeHeight * 2f, Layers.DriveableMask,
                    QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            Debug.LogError($"{nameof(PadLock)}: no ground or pad deck under the pad at {centre} to dock 07 on.");
            point = centre;
            return false;
        }

        /// <summary>The built pad 07 is on, by instance id, or -1.</summary>
        private int PadUnder(out StructurePose pose)
        {
            PlacedBase placed = _building.PlacedBase;
            float radiusSq = _radius * _radius;
            for (int i = 0; i < placed.Count; i++)
            {
                PlacedStructure pad = placed.StructureAt(i);
                if (pad.IsBuilt && pad.Definition.Id == StructureIds.ChargingPad &&
                    SurfaceRules.HorizontalDistanceSquared(_rover.Position, pad.Pose.Position) <= radiusSq)
                {
                    pose = pad.Pose;
                    return pad.InstanceId;
                }
            }

            pose = default;
            return -1;
        }

        /// <summary>The pad's bars and lamps; false (logged) when Art's pad lacks them.</summary>
        private bool Read(int instanceId)
        {
            if (!_building.TryGetView(instanceId, out StructureView view))
            {
                return false;
            }

            for (int i = 0; i < DockLock.Walls; i++)
            {
                string n = (i + 1).ToString(CultureInfo.InvariantCulture);
                Transform bar = Find(view.transform, BarPrefix + n);
                Transform stage = Find(view.transform, StagePrefix + n);
                Renderer lamp = stage != null ? stage.GetComponentInChildren<Renderer>() : null;
                if (bar == null || lamp == null)
                {
                    Debug.LogError($"{nameof(PadLock)}: Art's charging pad has no {BarPrefix}{n} or lit " +
                                   $"{StagePrefix}{n} (the pad's dock-lock contract).", view);
                    return false;
                }

                _bars[i] = bar;
                _barRest[i] = bar.localPosition;
                _stages[i] = new EmissionGlow(lamp);
                _stageLevels[i] = 0f;
            }

            return true;
        }

        /// <summary>The pad 07 leaves stands at rest: its bars back where Art built them, its lamps dark.</summary>
        private void Rest()
        {
            if (_pad < 0)
            {
                return;
            }

            for (int i = 0; i < DockLock.Walls; i++)
            {
                _bars[i].localPosition = _barRest[i];
                _stages[i].Apply(0f);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
