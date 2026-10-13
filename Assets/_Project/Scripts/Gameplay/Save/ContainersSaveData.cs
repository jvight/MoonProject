using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The sealed containers that stand open (section "gameplay.containers", docs/design/0.5.5-owner-feedback.md §2),
    /// by id.
    /// </summary>
    [Serializable]
    public sealed class ContainersSaveData
    {
        public string[] opened = Array.Empty<string>();
    }
}
