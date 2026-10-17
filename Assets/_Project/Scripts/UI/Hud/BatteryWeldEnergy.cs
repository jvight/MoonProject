using System;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary><see cref="IWeldEnergy"/> read from Gameplay's <see cref="IBuildEnergy"/>.</summary>
    internal sealed class BatteryWeldEnergy : IWeldEnergy
    {
        private readonly IBuildEnergy _energy;

        public BatteryWeldEnergy(IBuildEnergy energy)
        {
            _energy = energy ?? throw new ArgumentNullException(nameof(energy));
        }

        public float WeldShare01(StructureDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return _energy.WeldShare01(definition.Id);
        }
    }
}
