using System;
using System.Collections.Generic;
using MoonProject.Core.Save;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// <see cref="ISaveService"/> stand-in kept in memory: sections register and can be captured
    /// (<see cref="Capture"/>); a test may seed a section's saved JSON before 07 is spawned (<see cref="Seed"/>).
    /// </summary>
    public sealed class TestSave : ISaveService
    {
        private readonly Dictionary<string, ISaveSection> _sections = new Dictionary<string, ISaveSection>();
        private readonly Dictionary<string, string> _seeded = new Dictionary<string, string>();

        public string FilePath => "memory";

        public bool IsLoaded => true;

        public SaveLoadResult LoadResult => SaveLoadResult.NoSave;

        public event Action Saved
        {
            add { }
            remove { }
        }

        /// <summary>A loaded game's JSON for <paramref name="key"/>, restored when the section registers.</summary>
        public void Seed(string key, string json)
        {
            _seeded[key] = json;
        }

        /// <summary>The registered section's current state as JSON.</summary>
        public string Capture(string key)
        {
            return _sections[key].Capture();
        }

        public IDisposable Register(ISaveSection section)
        {
            _sections.Add(section.Key, section);
            if (_seeded.TryGetValue(section.Key, out string json))
            {
                section.Restore(json);
            }

            return new Unregister(() => _sections.Remove(section.Key));
        }

        public bool SaveNow()
        {
            return true;
        }

        public string PutAway()
        {
            throw new NotSupportedException("Rover's tests never start a new game.");
        }

        private sealed class Unregister : IDisposable
        {
            private readonly Action _action;

            public Unregister(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}
