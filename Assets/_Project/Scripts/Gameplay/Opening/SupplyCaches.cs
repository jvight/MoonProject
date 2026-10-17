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
    /// The opening's sealed supply crates (docs/design/opening-v2.md §4; no floating loot): Art's containers stood by
    /// world anchors and Lumen ruin pieces, cut open with the cutter like any sealed container
    /// (<see cref="SealedContainers"/>). As a crate stands open its yield goes into 07's hold, each material with the
    /// salvage note; what the hold has no room for stays in the crate and comes out when 07 is back within the
    /// beam's reach with room. Allocation-free per frame.
    /// </summary>
    public sealed class SupplyCaches : IDisposable
    {
        private readonly List<SupplyCache> _caches = new List<SupplyCache>();
        private readonly List<SealedContainer> _containers = new List<SealedContainer>();
        private readonly Recipe[] _left;
        private readonly EventBus _events;
        private readonly MaterialStock _stock;
        private readonly IRoverState _rover;
        private readonly ISaveService _save;
        private readonly float _reach;
        private readonly IDisposable _opened;

        /// <param name="frames">A crate's frame by its anchor id: a world anchor or a ruin piece.</param>
        /// <param name="reach">Metres within which an open crate still holding something gives the rest.</param>
        public SupplyCaches(IReadOnlyList<SupplyCache> caches, Func<string, Transform> frames, Transform parent,
            GameplayServices services, SealedContainers containers, float reach)
        {
            if (caches == null || frames == null || services == null || containers == null)
            {
                throw new ArgumentNullException(nameof(caches), "The supply crates are missing an input.");
            }

            _events = services.Events;
            _stock = services.Materials;
            _rover = services.Rover;
            _save = services.Save;
            _reach = reach;
            foreach (SupplyCache cache in caches)
            {
                Transform frame = frames(cache.AnchorId) ?? throw new InvalidOperationException(
                    $"Supply crate '{cache.Id}' stands by '{cache.AnchorId}', which is no world anchor or ruin piece.");
                GameObject prefab = services.Visuals.Container(cache.Container) ?? throw new InvalidOperationException(
                    $"Art's container '{cache.Container}' for supply crate '{cache.Id}' is not in GameplayVisuals " +
                    "(run Gameplay/Materials).");
                Transform root = Object.Instantiate(prefab, parent).transform;
                root.name = cache.Id;
                Vector3 spot = frame.TransformPoint(new Vector3(cache.Offset.x, 0f, cache.Offset.z));
                root.SetPositionAndRotation(SurfaceRules.OnSurface(services.Terrain, spot.x, spot.z),
                    Quaternion.Euler(0f, frame.eulerAngles.y + cache.Yaw, 0f));
                SealedContainer container = SealedContainer.Read(cache.Id, ContainerKinds.Of(cache.Container), root,
                    SealedContainer.LidPath, SealedContainer.ItemSocketPath, Always, out string problem)
                    ?? throw new InvalidOperationException(problem);
                containers.Add(container);
                _caches.Add(cache);
                _containers.Add(container);
            }

            _left = new Recipe[_caches.Count];
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

        public void Step()
        {
            float reachSq = _reach * _reach;
            for (int i = 0; i < _caches.Count; i++)
            {
                if (!IsEmpty(_left[i]) &&
                    SurfaceRules.HorizontalDistanceSquared(_rover.Position, _containers[i].CutPoint.position) <= reachSq)
                {
                    Give(i);
                }
            }
        }

        public void Capture(OpeningSaveData data)
        {
            int count = 0;
            for (int i = 0; i < _left.Length; i++)
            {
                count += IsEmpty(_left[i]) ? 0 : 1;
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

        public void Restore(OpeningSaveData data)
        {
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
        }

        private int Take(SalvageMaterial material, int units, Vector3 at, int index)
        {
            int fitted = Math.Min(units, _stock.Hold.FreeUnits(material));
            if (fitted <= 0)
            {
                return 0;
            }

            _events.Publish(new MaterialSalvaged(material, fitted, at, _caches[index].Id, 0, MaterialSource.Crate));
            _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, at, Vector3.up, 1f, 0f, material));
            _stock.Collect(material, fitted);
            return fitted;
        }
    }
}
