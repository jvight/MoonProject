using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>What one line of the reward card says (the words come from "ui.reward.*" keys).</summary>
    internal enum RewardLineKind
    {
        /// <summary>Home has power for the first time.</summary>
        LightOn = 0,

        /// <summary>Home's lamps burn brighter than before.</summary>
        LightUp = 1,

        /// <summary>The radio picks up the station: the first level of the upgrade to touch the radio.</summary>
        RadioOn = 2,

        /// <summary>The clear radio range grows: <see cref="RewardLine.Number"/> metres farther.</summary>
        Reach = 3,
    }

    /// <summary>One line of the reward card: what it says and the number it carries (0 when none).</summary>
    internal readonly struct RewardLine
    {
        public RewardLine(RewardLineKind kind, int number)
        {
            Kind = kind;
            Number = number;
        }

        public RewardLineKind Kind { get; }

        public int Number { get; }
    }

    /// <summary>
    /// What a level-up gave, read from the upgrade's data and never written by hand (pure, EditMode-tested): power
    /// and light at home, the radio, the range. Only what changed makes a line, in the order the player felt it.
    /// </summary>
    internal static class RewardLines
    {
        /// <summary>The most lines a level can have (one per kind).</summary>
        public const int Max = 4;

        /// <summary>
        /// Fills <paramref name="into"/> with the lines for a level that took the radio's clear radius from
        /// <paramref name="radiusBefore"/> to <paramref name="radiusAfter"/> metres and home's light boost from
        /// <paramref name="boostBefore"/> to <paramref name="boostAfter"/> (1 = unlit); <paramref name="wakesRadio"/>
        /// is true when this is the first level of its upgrade to touch the radio. Returns how many lines.
        /// </summary>
        public static int Compose(float radiusBefore, float radiusAfter, bool wakesRadio, float boostBefore,
            float boostAfter, RewardLine[] into)
        {
            int count = 0;
            if (boostAfter > boostBefore)
            {
                into[count++] = new RewardLine(boostBefore <= 1f ? RewardLineKind.LightOn : RewardLineKind.LightUp, 0);
            }

            if (wakesRadio)
            {
                into[count++] = new RewardLine(RewardLineKind.RadioOn, 0);
            }

            if (radiusAfter > radiusBefore)
            {
                into[count++] = new RewardLine(RewardLineKind.Reach, Mathf.RoundToInt(radiusAfter - radiusBefore));
            }

            return count;
        }
    }
}
