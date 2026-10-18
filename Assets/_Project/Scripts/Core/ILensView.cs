namespace MoonProject.Core
{
    /// <summary>
    /// The picture darkened by 07's lid (docs/design/cutscenes.md §1.8), registered in the <see cref="GameContext"/>
    /// by the World domain and driven by Rover for the opening's fades: the title's fade to black, the picture opening
    /// on 07's face and the handover's dip. Allocation-free: safe every frame.
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
