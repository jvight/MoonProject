using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// One GameObject of a model recipe: name, local pose and an optional mesh. A tree of nodes is what a builder
    /// writes as a prefab (meshes only: no colliders, no scripts).
    /// </summary>
    public sealed class ModelNode
    {
        private readonly List<ModelNode> _children = new List<ModelNode>();

        public ModelNode(string name, Vector3 localPosition, Quaternion localRotation, ModelMesh mesh = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A model node needs a name.", nameof(name));
            }

            Name = name;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            Mesh = mesh;
        }

        public ModelNode(string name, Vector3 localPosition, ModelMesh mesh = null)
            : this(name, localPosition, Quaternion.identity, mesh)
        {
        }

        public string Name { get; }

        public Vector3 LocalPosition { get; }

        public Quaternion LocalRotation { get; }

        /// <summary>Mesh rendered by this node with the shared palette material, or null for an empty pivot.</summary>
        public ModelMesh Mesh { get; }

        public IReadOnlyList<ModelNode> Children => _children;

        /// <summary>Adds <paramref name="child"/> and returns it (for nesting pivots inline).</summary>
        public ModelNode Add(ModelNode child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].Name == child.Name)
                {
                    throw new ArgumentException($"'{Name}' already has a child named '{child.Name}'.", nameof(child));
                }
            }

            _children.Add(child);
            return child;
        }

        /// <summary>Depth-first lookup by name (this node included), or null.</summary>
        public ModelNode GetDescendant(string name)
        {
            if (Name == name)
            {
                return this;
            }

            for (int i = 0; i < _children.Count; i++)
            {
                ModelNode hit = _children[i].GetDescendant(name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>Matrix from this node's space to its parent's space.</summary>
        public Matrix4x4 LocalMatrix => Matrix4x4.Translate(LocalPosition) * Matrix4x4.Rotate(LocalRotation);
    }
}
