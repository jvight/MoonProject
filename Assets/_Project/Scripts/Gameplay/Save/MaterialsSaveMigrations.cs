using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Steps of the "gameplay.wallet" section from one schema version to the next.</summary>
    public static class MaterialsSaveMigrations
    {
        /// <summary>
        /// Scrap per material unit in the version 1 to 2 conversion: the last scrap economy priced everything at 705
        /// scrap (the tower 135, Hover-Jump 150, the relay masts 420) and the recipes ask for 34 units, about 20 to 1.
        /// </summary>
        public const int ScrapPerUnit = 20;

        /// <summary>
        /// Units are dealt out in this order, over and over: 3 metal to 3 wiring to 1 optics, close to what the recipes
        /// ask for all together (16 metal, 13 wiring, 5 optics).
        /// </summary>
        private static readonly int[] DealOrder = { 0, 1, 0, 1, 0, 1, 2 };

        /// <summary>
        /// Version 1 to 2 replaced the scrap balance with salvaged materials: an old save's scrap becomes a fair
        /// material stock (<see cref="Convert"/>), so nothing a player gathered is lost.
        /// </summary>
        public static string Migrate(string json, int fromVersion)
        {
            if (fromVersion == 1)
            {
                WalletSaveData wallet = JsonUtility.FromJson<WalletSaveData>(json);
                if (wallet == null)
                {
                    throw new FormatException($"{GameplaySaveKeys.Materials} version 1 holds no data.");
                }

                return JsonUtility.ToJson(Convert(wallet.balance));
            }

            throw new InvalidOperationException(
                $"{GameplaySaveKeys.Materials} has no migration from version {fromVersion} to {fromVersion + 1}.");
        }

        /// <summary>
        /// The material stock worth <paramref name="scrap"/>: one unit per <see cref="ScrapPerUnit"/> scrap (rounded to
        /// the nearest unit), dealt out metal, wiring, metal, wiring, metal, wiring, optics, and again.
        /// </summary>
        public static MaterialsSaveData Convert(int scrap)
        {
            int units = Mathf.Max(0, Mathf.RoundToInt(scrap / (float)ScrapPerUnit));
            var stock = new MaterialsSaveData();
            for (int i = 0; i < units; i++)
            {
                switch (DealOrder[i % DealOrder.Length])
                {
                    case 0:
                        stock.metal++;
                        break;
                    case 1:
                        stock.wiring++;
                        break;
                    default:
                        stock.optics++;
                        break;
                }
            }

            return stock;
        }
    }
}
