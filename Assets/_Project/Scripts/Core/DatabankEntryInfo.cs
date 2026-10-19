using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// What 07's databank page shows for one entry (docs/design/databank.md §2), read from the Gameplay catalogue.
    /// Text fields are localization keys; an empty <see cref="HistoryKey"/> means NO LUMEN RECORD.
    /// </summary>
    public sealed class DatabankEntryInfo
    {
        public DatabankEntryInfo(string id, DatabankCategory category, string nameKey, DatabankMaker maker, string year,
            DatabankEra era, string readoutKey, string historyKey, DatabankViewKind view, GameObject model,
            IReadOnlyList<string> hiddenNodes, float framingYaw, Texture print)
        {
            Id = id;
            Category = category;
            NameKey = nameKey;
            Maker = maker;
            Year = year;
            Era = era;
            ReadoutKey = readoutKey;
            HistoryKey = historyKey;
            View = view;
            Model = model;
            HiddenNodes = hiddenNodes;
            FramingYaw = framingYaw;
            Print = print;
        }

        /// <summary><c>db.&lt;name&gt;</c>: saved, never renamed.</summary>
        public string Id { get; }

        public DatabankCategory Category { get; }

        public string NameKey { get; }

        public DatabankMaker Maker { get; }

        /// <summary>The year it was made or placed (<c>1967</c>, or <c>1971 · 1984</c> for two eras).</summary>
        public string Year { get; }

        public DatabankEra Era { get; }

        /// <summary>07's measured line: uppercase, <c>·</c>-separated.</summary>
        public string ReadoutKey { get; }

        /// <summary>The crew's catalogue card, signed; empty means NO LUMEN RECORD.</summary>
        public string HistoryKey { get; }

        public DatabankViewKind View { get; }

        /// <summary>The prefab a <see cref="DatabankViewKind.Model"/> entry turns on the page, or null.</summary>
        public GameObject Model { get; }

        /// <summary>Node names hidden on the page so the model matches the world (the Scout's removed scoop).</summary>
        public IReadOnlyList<string> HiddenNodes { get; }

        /// <summary>Degrees the model is turned to when the page first frames it.</summary>
        public float FramingYaw { get; }

        /// <summary>The scanned sheet a <see cref="DatabankViewKind.Print"/> entry shows, or null.</summary>
        public Texture Print { get; }
    }
}
