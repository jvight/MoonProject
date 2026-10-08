using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tetherable body of a drag piece (a wing, a roof section, a hatch): a box collider on the Relic layer and a
    /// Rigidbody that hangs still on its wreck until the tether latches on. Pulled a few metres clear, it asks the
    /// tether to let go softly and lies loose, ready for the beam to cut it. Nothing is ever lost (VISION ruling 1): a
    /// piece that comes to rest off the drivable floor, or sinks under it, floats gently back to open ground beside its
    /// site.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalvageDrag : MonoBehaviour, ITowable
    {
        /// <summary>Above this speed (m/s) a piece is still moving and is given time to settle on its own.</summary>
        private const float RestingSpeed = 0.3f;

        /// <summary>Directions tried round the site for open ground to set a lost piece down on.</summary>
        private const int ReturnBearings = 12;

        private SalvagePiece _piece;
        private SalvageTuning _tuning;
        private ITerrainQuery _terrain;
        private Vector3 _hangPosition;
        private float _offFloorTime;
        private float _returnTime;
        private float _returnDuration;
        private Vector3 _returnStart;
        private Quaternion _returnStartRotation;

        public Rigidbody Body { get; private set; }

        public BoxCollider Collider { get; private set; }

        /// <summary>Half of the largest dimension of its bounds (m).</summary>
        public float Radius { get; private set; }

        public bool IsTethered { get; private set; }

        /// <summary>The centre of its bounds in the world.</summary>
        public Vector3 Position => transform.TransformPoint(Collider.center);

        /// <summary>Still hanging on its wreck or lying loose, and not on the tether.</summary>
        public bool IsTetherable => !IsTethered &&
                                    (_piece.State == SalvagePieceState.Attached ||
                                     _piece.State == SalvagePieceState.Loose);

        /// <summary>Pulled at least the clearance (horizontal) away from where it hung.</summary>
        public bool IsClear => SurfaceRules.HorizontalDistance(Position, _hangPosition) >= _tuning.DragClearance;

        /// <summary>Where a returning piece will rest (its pivot), saved in its stead while it floats.</summary>
        internal Vector3 ReturnEnd { get; private set; }

        internal Quaternion ReturnEndRotation { get; private set; } = Quaternion.identity;

        bool ITowable.WantsRelease => _piece.State == SalvagePieceState.Attached && IsClear;

        /// <param name="bounds">The piece's mesh bounds in its own space.</param>
        internal void Setup(SalvagePiece piece, Bounds bounds, SalvageTuning tuning, PhysicsMaterial material,
            ITerrainQuery terrain)
        {
            _piece = piece ?? throw new ArgumentNullException(nameof(piece));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            Collider = gameObject.AddComponent<BoxCollider>();
            Collider.center = bounds.center;
            Collider.size = bounds.size;
            Collider.sharedMaterial = material;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = tuning.DragMass;
            Body.linearDamping = tuning.DragLinearDamping;
            Body.angularDamping = tuning.DragAngularDamping;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.isKinematic = true;
            Body.useGravity = false;
            Radius = Mathf.Max(0.05f, Vector3.Scale(bounds.extents, transform.lossyScale).magnitude);
            _hangPosition = Position;
        }

        /// <summary>A saved loose piece is set down where it lay.</summary>
        internal void RestoreLoose(Vector3 position, Quaternion rotation)
        {
            Place(position, rotation);
            SetFree(true);
            Body.Sleep();
        }

        /// <summary>The beam cut it free: it leaves physics to come away and fold.</summary>
        internal void Freeze()
        {
            SetFree(false);
            Collider.enabled = false;
        }

        /// <summary>
        /// Keeps the no-loss promise each frame: a free piece resting off the drivable floor (or under it) begins to
        /// float back, and a returning one glides on until it is set down. Allocation-free.
        /// </summary>
        internal void StepReach(float deltaTime)
        {
            if (_piece.State == SalvagePieceState.Returning)
            {
                StepReturn(deltaTime);
                return;
            }

            bool free = (_piece.State == SalvagePieceState.Attached || _piece.State == SalvagePieceState.Loose) &&
                        !IsTethered && !Body.isKinematic;
            if (!free)
            {
                _offFloorTime = 0f;
                return;
            }

            Vector3 position = Position;
            if (position.y < _terrain.SampleHeight(position.x, position.z) - _tuning.BelowSurfaceLimit)
            {
                BeginReturn();
                return;
            }

            bool resting = Body.linearVelocity.sqrMagnitude < RestingSpeed * RestingSpeed;
            if (_terrain.IsDrivable(position.x, position.z) || !resting)
            {
                _offFloorTime = 0f;
                return;
            }

            _offFloorTime += deltaTime;
            if (_offFloorTime >= _tuning.ReturnDelay)
            {
                BeginReturn();
            }
        }

        private void BeginReturn()
        {
            _offFloorTime = 0f;
            _returnTime = 0f;
            _returnStart = transform.position;
            _returnStartRotation = transform.rotation;
            ReturnEndRotation = Quaternion.Euler(0f, _returnStartRotation.eulerAngles.y, 0f);
            Vector3 spot = OpenGroundBesideSite();

            // Set its centre down a radius above the ground, so no part of it starts inside the floor.
            Vector3 centre = SurfaceRules.OnSurface(_terrain, spot.x, spot.z) + Vector3.up * Radius;
            ReturnEnd = centre - ReturnEndRotation * Vector3.Scale(Collider.center, transform.lossyScale);
            _returnDuration = _tuning.ReturnDuration +
                              Vector3.Distance(_returnStart, ReturnEnd) * _tuning.ReturnPerMetre;
            SetFree(false);
            Collider.enabled = false;
            _piece.State = SalvagePieceState.Returning;
        }

        private void StepReturn(float deltaTime)
        {
            _returnTime += deltaTime;
            float progress = _returnTime / _returnDuration;
            Place(GlidePath.Position(_returnStart, ReturnEnd, progress, _tuning.ReturnHover, _tuning.ReturnHover, 0f),
                GlidePath.Rotation(_returnStartRotation, ReturnEndRotation, progress));
            if (progress < 1f)
            {
                return;
            }

            Collider.enabled = true;
            SetFree(true);
            _piece.State = IsClear ? SalvagePieceState.Loose : SalvagePieceState.Attached;
        }

        /// <summary>
        /// Open ground just outside the site's footprint, on the side the piece was lost toward if that side is on the
        /// drivable floor, else the nearest side round the site that is.
        /// </summary>
        private Vector3 OpenGroundBesideSite()
        {
            SalvageSite site = _piece.Site;
            Vector3 centre = site.Position;
            Vector3 away = Position - centre;
            away.y = 0f;
            float bearing = SurfaceRules.Bearing(away.sqrMagnitude > 1e-6f ? away : -site.Root.forward);
            float distance = site.Radius + _tuning.ReturnMargin;
            for (int i = 0; i < ReturnBearings; i++)
            {
                // Try the lost side first, then fan out to either side of it.
                float turn = 360f / ReturnBearings * ((i + 1) / 2) * (i % 2 == 0 ? 1f : -1f);
                Vector3 spot = centre + SurfaceRules.BearingDirection(bearing + turn) * distance;
                if (SurfaceRules.InsideDrivable(_terrain, spot.x, spot.z, _tuning.ReturnMargin))
                {
                    return spot;
                }
            }

            return centre + SurfaceRules.BearingDirection(bearing) * distance;
        }

        private void Place(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
        }

        void ITowable.BeginTow()
        {
            IsTethered = true;
            SetFree(true);
        }

        void ITowable.EndTow()
        {
            IsTethered = false;
            if (_piece.State == SalvagePieceState.Attached && IsClear)
            {
                _piece.State = SalvagePieceState.Loose;
            }
        }

        void ITowable.SetAimHighlight(float level)
        {
            _piece.TetherHighlight = Mathf.Max(0f, level);
        }

        private void SetFree(bool free)
        {
            if (!free && !Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            Body.isKinematic = !free;
            Body.useGravity = free;
            if (free)
            {
                Body.WakeUp();
            }
        }
    }
}
