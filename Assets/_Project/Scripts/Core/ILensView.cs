namespace MoonProject.Core
{
    /// <summary>
    /// The picture through 07's own eye (docs/design/cutscenes.md §1.8), registered in the <see cref="GameContext"/>
    /// by the World domain and driven by Rover while the camera sits in the lens. Allocation-free: safe every frame.
    /// </summary>
    public interface ILensView
    {
        /// <summary>
        /// Builds what the view needs (its overlay) ahead of its first frame, where nobody
        /// sees the cost: on the title, or in the black of a new game.
        /// </summary>
        void Warm();

        /// <summary>Shows the lens' state this frame; the view stays on until <see cref="Release"/>.</summary>
        void Apply(in LensViewState state);

        /// <summary>The camera has left the lens: the picture is the world's own again.</summary>
        void Release();
    }
}
