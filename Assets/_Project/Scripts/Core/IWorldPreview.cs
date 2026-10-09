namespace MoonProject.Core
{
    /// <summary>
    /// The World system as other domains' editor tooling sees it in edit mode (implemented by the World domain's
    /// scene system), so an edit-mode preview can place content on the world's anchors without referencing the World
    /// assembly.
    /// </summary>
    public interface IWorldPreview
    {
        /// <summary>True once the world (terrain, anchors) has been generated for this scene.</summary>
        bool HasGeneratedWorld { get; }

        /// <summary>The world's anchors; meaningful only while <see cref="HasGeneratedWorld"/> is true.</summary>
        IWorldAnchors Anchors { get; }
    }
}
