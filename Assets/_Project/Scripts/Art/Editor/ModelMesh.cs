using System;

namespace MoonProject.Art.Editor
{
    /// <summary>A named mesh in a model recipe; several <see cref="ModelNode"/>s may share one (e.g. wheels).</summary>
    public sealed class ModelMesh
    {
        public ModelMesh(string name, LowPolyMeshBuilder geometry)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A model mesh needs a name (it names the asset file).", nameof(name));
            }

            Name = name;
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        }

        /// <summary>Asset and mesh name, unique within its output folder.</summary>
        public string Name { get; }

        public LowPolyMeshBuilder Geometry { get; }
    }
}
