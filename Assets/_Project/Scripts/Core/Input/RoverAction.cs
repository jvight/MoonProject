namespace MoonProject.Core.Input
{
    /// <summary>
    /// Rover actions the UI names in context prompts (see <see cref="InputReader.GetBindingLabel"/>).
    /// </summary>
    public enum RoverAction
    {
        Ping = 0,
        Excavate = 1,
        Tether = 2,
        Winch = 3,
    }
}
