using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every material the player has (<see cref="IMaterialStock"/>): 07's <see cref="Hold"/> (opening-v2 §5: inventory
    /// is 07's hold only). Salvage goes into the hold and recipes spend from it. Every change of the totals (a restore
    /// included) publishes <see cref="MaterialsChanged"/>.
    /// </summary>
    public sealed class MaterialStock : IMaterialStock
    {
        /// <summary>The recipe materials, in the order recipes spend them.</summary>
        private static readonly SalvageMaterial[] RecipeMaterials =
        {
            SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
        };

        private readonly EventBus _events;
        private readonly int[] _incoming = new int[SalvageMaterials.Count];

        public MaterialStock(EventBus events, CargoHold hold)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Hold = hold ?? throw new ArgumentNullException(nameof(hold));
        }

        public CargoHold Hold { get; }

        public int Metal => Of(SalvageMaterial.Metal);

        public int Wiring => Of(SalvageMaterial.Wiring);

        public int Optics => Of(SalvageMaterial.Optics);

        public int Total
        {
            get
            {
                int total = 0;
                for (int m = 0; m < SalvageMaterials.Count; m++)
                {
                    total += Of((SalvageMaterial)m);
                }

                return total;
            }
        }

        public int Of(SalvageMaterial material)
        {
            return Hold.Units(material);
        }

        /// <summary>
        /// Units of <paramref name=material/> the hold can still take, less what is already on its way in (a cut
        /// piece flying to 07): every source checks this, so nothing that flies in ever finds the hold full.
        /// </summary>
        public int Room(SalvageMaterial material)
        {
            return Math.Max(0, Hold.FreeUnits(material) - _incoming[(int)material]);
        }

        /// <summary>
        /// <paramref name="units"/> of <paramref name="material"/> set off for the hold (see <see cref="Room"/>).
        /// </summary>
        public void Expect(SalvageMaterial material, int units)
        {
            _incoming[(int)material] += units;
        }

        /// <summary>What was expected arrives: it is collected and no longer reserved.</summary>
        public void Receive(SalvageMaterial material, int units)
        {
            _incoming[(int)material] = Math.Max(0, _incoming[(int)material] - units);
            Collect(material, units);
        }

        public bool Has(Recipe recipe)
        {
            return Metal >= recipe.Metal && Wiring >= recipe.Wiring && Optics >= recipe.Optics;
        }

        /// <summary>
        /// A bundle reaches 07: what fits goes into the hold. Returns the units that did not fit (they wait in the
        /// world, M3-25 §2.3).
        /// </summary>
        public int Collect(SalvageMaterial material, int units)
        {
            if (units <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(units), units, "A bundle holds a positive amount.");
            }

            int fitted = Hold.Add(material, units);
            if (fitted > 0)
            {
                Announce();
            }

            return units - fitted;
        }

        /// <summary>
        /// Puts <paramref name="spares"/> straight into 07's hold (a session's stock laid in by hand). A loaded save
        /// lays its own hold in over them (<see cref="Restore"/> empties the hold first).
        /// </summary>
        public void Stow(Recipe spares)
        {
            foreach (SalvageMaterial material in RecipeMaterials)
            {
                int units = spares.Of(material);
                if (units > 0 && Hold.Add(material, units) != units)
                {
                    throw new InvalidOperationException($"07's hold has no room for its spare {material} ({units}).");
                }
            }

            Announce();
        }

        /// <summary>Spends <paramref name="recipe"/> if 07's hold covers it; else nothing changes.</summary>
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

            for (int i = 0; i < RecipeMaterials.Length; i++)
            {
                SalvageMaterial material = RecipeMaterials[i];
                Hold.Remove(material, recipe.Of(material));
            }

            Announce();
            return true;
        }

        public MaterialsSaveData Capture()
        {
            return new MaterialsSaveData { hold = Hold.Capture(), holdSlots = Hold.SlotCount };
        }

        /// <summary>Lays a save in: the hold keeps at least its saved slots.</summary>
        public void Restore(MaterialsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int[] hold = data.hold ?? Array.Empty<int>();
            Hold.Clear();
            Hold.SetSlotCount(Math.Max(Hold.SlotCount, data.holdSlots));
            for (int m = 0; m < hold.Length && m < SalvageMaterials.Count; m++)
            {
                if (hold[m] < 0)
                {
                    throw new FormatException($"07's saved {(SalvageMaterial)m} is negative ({hold[m]}).");
                }

                if (hold[m] > 0)
                {
                    Hold.Add((SalvageMaterial)m, hold[m]);
                }
            }

            Announce();
        }

        private void Announce()
        {
            _events.Publish(new MaterialsChanged(Metal, Wiring, Optics));
        }
    }
}
