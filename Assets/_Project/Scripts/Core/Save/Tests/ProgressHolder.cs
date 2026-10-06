using System;
using System.Collections.Generic;

namespace MoonProject.Core.Save.Tests
{
    /// <summary>A fake game system owning <see cref="ProgressData"/> and exposing it as a save section.</summary>
    public sealed class ProgressHolder
    {
        public const string Key = "test.progress";

        public ProgressData Data { get; set; } = new ProgressData();

        public int RestoreCount { get; private set; }

        public List<int> MigratedFrom { get; } = new List<int>();

        public bool ThrowOnCapture { get; set; }

        public SaveSection<ProgressData> Section(int version, Func<string, int, string> migrate = null)
        {
            return new SaveSection<ProgressData>(Key, version, Capture, Restore, migrate == null ? null : Track(migrate));
        }

        private ProgressData Capture()
        {
            if (ThrowOnCapture)
            {
                throw new InvalidOperationException("capture failed on purpose");
            }

            return new ProgressData { scrap = Data.scrap, name = Data.name, relics = Data.relics };
        }

        private void Restore(ProgressData data)
        {
            Data = data;
            RestoreCount++;
        }

        private Func<string, int, string> Track(Func<string, int, string> migrate)
        {
            return (json, from) =>
            {
                MigratedFrom.Add(from);
                return migrate(json, from);
            };
        }
    }
}
