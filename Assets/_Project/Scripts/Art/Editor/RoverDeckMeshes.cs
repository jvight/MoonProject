using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The old tape deck Tilly spotted (docs/features/M3-24 C), strapped across 07's back above the hatch: a
    /// cream-enamel deck on a metal bracket, a dark cassette window with two honey reels, three piano keys on top and a
    /// slotted speaker at its front end, held by two honey straps. Built in DeckSocket space: +X out from the body,
    /// +Y up, +Z forward, the bracket on the socket. Small and rounded, so it reads as a kept treasure, never a face.
    /// </summary>
    internal static class RoverDeckMeshes
    {
        private const float BracketDepth = 0.014f;
        private const float BracketHeight = 0.15f;
        private const float BracketLength = 0.3f;
        private const float DeckDepth = 0.07f;
        private const float DeckHeight = 0.13f;
        private const float DeckLength = 0.27f;
        private const float WindowHeight = 0.052f;
        private const float WindowLength = 0.13f;
        private const float WindowBack = -0.04f;
        private const float ReelRadius = 0.016f;
        private const float ReelSpacing = 0.034f;
        private const float KeyWidth = 0.026f;
        private const float KeyHeight = 0.014f;
        private const float KeyDepth = 0.04f;
        private const int GrilleBars = 4;
        private const float GrilleBar = 0.006f;
        private const float StrapWidth = 0.016f;
        private const float StrapLift = 0.004f;
        private const float Skin = 0.002f;
        private const int Sides = 10;

        /// <summary>Lays a prism's local Y along +X, out of the deck's face.</summary>
        private static readonly Vector3 AlongX = new Vector3(0f, 0f, 90f);

        public static LowPolyMeshBuilder Deck()
        {
            var b = new LowPolyMeshBuilder(420);
            b.Box(At(BracketDepth * 0.5f, 0f, 0f), new Vector3(BracketDepth, BracketHeight, BracketLength),
                PaletteSwatch.Metal, 0.003f);
            float x = BracketDepth + DeckDepth * 0.5f;
            b.Box(At(x, 0f, 0f), new Vector3(DeckDepth, DeckHeight, DeckLength), PaletteSwatch.Cream, 0.012f);
            float face = BracketDepth + DeckDepth;
            b.Box(At(face + Skin * 0.5f, 0.008f, WindowBack),
                new Vector3(Skin, WindowHeight, WindowLength), PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(face + Skin, 0.008f, WindowBack + side * ReelSpacing), AlongX),
                    ReelRadius, Skin * 2f, Sides, PaletteSwatch.Honey);
            }

            for (int key = -1; key <= 1; key++)
            {
                b.Box(At(x, DeckHeight * 0.5f + KeyHeight * 0.5f, WindowBack + key * (KeyWidth + 0.006f)),
                    new Vector3(KeyDepth, KeyHeight, KeyWidth), key == 0 ? PaletteSwatch.Honey : PaletteSwatch.Metal,
                    0.003f);
            }

            float grilleZ = DeckLength * 0.5f - 0.05f;
            for (int bar = 0; bar < GrilleBars; bar++)
            {
                float y = -0.03f + bar * 0.02f;
                b.Box(At(face + Skin * 0.5f, y, grilleZ), new Vector3(Skin, GrilleBar, 0.06f), PaletteSwatch.Charcoal);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * (DeckLength * 0.5f - 0.035f);
                b.Box(At(x + StrapLift * 0.5f, 0f, z),
                    new Vector3(DeckDepth + StrapLift, DeckHeight + StrapLift * 2f, StrapWidth), PaletteSwatch.Honey,
                    0.003f);
            }

            return b;
        }
    }
}
