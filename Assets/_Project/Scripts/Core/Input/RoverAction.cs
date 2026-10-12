namespace MoonProject.Core.Input
{
    /// <summary>
    /// Rover actions the UI names in context prompts (see <see cref="InputReader.GetBindingLabel"/>). The numbers are
    /// serialized in the UI's prompt table, so they never shift: 0 was the sonar ping, cut when 07 learned to listen.
    /// </summary>
    public enum RoverAction
    {
        Excavate = 1,
        Tether = 2,
        Winch = 3,
        Jump = 4,
    }
}
