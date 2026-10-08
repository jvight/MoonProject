using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tetherable body of a drag piece (a wing, a roof section, a hatch): a box collider on the Relic layer and a
    /// Rigidbody that hangs still on its wreck until the tether latches on. Pulled a few metres clear, it asks the
    /// tether to let go softly and lies loose, ready for the beam to cut it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalvageDrag : MonoBehaviour, ITowable
    {
        private SalvagePiece _piece;
        private Vector3 _hangPosition;
        private float _clearance;

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
        public bool IsClear => SurfaceRules.HorizontalDistance(Position, _hangPosition) >= _clearance;

        bool ITowable.WantsRelease => _piece.State == SalvagePieceState.Attached && IsClear;

        /// <param name="bounds">The piece's mesh bounds in its own space.</param>
        internal void Setup(SalvagePiece piece, Bounds bounds, SalvageTuning tuning, PhysicsMaterial material)
        {
            _piece = piece ?? throw new ArgumentNullException(nameof(piece));
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            _clearance = tuning.DragClearance;
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
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
            SetFree(true);
            Body.Sleep();
        }

        /// <summary>The beam cut it free: it leaves physics to come away and fold.</summary>
        internal void Freeze()
        {
            SetFree(false);
            Collider.enabled = false;
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
