using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The words of 07's databank page (docs/design/databank.md §4, text/databank-entries.md): the category names, the
    /// MAKER · YEAR · ERA row, the readout, the history card or NO LUMEN RECORD, a set's scan count and blueprint line,
    /// and the unlock line on 07's face. Strings are built when the shown card changes, never per frame.
    /// </summary>
    internal sealed class DatabankText
    {
        public const string TitleKey = "db.title";
        public const string NoRecordKey = "db.no_record";
        public const string ScanProgressKey = "db.scan_progress";
        public const string BlueprintKey = "db.blueprint";
        public const string ToastKey = "db.toast.added";
        public const string ColourKey = "db.colour";
        public const string MakerFieldKey = "db.field.maker";
        public const string YearFieldKey = "db.field.year";
        public const string EraFieldKey = "db.field.era";
        public const string EmptyMark = "—";

        private const string FieldGap = "   ";
        private const string Separator = " · ";

        private readonly ILocalization _localization;

        public DatabankText(ILocalization localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        /// <summary>"db.cat.machines": a category's name key.</summary>
        public static string CategoryKey(DatabankCategory category)
        {
            switch (category)
            {
                case DatabankCategory.Machines:
                    return "db.cat.machines";
                case DatabankCategory.Structures:
                    return "db.cat.structures";
                case DatabankCategory.Places:
                    return "db.cat.places";
                case DatabankCategory.Phenomena:
                    return "db.cat.phenomena";
                default:
                    return "db.cat.archive";
            }
        }

        /// <summary>"db.era.p": an era's name key; null for None.</summary>
        public static string EraKey(DatabankEra era)
        {
            switch (era)
            {
                case DatabankEra.Probes:
                    return "db.era.p";
                case DatabankEra.Sorties:
                    return "db.era.s";
                case DatabankEra.BuildOut:
                    return "db.era.b";
                case DatabankEra.Lumen:
                    return "db.era.l";
                case DatabankEra.Meridian:
                    return "db.era.m";
                case DatabankEra.Now:
                    return "db.era.n";
                default:
                    return null;
            }
        }

        /// <summary>The era strip's one-letter mark (P S B L M N).</summary>
        public static string EraMark(DatabankEra era)
        {
            switch (era)
            {
                case DatabankEra.Probes:
                    return "P";
                case DatabankEra.Sorties:
                    return "S";
                case DatabankEra.BuildOut:
                    return "B";
                case DatabankEra.Lumen:
                    return "L";
                case DatabankEra.Meridian:
                    return "M";
                case DatabankEra.Now:
                    return "N";
                default:
                    return EmptyMark;
            }
        }

        /// <summary>"db.maker.vis": a maker's name key; null for None.</summary>
        public static string MakerKey(DatabankMaker maker)
        {
            switch (maker)
            {
                case DatabankMaker.ILC:
                    return "db.maker.ilc";
                case DatabankMaker.KesslerArai:
                    return "db.maker.kessler_arai";
                case DatabankMaker.VIS:
                    return "db.maker.vis";
                case DatabankMaker.Tycho:
                    return "db.maker.tycho";
                default:
                    return null;
            }
        }

        public string Category(DatabankCategory category) => _localization.Get(CategoryKey(category));

        public string Name(DatabankEntryInfo entry) => _localization.Get(entry.NameKey);

        /// <summary><c>MAKER · VIS   YEAR · 1967   ERA · PROBES</c>; an empty field shows a dash.</summary>
        public string FieldRow(DatabankEntryInfo entry)
        {
            string maker = MakerKey(entry.Maker);
            string era = EraKey(entry.Era);
            string year = string.IsNullOrEmpty(entry.Year) ? EmptyMark : entry.Year;
            string makerName = maker != null ? _localization.Get(maker) : EmptyMark;
            string eraName = era != null ? _localization.Get(era) : EmptyMark;
            return _localization.Get(MakerFieldKey) + Separator + makerName +
                   FieldGap + _localization.Get(YearFieldKey) + Separator + year +
                   FieldGap + _localization.Get(EraFieldKey) + Separator + eraName;
        }

        public string Readout(DatabankEntryInfo entry) => _localization.Get(entry.ReadoutKey);

        /// <summary>
        /// The crew's card, or NO LUMEN RECORD when the catalogue has none; nothing until a set is whole.
        /// </summary>
        public string History(DatabankEntryInfo entry, DatabankProgress progress)
        {
            if (!progress.IsComplete)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(entry.HistoryKey)
                ? _localization.Get(NoRecordKey)
                : _localization.Get(entry.HistoryKey);
        }

        /// <summary>
        /// The set line under the readout: <c>SCAN 1/3</c> while pieces are missing, <c>BLUEPRINT · SOLAR ARRAY</c>
        /// once a set of two or more is whole; empty for an entry that unlocked whole.
        /// </summary>
        public string SetLine(DatabankEntryInfo entry, DatabankProgress progress)
        {
            if (progress.Count < 2)
            {
                return string.Empty;
            }

            return progress.IsComplete
                ? string.Format(_localization.Get(BlueprintKey), Name(entry).ToUpperInvariant())
                : string.Format(_localization.Get(ScanProgressKey), progress.Found, progress.Count);
        }

        /// <summary><c>Added to databank: Scout 3</c>.</summary>
        public string Toast(DatabankEntryInfo entry) => string.Format(_localization.Get(ToastKey), Name(entry));
    }
}
