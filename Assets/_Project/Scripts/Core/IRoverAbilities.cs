namespace MoonProject.Core
{
    /// <summary>
    /// Which workshop abilities 07 has, registered in the <see cref="GameContext"/> by the Rover domain. Gameplay grants
    /// them when an upgrade is bought or a save is loaded; Rover, UI and Audio read them.
    /// </summary>
    public interface IRoverAbilities
    {
        bool Has(RoverAbility ability);

        /// <summary>Unlocks <paramref name="ability"/> (idempotent). Publishes nothing; the purchase already did.</summary>
        void Grant(RoverAbility ability);
    }
}
