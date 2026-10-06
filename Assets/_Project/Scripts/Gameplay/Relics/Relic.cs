using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One relic in the world: the Art prefab as its visual, a Rigidbody and box collider on the Relic layer (active
    /// only while it is loose), a halo shell that brightens when it is aimed at, surfacing or on display, and its
    /// state, excavation progress and shelf slot. The gameplay systems move it; it only keeps itself alive-looking
    /// (halo easing, the bob of a relic waiting half-lifted in the dust).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Relic : MonoBehaviour
    {
        private const string VisualName = "Visual";
        private const string HaloName = "Halo";

        /// <summary>Collider size (m) of a relic whose Art model has not landed yet (it never leaves the ground).</summary>
        private const float ModelPendingSize = 0.5f;

        private readonly List<GlowRenderer> _halos = new List<GlowRenderer>();
        private RelicTuning _tuning;
        private float _aimHighlight;
        private float _halo;
        private Vector3 _restPosition;
        private Quaternion _restRotation = Quaternion.identity;
        private bool _lifting;

        // The last known pose, kept in plain fields so a save captured while the scene is being torn down (after
        // this object's transform is gone) still has it.
        private Vector3 _knownPosition;
        private Quaternion _knownRotation = Quaternion.identity;

        public RelicDefinition Definition { get; private set; }

        /// <summary>Position in the catalog (stable).</summary>
        public int Index { get; private set; }

        public RelicSite Site { get; private set; }

        public RelicState State { get; private set; }

        /// <summary>Excavation progress 0..1; never goes back down.</summary>
        public float Progress { get; private set; }

        /// <summary>True once the relic has answered a ping.</summary>
        public bool Discovered { get; private set; }

        /// <summary>Museum shelf slot while depositing or displayed, else -1.</summary>
        public int Slot { get; private set; } = -1;

        /// <summary>Set by the tether while it holds this relic.</summary>
        public bool IsTethered { get; internal set; }

        public Rigidbody Body { get; private set; }

        public BoxCollider Collider { get; private set; }

        /// <summary>Half of the largest dimension of the relic's bounds (m).</summary>
        public float Radius { get; private set; }

        /// <summary>Where the relic rests when fully buried (top just below the surface).</summary>
        public Vector3 BuriedPosition { get; private set; }

        /// <summary>
        /// Can the beam lift it? Only a relic whose Art model exists (the others stay buried sites for now) and that
        /// is still in the ground.
        /// </summary>
        public bool CanBeLifted => Definition.HasModel && (State == RelicState.Buried || State == RelicState.Surfacing);

        /// <summary>A loose relic nobody holds: the only kind the tether may grab.</summary>
        public bool IsTetherable => State == RelicState.Loose && !IsTethered;

        /// <summary>Still somewhere in the world (not on its way to, or on, the shelf), so it answers the sonar.</summary>
        public bool AnswersSonar => State != RelicState.Depositing && State != RelicState.Displayed && !IsTethered;

        /// <summary>The ground point a sonar answer or marker refers to.</summary>
        public Vector3 SonarPosition =>
            State == RelicState.Buried || State == RelicState.Surfacing ? Site.Position : transform.position;

        internal void Setup(RelicDefinition definition, int index, RelicSite site, RelicTuning tuning,
            PhysicsMaterial physicsMaterial, Material haloMaterial)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            Index = index;
            Site = site;
            gameObject.layer = Layers.Relic;

            var bounds = new Bounds(Vector3.zero, Vector3.one * ModelPendingSize);
            if (definition.HasModel)
            {
                GameObject visual = Instantiate(definition.Prefab, transform, false);
                visual.name = VisualName;
                bounds = LocalBounds(visual.transform);
                SetLayer(visual.transform, Layers.Relic);
                BuildHalos(visual.transform, haloMaterial);
            }

            Radius = Mathf.Max(0.05f, bounds.extents.magnitude);

            Collider = gameObject.AddComponent<BoxCollider>();
            Collider.center = bounds.center;
            Collider.size = bounds.size;
            Collider.sharedMaterial = physicsMaterial;

            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = definition.Mass;
            Body.linearDamping = tuning.LinearDamping;
            Body.angularDamping = tuning.AngularDamping;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.centerOfMass = Vector3.zero;

            float depth = bounds.max.y + tuning.BurialClearance;
            BuriedPosition = site.Position - Vector3.up * depth;
            Bury();
        }

        /// <summary>How strongly the tether's aim highlights this relic (0 = not aimed at, 1 = the hovered target).</summary>
        internal void SetAimHighlight(float level)
        {
            _aimHighlight = Mathf.Max(0f, level);
        }

        internal void MarkDiscovered()
        {
            Discovered = true;
        }

        /// <summary>The beam took hold of a buried or partly lifted relic.</summary>
        internal void BeginLift()
        {
            if (State != RelicState.Buried && State != RelicState.Surfacing)
            {
                throw new InvalidOperationException($"{Definition.Id} cannot be lifted while {State}.");
            }

            State = RelicState.Surfacing;
            _lifting = true;
        }

        /// <summary>Moves a lifting relic and records its excavation progress.</summary>
        internal void SetLiftPose(Vector3 position, Quaternion rotation, float progress)
        {
            Progress = Mathf.Max(Progress, Mathf.Clamp01(progress));
            _restPosition = position;
            _restRotation = rotation;
            Place(position, rotation);
        }

        /// <summary>The beam let go early: the relic waits where it is, gently bobbing, progress kept.</summary>
        internal void PauseLift()
        {
            _lifting = false;
        }

        /// <summary>Fully surfaced: hand the relic to physics with a soft hop.</summary>
        internal void Surface(Vector3 velocity, Vector3 angularVelocity)
        {
            Progress = 1f;
            _lifting = false;
            MakeLoose(velocity, angularVelocity);
        }

        /// <summary>Takes a relic out of physics to be carried along a scripted path (shelf, return).</summary>
        internal void BeginCarry(RelicState carryState, int slot)
        {
            if (carryState != RelicState.Depositing && carryState != RelicState.Returning)
            {
                throw new ArgumentOutOfRangeException(nameof(carryState), carryState, "Not a carried state.");
            }

            State = carryState;
            Slot = carryState == RelicState.Depositing ? slot : -1;
            SetPhysics(false);
        }

        /// <summary>Moves a carried (kinematic) relic.</summary>
        internal void SetCarryPose(Vector3 position, Quaternion rotation)
        {
            Place(position, rotation);
        }

        internal void SetDisplayed(int slot)
        {
            State = RelicState.Displayed;
            Slot = slot;
            SetPhysics(false);
        }

        /// <summary>A carried relic is set down as a free body again (after drifting back within reach).</summary>
        internal void Release()
        {
            MakeLoose(Vector3.zero, Vector3.zero);
        }

        internal RelicSaveData Capture()
        {
            return new RelicSaveData
            {
                id = Definition.Id,
                state = (int)State,
                progress = Progress,
                discovered = Discovered,
                position = _knownPosition,
                rotation = _knownRotation,
                slot = Slot,
            };
        }

        /// <summary>Applies saved state. Carried relics come back where they were heading (see RelicField).</summary>
        internal void Restore(RelicState state, float progress, bool discovered, Vector3 position,
            Quaternion rotation, int slot)
        {
            _lifting = false;
            switch (state)
            {
                case RelicState.Buried:
                    Bury();
                    break;
                case RelicState.Surfacing:
                    State = RelicState.Surfacing;
                    SetPhysics(false);
                    _restPosition = position;
                    _restRotation = rotation;
                    Place(position, rotation);
                    break;
                case RelicState.Displayed:
                    Place(position, rotation);
                    SetDisplayed(slot);
                    break;
                default:
                    Place(position, rotation);
                    MakeLoose(Vector3.zero, Vector3.zero);
                    Body.Sleep();
                    break;
            }

            Discovered = discovered;
            Progress = state == RelicState.Buried ? 0f : Mathf.Clamp01(progress);
        }

        private void Bury()
        {
            State = RelicState.Buried;
            Progress = 0f;
            Slot = -1;
            SetPhysics(false);
            _restPosition = BuriedPosition;
            _restRotation = Quaternion.identity;
            Place(BuriedPosition, Quaternion.identity);
        }

        private void Place(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _knownPosition = position;
            _knownRotation = rotation;
        }

        private void MakeLoose(Vector3 velocity, Vector3 angularVelocity)
        {
            State = RelicState.Loose;
            Slot = -1;
            SetPhysics(true);
            Body.linearVelocity = velocity;
            Body.angularVelocity = angularVelocity;
        }

        private void SetPhysics(bool active)
        {
            if (active)
            {
                Collider.enabled = true;
                Body.isKinematic = false;
                Body.useGravity = true;
                Body.WakeUp();
            }
            else
            {
                if (!Body.isKinematic)
                {
                    Body.linearVelocity = Vector3.zero;
                    Body.angularVelocity = Vector3.zero;
                }

                Body.isKinematic = true;
                Body.useGravity = false;
                Collider.enabled = false;
            }
        }

        private void Update()
        {
            if (_tuning == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float stateGlow = State == RelicState.Surfacing ? _tuning.SurfacingGlow
                : State == RelicState.Displayed || State == RelicState.Depositing ? _tuning.DisplayGlow
                : 0f;
            _halo = Damp.Toward(_halo, Mathf.Max(stateGlow, _aimHighlight), _tuning.HaloEase, deltaTime);
            for (int i = 0; i < _halos.Count; i++)
            {
                _halos[i].Apply(_halo);
            }

            if (State == RelicState.Surfacing && !_lifting)
            {
                float bob = Mathf.Sin(Time.time * 2f * Mathf.PI * _tuning.BobFrequency + Index) * _tuning.WaitingBob;
                transform.SetPositionAndRotation(_restPosition + Vector3.up * bob, _restRotation);
            }

            _knownPosition = transform.position;
            _knownRotation = transform.rotation;
        }

        private void BuildHalos(Transform visual, Material haloMaterial)
        {
            MeshFilter[] filters = visual.GetComponentsInChildren<MeshFilter>(true);
            float scale = _tuning.HaloScale;
            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                MeshRenderer halo = GlowObject.Create(HaloName, filter.transform, filter.sharedMesh, haloMaterial);
                halo.gameObject.layer = Layers.Relic;
                Vector3 centre = filter.sharedMesh.bounds.center;
                halo.transform.localPosition = centre * (1f - scale);
                halo.transform.localScale = Vector3.one * scale;
                _halos.Add(new GlowRenderer(halo));
            }
        }

        private Bounds LocalBounds(Transform visual)
        {
            MeshFilter[] filters = visual.GetComponentsInChildren<MeshFilter>(true);
            bool any = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Bounds local = mesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = local.center + Vector3.Scale(local.extents, offset);
                    Vector3 rootPoint = transform.InverseTransformPoint(filter.transform.TransformPoint(point));
                    if (any)
                    {
                        bounds.Encapsulate(rootPoint);
                    }
                    else
                    {
                        bounds = new Bounds(rootPoint, Vector3.zero);
                        any = true;
                    }
                }
            }

            if (!any)
            {
                throw new InvalidOperationException(
                    $"Relic prefab {Definition.Prefab.name} has no meshes; check the Art content contract.");
            }

            return bounds;
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
