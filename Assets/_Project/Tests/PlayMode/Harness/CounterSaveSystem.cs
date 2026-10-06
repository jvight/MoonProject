using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;

namespace MoonProject.Testing
{
    /// <summary>
    /// <see cref="IGameSystem"/> test double that registers a one-number save section the way domain systems do:
    /// in Initialize, disposing the registration in OnDestroy.
    /// </summary>
    public sealed class CounterSaveSystem : MonoBehaviour, IGameSystem
    {
        public const string SectionKey = "testing.counter";

        private IDisposable _registration;

        public int Count { get; set; }

        /// <summary>The count when Initialize ran (before the bootstrap loaded the save).</summary>
        public int CountAtInitialize { get; private set; }

        public void Initialize(GameContext context)
        {
            CountAtInitialize = Count;
            _registration = context.Get<ISaveService>().Register(new SaveSection<CounterSaveData>(
                SectionKey, 1, () => new CounterSaveData { count = Count }, data => Count = data.count));
        }

        private void OnDestroy()
        {
            _registration?.Dispose();
        }
    }
}
