using UnityEngine;
using MoonProject.World;

namespace MoonProject.Autoplay
{
    /// <summary>The world's cues as the bot can wait for them (<see cref="WorldCue"/>): lit, and where they stand.</summary>
    internal static class WorldCues
    {
        /// <summary>The cue's level counts as lit above this share of its full glow.</summary>
        private const float LitLevel = 0.2f;

        /// <summary>The fallen array's glint in the new sunlight (opening v3 R4), or null when the scene has none.</summary>
        public static WorldCue ArrayGlint(AutoplayWorld world)
        {
            ArrayGlint glint = world.Find<ArrayGlint>();
            if (glint == null)
            {
                return null;
            }

            return new WorldCue("the fallen array's glint", () =>
            {
                for (int wing = 0; wing < glint.WingCount; wing++)
                {
                    if (glint.LevelOf(wing) > LitLevel)
                    {
                        return true;
                    }
                }

                return false;
            }, () => glint.transform.position);
        }

        /// <summary>Every cue the scene has, for a check that wants any of them.</summary>
        public static System.Collections.Generic.List<WorldCue> All(AutoplayWorld world)
        {
            var cues = new System.Collections.Generic.List<WorldCue>(2);
            WorldCue glint = ArrayGlint(world);
            if (glint != null)
            {
                cues.Add(glint);
            }

            WorldCue lamp = ModuleLamp(world);
            if (lamp != null)
            {
                cues.Add(lamp);
            }

            return cues;
        }

        /// <summary>The habitat's power module lamp, blinking from dusk (opening v3 R4), or null.</summary>
        public static WorldCue ModuleLamp(AutoplayWorld world)
        {
            ModuleStatusLamp lamp = world.Find<ModuleStatusLamp>();
            return lamp == null ? null : new WorldCue("the power module's status lamp", () => lamp.Level > LitLevel,
                () => lamp.transform.position);
        }
    }
}
