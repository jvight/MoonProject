using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's salvaged materials (<see cref="IMaterialStock"/>): salvage adds to it, crafting spends whole recipes, and
    /// every change (a restore from a save included) publishes <see cref="MaterialsChanged"/> with the new totals.
    /// </summary>
    public sealed class MaterialStock : IMaterialStock
    {
        private readonly EventBus _events;

        public MaterialStock(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public int Metal { get; private set; }

        public int Wiring { get; private set; }

        public int Optics { get; private set; }

        public int Total => Metal + Wiring + Optics;

        public int Of(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return Metal;
                case SalvageMaterial.Wiring:
                    return Wiring;
                case SalvageMaterial.Optics:
                    return Optics;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }

        public bool Has(Recipe recipe)
        {
            return Metal >= recipe.Metal && Wiring >= recipe.Wiring && Optics >= recipe.Optics;
        }

        public void Add(SalvageMaterial material, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Salvage adds a positive amount.");
            }

            switch (material)
            {
                case SalvageMaterial.Metal:
                    Metal = checked(Metal + amount);
                    break;
                case SalvageMaterial.Wiring:
                    Wiring = checked(Wiring + amount);
                    break;
                case SalvageMaterial.Optics:
                    Optics = checked(Optics + amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }

            Announce();
        }

        /// <summary>Spends <paramref name="recipe"/> if the stock covers all of it; else nothing changes.</summary>
        public bool TrySpend(Recipe recipe)
        {
            if (recipe.IsFree)
            {
                throw new ArgumentException("A recipe costs something.", nameof(recipe));
            }

            if (!Has(recipe))
            {
                return false;
            }

            Metal -= recipe.Metal;
            Wiring -= recipe.Wiring;
            Optics -= recipe.Optics;
            Announce();
            return true;
        }

        public MaterialsSaveData Capture()
        {
            return new MaterialsSaveData { metal = Metal, wiring = Wiring, optics = Optics };
        }

        public void Restore(MaterialsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.metal < 0 || data.wiring < 0 || data.optics < 0)
            {
                throw new FormatException(
                    $"Saved materials ({data.metal}, {data.wiring}, {data.optics}) are negative.");
            }

            Metal = data.metal;
            Wiring = data.wiring;
            Optics = data.optics;
            Announce();
        }

        private void Announce()
        {
            _events.Publish(new MaterialsChanged(Metal, Wiring, Optics));
        }
    }
}
