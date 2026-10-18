using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's supply crates (docs/design/opening-v2.md §4, §10; no floating loot): Art's containers stood by
    /// world anchors and Lumen ruin pieces, cut open with the cutter like any sealed container
    /// (<see cref="SealedContainers"/>), or standing open from the start (the lander's, B4: no cutting before the
    /// igniter). As a crate stands open its yield goes into 07's hold, each material with the salvage note; an open
    /// one gives it once 07 is within the beam's reach. What the hold has no room for stays in the crate and comes out
    /// when 07 is back within reach with room; a story part (the spare igniter) comes out with
    /// <see cref="StoryPartCollected"/>. Allocation-free per frame.
    /// </summary>
    public sealed class SupplyCaches : IDisposable
    {
        /// <summary>Art's open crate's children (Crate_M_Open): its soft rim glow and the contents in view.</summary>
        private const string RimGlowName = "RimGlow";
        private const string IgniterName = "Igniter";
        private const string MetalName = "MetalBundle";
        private const string WiringName = "WiringSpool";

        /// <summary>A frame's forward laid flat shorter than this is not a heading (it points up or down).</summary>
        private const float LevelShare = 0.5f;

        private readonly List<SupplyCache> _caches = new List<SupplyCache>();
        private readonly List<SealedContainer> _containers = new List<SealedContainer>();
        private readonly Recipe[] _left;
        private readonly Func<bool>[] _heldBack;
        private readonly bool[] _partLeft;
        private readonly GameObject[] _props;
        private readonly List<Quaternion> _levels = new List<Quaternion>();
        private readonly EventBus _events;
        private readonly MaterialStock _stock;
        private readonly IRoverState _rover;
        private readonly ISaveService _save;
        private readonly float _reach;
        private readonly float _collectReach;
        private readonly IDisposable _opened;
        private readonly float _rimReach;
        private readonly GameObject[] _rims;
        private readonly bool[] _rimOn;
        private readonly GameObject[] _igniters;
        private readonly GameObject[] _metals;
        private readonly GameObject[] _wirings;

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
            _rimReach = tuning.CrateRimReach;

            _events = services.Events;
            _stock = services.Materials;
            _rover = services.Rover;
            _save = services.Save;
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
            _props = new GameObject[_caches.Count];
            _rims = new GameObject[_caches.Count];
            _rimOn = new bool[_caches.Count];
            _igniters = new GameObject[_caches.Count];
            _metals = new GameObject[_caches.Count];
            _wirings = new GameObject[_caches.Count];
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

                if (_caches[i].Prop != null)
                {
                    _props[i] = Object.Instantiate(_caches[i].Prop, _containers[i].ItemSocket, false);
                    _props[i].transform.rotation = _levels[i] * _caches[i].Prop.transform.localRotation;
                }

                Transform root = _containers[i].Root;
                _rims[i] = Child(root, RimGlowName);
                _igniters[i] = Child(root, IgniterName);
                _metals[i] = Child(root, MetalName);
                _wirings[i] = Child(root, WiringName);
                if (_rims[i] != null)
                {
                    _rims[i].SetActive(false);
                }

                ShowContents(i);
            }

            _opened = _events.Subscribe<ContainerOpened>(OnOpened);
        }

        public int Count => _caches.Count;

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

        public void Step()
        {
            for (int i = 0; i < _caches.Count; i++)
            {
                bool holds = !IsEmpty(_left[i]) || _partLeft[i];
                float reach = _caches[i].StartsOpen ? _collectReach : _reach;
                if (holds && (_heldBack[i] == null || _heldBack[i]()) &&
                    SurfaceRules.HorizontalDistanceSquared(_rover.Position, _containers[i].CutPoint.position) <=
                    reach * reach)
                {
                    Give(i);
                    holds = !IsEmpty(_left[i]) || _partLeft[i];
                }

                // The rim glows only near: the owner could not find the lander's crate at B4, and loot never
                // glows across the basin.
                bool rim = holds && _rims[i] != null &&
                           SurfaceRules.HorizontalDistanceSquared(_rover.Position, _containers[i].Root.position) <=
                           _rimReach * _rimReach;
                if (rim != _rimOn[i])
                {
                    _rimOn[i] = rim;
                    _rims[i].SetActive(rim);
                }
            }
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

            for (int i = 0; i < _caches.Count; i++)
            {
                ShowContents(i);
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

            ShowContents(index);
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

        /// <summary>What crate <paramref name="index"/> still holds stays in view; what 07 took is gone.</summary>
        private void ShowContents(int index)
        {
            Show(_props[index], _partLeft[index]);
            Show(_igniters[index], _partLeft[index]);
            Show(_metals[index], _left[index].Metal > 0);
            Show(_wirings[index], _left[index].Wiring > 0);
        }

        private static void Show(GameObject shown, bool visible)
        {
            if (shown != null && shown.activeSelf != visible)
            {
                shown.SetActive(visible);
            }
        }

        /// <summary>
        /// A crate 07 bumps into rather than drives through: a box round what it shows, on the Prop layer, unless Art
        /// gave it colliders of its own.
        /// </summary>
        private static void MakeSolid(Transform root)
        {
            if (root.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Supply crate '{root.name}' shows no mesh to stand solid on.");
            }

            Bounds shown = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                shown.Encapsulate(renderers[i].bounds);
            }

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = root.InverseTransformPoint(shown.center);
            Vector3 size = root.InverseTransformVector(shown.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            root.gameObject.layer = Layers.Prop;
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
