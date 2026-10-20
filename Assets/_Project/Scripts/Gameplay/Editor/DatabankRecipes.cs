using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Core;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The opening basin's 19 databank entries (docs/design/databank.md, docs/design/text/databank-entries.md): ids,
    /// categories, makers, years, eras, text keys, the Art model each turns on the page, and the rule that unlocks it.
    /// The content builder writes them into the <see cref="DatabankCatalogue"/>. The four maker cards show Art's
    /// prints; the other print entries leave the sheet empty until theirs exist.
    /// </summary>
    internal static class DatabankRecipes
    {
        private const string Models = ArtPaths.R1ModelsFolder;
        private const string LanderCargo = ScanTuning.LanderCargoId;
        private const string LumenSets = "FallenSolarArray,HabitatModules,RadioTower";
        private const string Prints = "Assets/_Project/Art/R1/Brands/print_";

        /// <summary>
        /// Degrees an SSR rover turns on the page so its CRT face looks out over its front-left shoulder (its art faces
        /// away from the stage camera at 0).
        /// </summary>
        private const float RoverFramingYaw = 145f;

        /// <summary>Art's prefab for the Scout, whose camera and scoop the 1971 crew took home.</summary>
        private static readonly string[] ScoutRemovedParts = { "TvCamera", "ScoopArm" };

        public static DatabankEntry[] Create()
        {
            return new[]
            {
                Machine("unit07", DatabankMaker.KesslerArai, "1983", DatabankEra.Lumen, "db.unit07.readout",
                    "notes.unit07", Models + "/Rover/RoverModel.prefab", DatabankUnlock.Seeded, RoverFramingYaw),
                Machine("elder06", DatabankMaker.KesslerArai, "1978", DatabankEra.BuildOut, "elder.06.manifest",
                    "db.elder06.history", Models + "/Rover/Elder06.prefab", DatabankUnlock.StoryScan("elder06"),
                    RoverFramingYaw),
                Machine("elder01", DatabankMaker.KesslerArai, "1978", DatabankEra.BuildOut, "db.elder01.readout",
                    "db.elder01.history", Models + "/Rover/Elder01.prefab", DatabankUnlock.StoryScan("elder01"),
                    RoverFramingYaw),
                new DatabankEntry("db.scout3", DatabankCategory.Machines, "scan.scout3.name", DatabankMaker.VIS, "1967",
                    DatabankEra.Probes, "scan.scout3.manifest", "scan.scout3.note", DatabankViewKind.Model,
                    LoadModel(Models + "/Props/Prop_OldLander.prefab"), ScoutRemovedParts, 0f, null,
                    DatabankUnlock.StoryScan("scout3")),
                Machine("cargo_lander", DatabankMaker.VIS, "1987", DatabankEra.Lumen, "db.cargo_lander.readout",
                    "db.cargo_lander.history", Models + "/Lander/CargoLander.prefab",
                    DatabankUnlock.FragmentOf(LanderCargo)),
                Machine("kestrel3", DatabankMaker.Tycho, "1985", DatabankEra.Meridian, "db.kestrel3.readout",
                    "db.kestrel3.history", ArtPaths.SitesFolder + "/Site_kestrel.prefab",
                    DatabankUnlock.StoryScan("kestrel3")),
                Structure("habitat", DatabankMaker.VIS, "1984", DatabankEra.Lumen, Models + "/Ruins/HabitatModules.prefab",
                    DatabankUnlock.FragmentOf("HabitatModules")),
                Structure("solar_array", DatabankMaker.VIS, "1984", DatabankEra.Lumen,
                    Models + "/Ruins/FallenSolarArray.prefab", DatabankUnlock.FragmentOf("FallenSolarArray")),
                Structure("radio_tower", DatabankMaker.VIS, "1984", DatabankEra.Lumen,
                    Models + "/Ruins/RadioTower.prefab", DatabankUnlock.FragmentOf("RadioTower")),
                Structure("seismic_station", DatabankMaker.ILC, "1971 · 1984", DatabankEra.Sorties,
                    Models + "/Props/Prop_SeismicStation.prefab", DatabankUnlock.StoryScan("seismic")),
                Structure("trickle_post", DatabankMaker.KesslerArai, "1983", DatabankEra.Lumen,
                    Models + "/Props/DeadChargingPost.prefab", DatabankUnlock.OnWake),
                Structure("retroreflector", DatabankMaker.ILC, "1971 · 1984", DatabankEra.Sorties,
                    Models + "/Props/Prop_Retroreflector.prefab", DatabankUnlock.StoryScan("retroreflector")),
                Print("lumen", DatabankCategory.Places, DatabankMaker.None, "1984", DatabankEra.Lumen,
                    "db.lumen.readout", "db.lumen.history", DatabankUnlock.FragmentOf(LumenSets)),
                Print("earth_eclipse", DatabankCategory.Phenomena, DatabankMaker.None, string.Empty, DatabankEra.None,
                    "db.earth_eclipse.readout", "db.earth_eclipse.history", DatabankUnlock.Seeded),
                Print("crew", DatabankCategory.Archive, DatabankMaker.ILC, "1985", DatabankEra.Lumen,
                    "story.airlock_panel", "db.crew.history", DatabankUnlock.StoryScan("airlock")),
                Card("kessler_arai", DatabankMaker.KesslerArai, "1983", DatabankEra.Lumen, DatabankUnlock.Seeded),
                Card("ilc", DatabankMaker.ILC, "1984", DatabankEra.Lumen, DatabankUnlock.StoryScan("flag")),
                Card("tycho", DatabankMaker.Tycho, "1985", DatabankEra.Meridian, DatabankUnlock.StoryScan("kestrel3")),
                Card("vis", DatabankMaker.VIS, "1967", DatabankEra.Probes, DatabankUnlock.SetComplete(LanderCargo)),
            };
        }

        private static DatabankEntry Machine(string name, DatabankMaker maker, string year, DatabankEra era,
            string readoutKey, string historyKey, string modelPath, DatabankUnlock unlock, float framingYaw = 0f)
        {
            return new DatabankEntry("db." + name, DatabankCategory.Machines, "db." + name + ".name", maker, year, era,
                readoutKey, historyKey, DatabankViewKind.Model, LoadModel(modelPath), null, framingYaw, null, unlock);
        }

        private static DatabankEntry Structure(string name, DatabankMaker maker, string year, DatabankEra era,
            string modelPath, DatabankUnlock unlock)
        {
            return new DatabankEntry("db." + name, DatabankCategory.Structures, "db." + name + ".name", maker, year,
                era, "db." + name + ".readout", "db." + name + ".history", DatabankViewKind.Model, LoadModel(modelPath),
                null, 0f, null, unlock);
        }

        private static DatabankEntry Print(string name, DatabankCategory category, DatabankMaker maker, string year,
            DatabankEra era, string readoutKey, string historyKey, DatabankUnlock unlock)
        {
            return new DatabankEntry("db." + name, category, "db." + name + ".name", maker, year, era, readoutKey,
                historyKey, DatabankViewKind.Print, null, null, 0f, null, unlock);
        }

        /// <summary>A maker's archive card: Art's print of its mark (Art/R1/Brands/print_&lt;brand&gt;.png).</summary>
        private static DatabankEntry Card(string brand, DatabankMaker maker, string year, DatabankEra era,
            DatabankUnlock unlock)
        {
            string name = "card_" + brand;
            return new DatabankEntry("db." + name, DatabankCategory.Archive, "db." + name + ".name", maker, year, era,
                "db." + name + ".readout", "db." + name + ".history", DatabankViewKind.Print, null, null, 0f,
                LoadPrint(Prints + brand + ".png"), unlock);
        }

        private static Texture2D LoadPrint(string path)
        {
            var print = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (print == null)
            {
                throw new InvalidOperationException(
                    $"{GameplayContentBuilder.BuilderPath}: Art print {path} is missing (databank archive card).");
            }

            return print;
        }

        private static GameObject LoadModel(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"{GameplayContentBuilder.BuilderPath}: Art prefab {path} is missing (databank entry model). " +
                    "Run the Art builders.");
            }

            return prefab;
        }
    }
}
