using System;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary><see cref="IWeldEnergy"/> read from Gameplay's <see cref="BatteryField"/>.</summary>
    internal sealed class BatteryWeldEnergy : IWeldEnergy
    {
        private readonly BatteryField _battery;

        public BatteryWeldEnergy(BatteryField battery)
        {
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
        }

        public float WeldShare01(StructureDefinition definition)
        {
            return _battery.WeldShare01(definition);
        }
    }
}
