namespace MoonProject.Core
{
    /// <summary>
    /// Which workshop abilities 07 has, registered in the <see cref="GameContext"/> by the Rover domain. Gameplay grants
    /// them as a part is fitted or a save is loaded, and revokes them as a part is unequipped; Rover, UI and Audio read
    /// them.
    /// </summary>
    public interface IRoverAbilities
    {
        bool Has(RoverAbility ability);

        /// <summary>Unlocks <paramref name="ability"/> (idempotent). Publishes nothing; the purchase already did.</summary>
        void Grant(RoverAbility ability);

        /// <summary>
        /// Pauses <paramref name="ability"/> (idempotent): its part was unequipped (0.5.5 §3.5), and refitting grants it
        /// again. Publishes nothing; the unequip already did.
        /// </summary>
        void Revoke(RoverAbility ability);
    }
}
