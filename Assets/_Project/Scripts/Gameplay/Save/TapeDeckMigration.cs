using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Before 0.5.5 §2 Ro's tape deck was the cassette_player keepsake, dug up and towed to Kenji's port. A save from
    /// then that had it fitted (or gliding onto the tray) keeps it fitted; one that had only dug it up finds the deck
    /// back in Kenji's sealed spare crate.
    /// </summary>
    public static class TapeDeckMigration
    {
        /// <summary>The keepsake id the deck had.</summary>
        public const string LegacyDeckRelic = "cassette_player";

        /// <summary>The keepsake states it had on the port: gliding onto the tray (8) and fitted (9).</summary>
        private const int LegacyPorting = 8;

        private const int LegacyFitted = 9;

        /// <summary>True when <paramref name="relics"/> had the old keepsake deck on the port or fitted on 07.</summary>
        public static bool WasFitted(RelicsSaveData relics)
        {
            if (relics?.relics == null)
            {
                return false;
            }

            foreach (RelicSaveData relic in relics.relics)
            {
                if (relic != null && string.Equals(relic.id, LegacyDeckRelic, StringComparison.Ordinal) &&
                    (relic.state == LegacyPorting || relic.state == LegacyFitted))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
