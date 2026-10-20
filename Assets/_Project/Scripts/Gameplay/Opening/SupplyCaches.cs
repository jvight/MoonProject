using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's supply crates (docs/design/opening-v2.md §4, §10; no floating loot): Art's containers stood by
    /// world anchors and Lumen ruin pieces, cut open with the cutter like any sealed container
    /// (<see cref="SealedContainers"/>), or standing open from the start (the lander's, B4: no cutting before the
    /// igniter). As a crate stands open its yield goes into 07's hold, each material with the salvage note: an open
    /// one offers it within the collect reach and gives it on a held E (Pick up), a cut-open one gives it once 07 is
    /// within the beam's reach. What the hold has no room for stays in the crate and comes out the same way; a story part (the spare igniter) comes out with
    /// <see cref="StoryPartCollected"/>. Allocation-free per frame.
    /// </summary>
    public sealed class SupplyCaches : IDisposable, ISupplyPurposes
    {
        /// <summary>Name key of an open crate's contents, after the Pick up verb.</summary>
        private const string CrateContentsNoun = "pickup.crate.contents";

        /// <summary>Art's open cargo crate's soft rim glow (Crate_Cargo_Open).</summary>
        private const string RimGlowName = "RimGlow";

        /// <summary>The cargo crate's side panel, folded flat onto the ground: 07 rolls over it, so it is no shell.</summary>
        private const string OpenPanelName = "SidePanel_Open";

        /// <summary>A frame's forward laid flat shorter than this is not a heading (it points up or down).</summary>
        private const float LevelShare = 0.5f;

        private readonly List<SupplyCache> _caches = new List<SupplyCache>();
        private readonly List<SealedContainer> _containers = new List<SealedContainer>();
        private readonly Recipe[] _left;
        private readonly Func<bool>[] _heldBack;
        private readonly bool[] _partLeft;
        private readonly List<Quaternion> _levels = new List<Quaternion>();
        private readonly EventBus _events;
        private readonly MaterialStock _stock;
        private readonly IRoverState _rover;
        private readonly ISaveService _save;
        private readonly InputReader _input;
        private readonly PickupReach _pickups;
        private readonly float _reach;
        private readonly float _collectReach;
        private readonly IDisposable _opened;
        private readonly CrateRimGlow[] _rims;

        /// <param name="frames">A crate's frame by its anchor id: a world anchor or a ruin piece.</param>
        /// <param name="reach">Metres within which a cut-open crate still holding something gives the rest.</param>
        /// <param name="tuning">The crates, the open ones' collect reach and their rim glow's reach.</param>
        public SupplyCaches(OpeningTuning tuning, Func<string, Transform> frames, Transform parent,
            GameplayServices services, SealedContainers containers, float reach)
        {
            if (tuning == null || frames == null || services == null || containers == null)
            {
                throw new ArgumentNullException(nameof(tuning), "The supply crates are missing an input.");
            }

            IReadOnlyList<SupplyCache> caches = tuning.Caches;
            float collectReach = tuning.CollectReach;

            _events = services.Events;
            _stock = services.Materials;
            _rover = services.Rover;
            _save = services.Save;
            _input = services.Input;
            _pickups = services.Pickups;
            _reach = reach;
            _collectReach = collectReach;
            foreach (SupplyCache cache in caches)
            {
                Transform frame = frames(cache.AnchorId) ?? throw new InvalidOperationException(
                    $"Supply crate '{cache.Id}' stands by '{cache.AnchorId}', which is no world anchor or ruin piece.");
                GameObject prefab = services.Visuals.Container(cache.Container) ?? throw new InvalidOperationException(
                    $"Art's container '{cache.Container}' for supply crate '{cache.Id}' is not in GameplayVisuals " +
                    "(run Gameplay/Materials).");
                Transform root = Object.Instantiate(prefab, parent).transform;
                root.name = cache.Id;
                Quaternion heading = Heading(frame);
                Vector3 spot = frame.position + heading * new Vector3(cache.Offset.x, 0f, cache.Offset.z);
                Quaternion level = heading * Quaternion.Euler(0f, cache.Yaw, 0f);
                root.SetPositionAndRotation(SurfaceRules.OnSurface(services.Terrain, spot.x, spot.z),
                    level * prefab.transform.localRotation);
                _levels.Add(level);
                MakeSolid(root);
                SealedContainer container = SealedContainer.Read(cache.Id, ContainerKinds.Of(cache.Container), root,
                    SealedContainer.LidPath, SealedContainer.ItemSocketPath, Always, out string problem)
                    ?? throw new InvalidOperationException(problem);
                containers.Add(container);
                _caches.Add(cache);
                _containers.Add(container);
            }

            _left = new Recipe[_caches.Count];
            _rims = new CrateRimGlow[_caches.Count];
            _heldBack = new Func<bool>[_caches.Count];
            _partLeft = new bool[_caches.Count];
            for (int i = 0; i < _caches.Count; i++)
            {
                if (_caches[i].StartsOpen)
                {
                    containers.StandOpen(_caches[i].Id);
                    _left[i] = _caches[i].Yield;
                    _partLeft[i] = _caches[i].Parts.Count > 0;
                }

                Transform root = _containers[i].Root;
                GameObject rim = Child(root, RimGlowName);
                _rims[i] = rim != null ? new CrateRimGlow(rim, RimLight(rim, root, tuning), tuning) : null;
            }

            _opened = _events.Subscribe<ContainerOpened>(OnOpened);
        }

        public int Count => _caches.Count;

        public bool TryPurposeOf(string sourceId, out string structureId)
        {
            for (int i = 0; i < _caches.Count; i++)
            {
                if (string.Equals(_caches[i].Id, sourceId, StringComparison.Ordinal))
                {
                    structureId = _caches[i].ForStructure;
                    return true;
                }
            }

            structureId = null;
            return false;
        }

        public SupplyCache Get(int index)
        {
            return _caches[index];
        }

        /// <summary>What open crate <paramref name="index"/> still holds.</summary>
        public Recipe Left(int index)
        {
            return _left[index];
        }

        /// <summary>Crate <paramref name="index"/> still holds its story part.</summary>
        public bool PartLeft(int index)
        {
            return _partLeft[index];
        }

        /// <summary>
        /// Crate <paramref name="id"/> keeps what it holds until <paramref name="until"/> says so: the lander's cargo
        /// crate gives nothing while 07 is still scanning the pad's fragments on it (B3 before B4).
        /// </summary>
        public void HoldBack(string id, Func<bool> until)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                throw new ArgumentException($"No supply crate '{id}' (OpeningTuning's caches).", nameof(id));
            }

            _heldBack[index] = until ?? throw new ArgumentNullException(nameof(until));
        }

        /// <summary>
        /// The level frame crate <paramref name="id"/> stands in (y up, z its front), without its prefab's root turn.
        /// </summary>
        public Quaternion LevelOf(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _levels[index] : throw new ArgumentException(
                $"No supply crate '{id}' (OpeningTuning's caches).", nameof(id));
        }

        /// <summary>The crate called <paramref name="id"/>, standing in the world.</summary>
        public Transform RootOf(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _containers[index].Root : throw new ArgumentException(
                $"No supply crate '{id}' (OpeningTuning's caches).", nameof(id));
        }

        public void Step(float deltaTime)
        {
            for (int i = 0; i < _caches.Count; i++)
            {
                bool holds = !IsEmpty(_left[i]) || _partLeft[i];
                if (holds && (_heldBack[i] == null || _heldBack[i]()) && Takes(i))
                {
                    Give(i);
                    holds = !IsEmpty(_left[i]) || _partLeft[i];
                }

                // The rim glows only near: the owner could not find the lander's crate at B3/B4, and loot never
                // glows across the basin.
                _rims[i]?.Step(holds, SurfaceRules.HorizontalDistance(_rover.Position, _containers[i].Root.position),
                    deltaTime);
            }
        }

        /// <summary>
        /// 07 takes what crate <paramref name="index"/> holds now: an open crate offers it within the collect reach and
        /// gives it on a held E (B4: the player is told to take the parts, and does); a cut-open one gives the rest as
        /// soon as 07 is back within reach.
        /// </summary>
        private bool Takes(int index)
        {
            Vector3 at = _containers[index].CutPoint.position;
            if (!_caches[index].StartsOpen)
            {
                return SurfaceRules.HorizontalDistanceSquared(_rover.Position, at) <= _reach * _reach;
            }

            return _pickups.Offer(Time.frameCount, _input.ExcavateHeld, PickupReach.OpenCrates, index, at,
                _rover.Position, _collectReach, CrateContentsNoun);
        }

        public void Capture(OpeningSaveData data)
        {
            int count = 0;
            int parts = 0;
            for (int i = 0; i < _left.Length; i++)
            {
                count += IsEmpty(_left[i]) ? 0 : 1;
                parts += _partLeft[i] ? 1 : 0;
            }

            data.partsLeft = new string[parts];
            parts = 0;
            for (int i = 0; i < _partLeft.Length; i++)
            {
                if (_partLeft[i])
                {
                    data.partsLeft[parts++] = _caches[i].Id;
                }
            }

            data.leftIds = new string[count];
            data.left = new Recipe[count];
            count = 0;
            for (int i = 0; i < _left.Length; i++)
            {
                if (!IsEmpty(_left[i]))
                {
                    data.leftIds[count] = _caches[i].Id;
                    data.left[count++] = _left[i];
                }
            }
        }

        /// <summary>
        /// A loaded game's crates: an open-from-the-start crate holds only what the save says it still holds.
        /// </summary>
        public void Restore(OpeningSaveData data)
        {
            for (int i = 0; i < _caches.Count; i++)
            {
                _left[i] = default;
                _partLeft[i] = false;
            }

            foreach (string id in data.partsLeft ?? Array.Empty<string>())
            {
                int index = IndexOf(id);
                if (index >= 0)
                {
                    _partLeft[index] = _caches[index].Parts.Count > 0;
                }
            }

            string[] ids = data.leftIds ?? Array.Empty<string>();
            Recipe[] left = data.left ?? Array.Empty<Recipe>();
            for (int i = 0; i < ids.Length && i < left.Length; i++)
            {
                int index = IndexOf(ids[i]);
                if (index >= 0)
                {
                    _left[index] = left[i];
                }
            }
        }

        public void Dispose()
        {
            _opened.Dispose();
        }

        private static bool Always()
        {
            return true;
        }

        private static bool IsEmpty(Recipe recipe)
        {
            return recipe.Metal <= 0 && recipe.Wiring <= 0 && recipe.Optics <= 0;
        }

        private int IndexOf(string id)
        {
            for (int i = 0; i < _caches.Count; i++)
            {
                if (string.Equals(_caches[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnOpened(ContainerOpened opened)
        {
            int index = IndexOf(opened.ContainerId);
            if (index < 0)
            {
                return;
            }

            _left[index] = _caches[index].Yield;
            Give(index);
            _save.SaveNow();
        }

        /// <summary>Moves what fits of crate <paramref name="index"/>'s contents into 07's hold.</summary>
        private void Give(int index)
        {
            Vector3 at = _containers[index].CutPoint.position;
            Recipe left = _left[index];
            int metal = Take(SalvageMaterial.Metal, left.Metal, at, index);
            int wiring = Take(SalvageMaterial.Wiring, left.Wiring, at, index);
            int optics = Take(SalvageMaterial.Optics, left.Optics, at, index);
            _left[index] = new Recipe(left.Metal - metal, left.Wiring - wiring, left.Optics - optics);
            if (_partLeft[index])
            {
                GiveParts(index, at);
            }
        }

        /// <summary>
        /// Crate <paramref name="index"/>'s story parts into 07's hold, all together once it has a slot for each
        /// (never half: a part fitted since, like the igniter, would come back).
        /// </summary>
        private void GiveParts(int index, Vector3 at)
        {
            IReadOnlyList<string> parts = _caches[index].Parts;
            CargoHold hold = _stock.Hold;
            int needed = 0;
            for (int p = 0; p < parts.Count; p++)
            {
                needed += hold.HasPart(parts[p]) ? 0 : 1;
            }

            if (hold.SlotCount - hold.UsedSlots < needed)
            {
                return;
            }

            for (int p = 0; p < parts.Count; p++)
            {
                if (!hold.HasPart(parts[p]) && hold.TryAddPart(parts[p]))
                {
                    _events.Publish(new StoryPartCollected(parts[p], at));
                }
            }

            _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, at, Vector3.up, 1f, 0f));
            _partLeft[index] = false;
        }

        /// <summary>
        /// A crate 07 bumps into rather than drives through: a box round each of its shell's meshes, in the mesh's own
        /// space (not its lid, which swings open, nor what lies in it, so what 07 scans or takes from an open crate
        /// stays in plain view of its lens; Art splits the cargo crate's shell so no box bridges over its sockets), on
        /// the Prop layer, unless Art gave it colliders of its own.
        /// </summary>
        private static void MakeSolid(Transform root)
        {
            if (root.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            bool any = false;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || IsLoose(root, filter.transform))
                {
                    continue;
                }

                Bounds mesh = filter.sharedMesh.bounds;
                var box = filter.gameObject.AddComponent<BoxCollider>();
                box.center = mesh.center;
                box.size = mesh.size;
                filter.gameObject.layer = Layers.Prop;
                any = true;
            }

            if (!any)
            {
                throw new InvalidOperationException($"Supply crate '{root.name}' shows no mesh to stand solid on.");
            }

            root.gameObject.layer = Layers.Prop;
        }

        /// <summary>
        /// The small warm light over an open crate's rim, in the rim's own colour, off until <see cref="CrateRimGlow"/>
        /// lights it.
        /// </summary>
        private static Light RimLight(GameObject rim, Transform root, OpeningTuning tuning)
        {
            Renderer shown = rim.GetComponentInChildren<Renderer>(true) ?? throw new InvalidOperationException(
                $"Supply crate '{root.name}' has a rim glow with no mesh.");
            var host = new GameObject("RimLight");
            host.transform.SetParent(root, false);
            host.transform.position = shown.bounds.center + Vector3.up * tuning.CrateRimLightLift;
            var light = host.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = tuning.CrateRimLightRange;
            light.shadows = LightShadows.None;
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        /// <summary>
        /// The crate's lid, what lies in it, what glows on its rim or its side panel folded flat on the ground: none of
        /// it its shell.
        /// </summary>
        private static bool IsLoose(Transform root, Transform node)
        {
            for (; node != null && node != root; node = node.parent)
            {
                string name = node.name;
                if (name == SealedContainer.LidPath || name == SealedContainer.ItemSocketPath ||
                    name == RimGlowName || name == OpenPanelName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The crate's child called <paramref name="name"/>, at any depth, or null (only some crates have it).
        /// </summary>
        private static GameObject Child(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node.gameObject;
                }
            }

            return null;
        }

        private int Take(SalvageMaterial material, int units, Vector3 at, int index)
        {
            int fitted = Math.Min(units, _stock.Room(material));
            if (fitted <= 0)
            {
                return 0;
            }

            _events.Publish(new MaterialSalvaged(material, fitted, at, _caches[index].Id, 0, MaterialSource.Crate));
            _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, at, Vector3.up, 1f, 0f, material));
            _stock.Collect(material, fitted);
            return fitted;
        }

        /// <summary>
        /// The level frame a crate's offset is in: the frame's heading on the ground. A ruin piece keeps Art's FBX
        /// root turn (its forward points down, its up along the ground), so its heading is its up laid flat.
        /// </summary>
        private static Quaternion Heading(Transform frame)
        {
            Vector3 forward = frame.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < LevelShare * LevelShare)
            {
                forward = frame.up;
                forward.y = 0f;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
