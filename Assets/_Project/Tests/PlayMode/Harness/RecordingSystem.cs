using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Testing
{
    /// <summary>
    /// <see cref="IGameSystem"/> test double: remembers the context it was initialised with and appends its
    /// <see cref="Label"/> to a shared <see cref="Journal"/>, so tests can assert initialisation order.
    /// </summary>
    public sealed class RecordingSystem : MonoBehaviour, IGameSystem
    {
        /// <summary>Name written to <see cref="Journal"/> on initialisation.</summary>
        public string Label { get; set; }

        /// <summary>Shared list several systems append to, in initialisation order.</summary>
        public IList<string> Journal { get; set; }

        public GameContext Context { get; private set; }

        public int InitializeCount { get; private set; }

        public void Initialize(GameContext context)
        {
            Context = context;
            InitializeCount++;
            Journal?.Add(Label);
        }
    }
}
