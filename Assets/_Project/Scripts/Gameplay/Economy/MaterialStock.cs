using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every material the player has (<see cref="IMaterialStock"/>): 07's <see cref="Hold"/> and home's
    /// <see cref="Home"/> together (docs/features/M3-25 §3.3). Salvage goes into the hold, the dock moves it home,
    /// recipes read both and spend home first, then 07. Every change of the totals (a restore included) publishes
    /// <see cref="MaterialsChanged"/>.
    /// </summary>
    public sealed class MaterialStock : IMaterialStock
    {
        /// <summary>The recipe materials, in the order recipes spend them.</summary>
        private static readonly SalvageMaterial[] RecipeMaterials =
        {
            SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
        };

        private readonly EventBus _events;

        public MaterialStock(EventBus events, CargoHold hold, HomeStorage home)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Hold = hold ?? throw new ArgumentNullException(nameof(hold));
            Home = home ?? throw new ArgumentNullException(nameof(home));
        }

        public CargoHold Hold { get; }

        public HomeStorage Home { get; }

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
            return Hold.Units(material) + Home.Units(material);
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
        /// A new game's spares (0.5.5 D4): <paramref name="spares"/> go into 07's hold. A loaded save lays its own hold
        /// in over them (<see cref="Restore"/> empties the hold first), so only a new save keeps them.
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

        /// <summary>
        /// Moves up to <paramref name="units"/> of <paramref name="material"/> from 07's hold onto home's shelves (the
        /// dock's unload, M3-25 §3.2); returns how many moved. The totals do not change.
        /// </summary>
        public int MoveHome(SalvageMaterial material, int units)
        {
            int moving = Math.Min(Math.Min(units, Hold.Units(material)), Home.FreeUnits(material));
            if (moving <= 0)
            {
                return 0;
            }

            Hold.Remove(material, moving);
            Home.Add(material, moving);
            return moving;
        }

        /// <summary>
        /// Moves up to <paramref name="units"/> of <paramref name="material"/> from home's shelves into 07's hold (the
        /// docked transfer view, 0.5.5 §3); returns how many moved. The totals do not change.
        /// </summary>
        public int MoveAboard(SalvageMaterial material, int units)
        {
            int moving = Math.Min(Math.Min(units, Home.Units(material)), Hold.FreeUnits(material));
            if (moving <= 0)
            {
                return 0;
            }

            Home.Remove(material, moving);
            Hold.Add(material, moving);
            return moving;
        }

        /// <summary>
        /// Puts <paramref name="units"/> straight onto home's shelves (Tilly's fetch, M3-25 T3); returns what did not
        /// fit, which stays where it was.
        /// </summary>
        public int Deliver(SalvageMaterial material, int units)
        {
            int fitted = Home.Add(material, units);
            if (fitted > 0)
            {
                Announce();
            }

            return units - fitted;
        }

        /// <summary>
        /// Spends <paramref name="recipe"/> if home and 07 together cover it, home first; else nothing changes.
        /// </summary>
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
                int need = recipe.Of(material);
                need -= Home.Remove(material, need);
                Hold.Remove(material, need);
            }

            Announce();
            return true;
        }

        public MaterialsSaveData Capture()
        {
            return new MaterialsSaveData
            {
                hold = Hold.Capture(),
                holdSlots = Hold.SlotCount,
                home = Home.CaptureShelves(),
                overflow = Home.CaptureOverflow(),
                shelvesBuilt = Home.BuiltCount,
            };
        }

        /// <summary>
        /// Lays a save in. The hold keeps at least its saved slots (the kits restore after), and anything that no
        /// longer fits it goes home; home's own overflow waits in crates. Nothing is dropped.
        /// </summary>
        public void Restore(MaterialsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int[] hold = data.hold ?? Array.Empty<int>();
            var home = new int[SalvageMaterials.Count];
            Copy(data.home, home, "home");
            Home.Restore(data.shelvesBuilt, home, data.overflow ?? Array.Empty<int>());
            Hold.Clear();
            Hold.SetSlotCount(Math.Max(Hold.SlotCount, data.holdSlots));
            for (int m = 0; m < hold.Length && m < SalvageMaterials.Count; m++)
            {
                if (hold[m] < 0)
                {
                    throw new FormatException($"07's saved {(SalvageMaterial)m} is negative ({hold[m]}).");
                }

                var material = (SalvageMaterial)m;
                int left = hold[m] - (hold[m] > 0 ? Hold.Add(material, hold[m]) : 0);
                int shelved = left > 0 ? Home.Add(material, left) : 0;
                if (left > shelved)
                {
                    Home.AddOverflow(material, left - shelved);
                }
            }

            Announce();
        }

        /// <summary>
        /// Frees one hold slot for a restored story part from a 0.5.5 save whose hold was full of materials: the last
        /// material stack moves to home's shelves (or home's overflow crates), so nothing is lost.
        /// </summary>
        internal void MakeRoomForPart()
        {
            while (!Hold.HasFreeSlot && Hold.MaterialSlots > 0)
            {
                MaterialSlot slot = Hold.GetSlot(Hold.MaterialSlots - 1);
                int taken = Hold.Remove(slot.Material, slot.Units);
                int shelved = taken > 0 ? Home.Add(slot.Material, taken) : 0;
                if (taken > shelved)
                {
                    Home.AddOverflow(slot.Material, taken - shelved);
                }
            }
        }

        private static void Copy(int[] from, int[] to, string what)
        {
            if (from == null)
            {
                return;
            }

            for (int m = 0; m < from.Length && m < to.Length; m++)
            {
                if (from[m] < 0)
                {
                    throw new FormatException($"The saved {what} {(SalvageMaterial)m} is negative ({from[m]}).");
                }

                to[m] = from[m];
            }
        }

        private void Announce()
        {
            _events.Publish(new MaterialsChanged(Metal, Wiring, Optics));
        }
    }
}
