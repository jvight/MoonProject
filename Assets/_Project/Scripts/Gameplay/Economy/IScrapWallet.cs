namespace MoonProject.Gameplay
{
    /// <summary>
    /// Read-only view of the scrap balance, registered in the GameContext for UI. Changes are also published as
    /// <c>CurrencyChanged</c>.
    /// </summary>
    public interface IScrapWallet
    {
        int Balance { get; }

        bool CanAfford(int cost);
    }
}
