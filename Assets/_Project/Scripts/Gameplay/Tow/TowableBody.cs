using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A load on skids for 07's winch cable: its Rigidbody stands fast (kinematic) until the cable hooks it, slides
    /// on low-friction skids while towed, and stands fast again once it has come to rest after the cable lets go, so
    /// a load never creeps down a slope on its own. Its colliders take the skids' friction. Plain: the load's
    /// GameObject (its body, its colliders and its hitch node) is the caller's.
    /// </summary>
    public sealed class TowableBody : ITowable
    {
        private readonly Transform _hitch;
        private readonly TowSettings _settings;
        private float _restTime;

        /// <param name="hitch">Where the cable meets the load (a node on its front face).</param>
        /// <param name="mass">The load's mass (kg).</param>
        public TowableBody(string id, Rigidbody body, Transform hitch, float mass, TowSettings settings)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A towable load needs an id.", nameof(id));
            }

            Id = id;
            Body = body != null ? body : throw new ArgumentNullException(nameof(body));
            _hitch = hitch != null ? hitch : throw new ArgumentNullException(nameof(hitch));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Collider[] colliders = body.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                throw new InvalidOperationException($"Towable load '{id}' has no collider to slide on.");
            }

            var skids = new PhysicsMaterial(id + "_Skids")
            {
                dynamicFriction = settings.SkidFriction,
                staticFriction = settings.SkidFriction,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            foreach (Collider collider in colliders)
            {
                collider.sharedMaterial = skids;
            }

            body.mass = mass;
            body.linearDamping = settings.LoadDrag;
            body.angularDamping = settings.LoadAngularDrag;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            StandFast();
        }

        public string Id { get; }

        public Rigidbody Body { get; }

        public Transform Root => Body.transform;

        public Vector3 Hitch => _hitch.position;

        public bool IsTowed { get; private set; }

        /// <summary>It stands fast where it is: no physics moves it until the cable hooks it.</summary>
        public bool StandsFast => Body.isKinematic;

        public void BeginTow()
        {
            IsTowed = true;
            _restTime = 0f;
            Body.isKinematic = false;
            Body.useGravity = true;
            Body.WakeUp();
        }

        public void EndTow()
        {
            IsTowed = false;
            _restTime = 0f;
        }

        public void Settle(float deltaTime)
        {
            if (IsTowed || Body.isKinematic)
            {
                return;
            }

            float rest = _settings.RestSpeed;
            _restTime = Body.linearVelocity.sqrMagnitude <= rest * rest ? _restTime + deltaTime : 0f;
            if (_restTime >= _settings.RestSeconds)
            {
                StandFast();
            }
        }

        public void Place(Vector3 position, Quaternion rotation)
        {
            IsTowed = false;
            StandFast();
            Body.transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
        }

        private void StandFast()
        {
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            Body.isKinematic = true;
            Body.useGravity = false;
            _restTime = 0f;
        }
    }
}
