namespace MoonProject.Editor.SceneBuild
{
    /// <summary>
    /// A domain's part of <c>Main.unity</c>. Implement it once per domain editor assembly (e.g.
    /// <c>RoverSceneContributor</c> in MoonProject.Rover.Editor) as a class with a public parameterless constructor;
    /// <see cref="MainSceneBuilder"/> finds it via TypeCache and calls <see cref="Contribute"/> on every build of a
    /// fresh, empty scene. Contributions must be deterministic: same inputs, same scene. Never implement this in a
    /// test assembly (it would be picked up by real builds).
    /// </summary>
    public interface ISceneContributor
    {
        /// <summary>
        /// Lower runs first; systems added earlier are initialised earlier. Bands: World 200, Rover 300, Audio 400,
        /// Gameplay 500, UI 600 (same as builder orders).
        /// </summary>
        int Order { get; }

        /// <summary>Adds this domain's objects and systems. Throw or Debug.LogError to fail the build.</summary>
        void Contribute(SceneBuildContext context);
    }
}
