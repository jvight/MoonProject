using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// What else lies around (0.5.5 owner feedback §1.7): robots stopped mid-task, poignant and never grim, slumped and
    /// dust-capped with their eyes dark. "06", the crew's earlier survey cart, built from 07's own parts at a smaller
    /// scale; "Mule", a lamp-and-lift construction bot fallen face-down beside its last lamp post; a haul sled bot
    /// stalled mid-haul with its load still strapped and its capacitor bank open; and a Tilly-kin survey drone caught
    /// in a guy wire. Each space: its root on the ground, +Z the anchor's Forward.
    /// </summary>
    internal static class DerelictMeshes
    {
        // 06 is 07's family at three quarters of its size: the same body language, wheels, neck, head and antenna.
        private const float ElderScale = 0.72f;
        private static readonly Vector3 ElderBody = new Vector3(0.62f, 0.34f, 0.92f);
        private const float ElderBodyY = 0.42f;

        // How 06 slumps: rolled onto its left against the hut's wall, nose down a little.
        private static readonly Vector3 ElderSlump = new Vector3(5f, 0f, 9f);
        private static readonly Vector3 ElderNeckDroop = new Vector3(24f, 6f, 0f);
        private static readonly Vector3 ElderHeadDroop = new Vector3(28f, 0f, 0f);

        /// <summary>06's battery bay door, fallen open on the ground on its right (06 space).</summary>
        public static readonly Vector3 ElderBayDoor = new Vector3(0.66f, 0f, 0.02f);

        /// <summary>
        /// Where Ro's tape case lies by 06 (06 space): on the ground against its flank behind the fallen bay door, by
        /// the rear wheel, where it slid out of the open bay; its front (+Z, the seal) faces out, its length along 06.
        /// </summary>
        public static readonly Vector3 ElderTapeSeat = new Vector3(0.7f, 0f, -0.5f);

        public static readonly Quaternion ElderTapeTurn = Quaternion.Euler(0f, 90f, 0f);

        /// <summary>
        /// Where relay.2's service hut stands by 06 (06 space): on 06's left, its open side turned toward 06, so 06
        /// slumps against it.
        /// </summary>
        public static readonly Vector3 ElderHutSeat = new Vector3(-1.9f, 0f, 0.4f);

        public static readonly Quaternion ElderHutTurn = Quaternion.Euler(0f, 90f, 0f);

        /// <summary>
        /// Where Cell Pack II's fragment <paramref name="index"/> lies on 06 (06 space) and how: one cell half out of
        /// its open battery bay, one on the bay's fallen door, the cell rail on its lid.
        /// </summary>
        public static Vector3 ElderSocket(int index, out Quaternion rotation)
        {
            Matrix4x4 slump = At(Vector3.zero, ElderSlump);
            switch (index)
            {
                case 0:
                    rotation = slump.rotation * Quaternion.Euler(0f, 90f, -18f);
                    return slump.MultiplyPoint3x4(new Vector3(ElderBody.x * 0.5f - 0.08f, ElderBodyY - 0.08f, 0.02f));
                case 1:
                    rotation = Quaternion.Euler(0f, 4f, 2f);
                    return ElderBayDoor + new Vector3(0f, 0.024f, 0.05f);
                default:
                    rotation = slump.rotation * Quaternion.Euler(0f, 90f, 0f);
                    return slump.MultiplyPoint3x4(new Vector3(0f, ElderBodyY + ElderBody.y * 0.5f + 0.03f, -0.12f));
            }
        }

        public static LowPolyMeshBuilder Elder()
        {
            var b = new LowPolyMeshBuilder(4000);
            Matrix4x4 slump = At(Vector3.zero, ElderSlump);
            MeshRange shell = b.Box(slump * At(0f, ElderBodyY, 0f), ElderBody, PaletteSwatch.Enamel, 0.09f);
            b.Box(slump * At(0f, ElderBodyY + 0.03f, 0f), new Vector3(ElderBody.x + 0.012f, 0.06f, ElderBody.z + 0.012f),
                PaletteSwatch.FadedAccent);
            b.Box(slump * At(0f, ElderBodyY - ElderBody.y * 0.5f, 0f), new Vector3(ElderBody.x - 0.06f, 0.05f,
                ElderBody.z - 0.06f), PaletteSwatch.Charcoal);
            MeshRange lid = b.Box(slump * At(0f, ElderBodyY + ElderBody.y * 0.5f + 0.015f, 0f),
                new Vector3(ElderBody.x - 0.08f, 0.03f, ElderBody.z - 0.1f), PaletteSwatch.WornPanel);
            SiteKit.DustCap(b, shell);
            SiteKit.DustCap(b, lid);
            b.Box(slump * At(ElderBody.x * 0.5f + 0.004f, ElderBodyY, 0.02f), new Vector3(0.012f, 0.2f, 0.46f),
                PaletteSwatch.Charcoal);
            Glyphs.Write(b, slump * At(new Vector3(-0.12f, ElderBodyY + 0.02f, -ElderBody.z * 0.5f - 0.006f),
                new Vector3(0f, 180f, 0f)), "06", 0.11f, PaletteSwatch.FadedPaint, PaletteSwatch.Enamel);

            LowPolyMeshBuilder wheel = RoverMeshes.Wheel();
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Matrix4x4 at = slump * Matrix4x4.TRS(new Vector3(x * 0.4f, 0.25f, z * 0.32f), Quaternion.identity,
                        Vector3.one * ElderScale);
                    b.Append(wheel, x > 0 ? at : at * MirrorX);
                }
            }

            Matrix4x4 neck = slump * Matrix4x4.TRS(new Vector3(0f, ElderBodyY + ElderBody.y * 0.5f, 0.28f),
                Rotation(ElderNeckDroop), Vector3.one * ElderScale);
            b.Append(RoverMeshes.Neck(), neck);
            Matrix4x4 head = neck * At(RoverMeshes.HeadHinge, ElderHeadDroop);
            MeshRange hood = b.Append(RoverMeshes.Head(), head);
            SiteKit.DustCap(b, hood);
            Matrix4x4 eye = head * At(RoverMeshes.EyeCentre);
            MeshRange lens = b.Append(RoverMeshes.Eye(), eye);
            b.RepaintFacing(lens, eye.MultiplyVector(Vector3.forward).normalized, 0.2f, PaletteSwatch.Charcoal);
            b.Append(RoverMeshes.Eyelid(), eye * At(Vector3.zero, new Vector3(55f, 0f, 0f)));
            b.Append(RoverMeshes.Antenna(), slump * Matrix4x4.TRS(new Vector3(0.22f, ElderBodyY + ElderBody.y * 0.5f,
                -0.32f), Rotation(new Vector3(-12f, 0f, 0f)), Vector3.one * ElderScale));

            MeshRange door = b.Box(At(ElderBayDoor + new Vector3(0f, 0.012f, 0f), new Vector3(0f, 4f, 2f)),
                new Vector3(0.3f, 0.024f, 0.5f), PaletteSwatch.Enamel, 0.006f);
            SiteKit.DustCap(b, door);
            return b;
        }

        // Mule: a squat tracked base, a telescoping lift mast and its lamp head; it fell forward, the mast along the
        // ground ahead of it.
        private static readonly Vector3 MuleFall = new Vector3(72f, 0f, 6f);

        /// <summary>How Mule lies: tipped forward about its front edge onto its face.</summary>
        public static Matrix4x4 MulePose => At(new Vector3(0f, 0.05f, 0.45f), MuleFall) * At(0f, 0f, -0.45f);

        public static Quaternion MuleFallRotation => Rotation(MuleFall);
        private const float MuleTrackGauge = 0.36f;

        // Mule's lift mast, lying along the ground ahead of it.
        private static Matrix4x4 MuleMast => At(new Vector3(0.05f, 0.1f, 0.5f), new Vector3(86f, 4f, 0f));

        /// <summary>
        /// Where the Warm Headlamp's fragment <paramref name="index"/> lies on Mule (Mule space) and how: its cracked
        /// lamp head still on the mast's tip, its torn reflector on the chassis's back, which faces up now it lies on
        /// its face.
        /// </summary>
        public static Vector3 MuleSocket(int index, out Quaternion rotation)
        {
            if (index == 0)
            {
                rotation = MuleMast.rotation * Quaternion.Euler(-90f, 0f, 0f);
                return MuleMast.MultiplyPoint3x4(new Vector3(0f, 1.8f, 0.05f));
            }

            Vector3 back = MulePose.MultiplyVector(Vector3.back).normalized;
            rotation = Quaternion.FromToRotation(Vector3.up, back) * Quaternion.Euler(0f, 20f, 0f);
            return MulePose.MultiplyPoint3x4(new Vector3(0f, 0.38f, -0.4f));
        }

        /// <summary>Mule's cut piece: the right track's side skirt, pivoted at its top edge (Mule space).</summary>
        public static readonly Vector3 MuleSkirt = new Vector3(0.47f, 0.32f, -0.1f);

        public static LowPolyMeshBuilder Mule()
        {
            var b = new LowPolyMeshBuilder(2500);
            Matrix4x4 fall = MulePose;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(fall * At(side * MuleTrackGauge, 0.13f, 0f), new Vector3(0.18f, 0.26f, 0.95f),
                    PaletteSwatch.Charcoal, 0.05f);
            }

            MeshRange chassis = b.Box(fall * At(0f, 0.38f, 0f), new Vector3(0.62f, 0.28f, 0.78f), PaletteSwatch.Honey,
                0.03f);
            SiteKit.DustCap(b, chassis);
            b.Box(fall * At(0f, 0.38f, 0.395f), new Vector3(0.5f, 0.06f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(fall * At(0f, 0.6f, -0.22f), new Vector3(0.26f, 0.16f, 0.24f), PaletteSwatch.WornPanel, 0.02f);
            b.Prism(fall * At(new Vector3(0f, 0.62f, -0.09f), new Vector3(90f, 0f, 0f)), 0.04f, 0.02f, 8,
                PaletteSwatch.Charcoal);
            Matrix4x4 mast = MuleMast;
            b.Box(mast * At(0f, 0.5f, 0f), new Vector3(0.12f, 1f, 0.12f), PaletteSwatch.Metal);
            b.Box(mast * At(0f, 1.35f, 0f), new Vector3(0.09f, 0.75f, 0.09f), PaletteSwatch.Metal);
            b.Box(mast * At(0f, 1.74f, 0f), new Vector3(0.16f, 0.04f, 0.16f), PaletteSwatch.Charcoal);

            var post = new Vector3(1.25f, -0.4f, 0.4f);
            var top = new Vector3(1.4f, 1.55f, 0.5f);
            RecipeKit.Rod(b, post, top, 0.035f, 6, PaletteSwatch.Metal);
            b.Frustum(At(top + Vector3.up * 0.06f), 0.09f, 0.05f, 0.1f, 8, PaletteSwatch.Honey);
            b.Prism(At(new Vector3(1.26f, 0.02f, 0.4f)), 0.16f, 0.04f, 8, PaletteSwatch.DustShadow);
            return b;
        }

        /// <summary>Mule's track skirt (skirt space: its top edge's middle at the origin), the piece 07 cuts.</summary>
        public static LowPolyMeshBuilder MuleSkirtPiece()
        {
            var b = new LowPolyMeshBuilder(40);
            MeshRange skirt = b.Box(At(0f, -0.1f, 0f), new Vector3(0.03f, 0.2f, 0.7f), PaletteSwatch.Honey, 0.006f);
            SiteKit.DustCap(b, skirt);
            return b;
        }

        // The haul sled: a long low deck on four small wheels, a drooped sensor head at the front, its load strapped
        // on behind and its capacitor bank's side door hanging open.
        private const float SledLength = 1.9f;
        private const float SledWidth = 0.9f;
        private const float SledDeck = 0.38f;

        /// <summary>The strapped load's two pieces (sled space, each pivoted at its base): a crate and a cable
        /// drum.</summary>
        public static readonly Vector3[] SledLoad = { new Vector3(-0.1f, SledDeck + 0.02f, 0.02f),
            new Vector3(0.12f, SledDeck + 0.02f, 0.5f) };

        // The capacitor bank's side door, hanging open from the deck's edge down to the dust.
        private static readonly Vector3 SledDoorHinge = new Vector3(SledWidth * 0.5f, SledDeck - 0.12f, 0.2f);
        private const float SledDoorDrop = 29f;
        private const float SledDoorWidth = 0.5f;

        /// <summary>
        /// Where the Boost Coils' fragment <paramref name="index"/> lies on the sled (sled space) and how: two cells
        /// spilled out of the open bank onto its hanging side door, the bank's frame on the deck's rear, half over its
        /// edges.
        /// </summary>
        public static Vector3 SledSocket(int index, out Quaternion rotation)
        {
            if (index == 2)
            {
                rotation = Quaternion.Euler(0f, 90f, 0f);
                return new Vector3(0f, SledDeck, -0.6f);
            }

            Quaternion door = Quaternion.Euler(0f, 0f, -SledDoorDrop);
            rotation = door * Quaternion.Euler(0f, 90f, 0f);
            return SledDoorHinge + door * new Vector3(SledDoorWidth * 0.5f, 0.02f, index * 0.42f - 0.21f);
        }

        public static LowPolyMeshBuilder HaulSled()
        {
            var b = new LowPolyMeshBuilder(2500);
            MeshRange deck = b.Box(At(0f, SledDeck - 0.04f, 0f), new Vector3(SledWidth, 0.08f, SledLength),
                PaletteSwatch.Sage, 0.02f);
            SiteKit.DustCap(b, deck);
            b.Box(At(0f, SledDeck - 0.16f, 0f), new Vector3(SledWidth - 0.12f, 0.18f, SledLength - 0.2f),
                PaletteSwatch.Charcoal);
            LowPolyMeshBuilder wheel = RoverMeshes.Wheel();
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Matrix4x4 at = Matrix4x4.TRS(new Vector3(x * (SledWidth * 0.5f + 0.02f), 0.2f, z * 0.7f),
                        Quaternion.identity, Vector3.one * 0.58f);
                    b.Append(wheel, x > 0 ? at : at * MirrorX);
                }
            }

            Matrix4x4 head = At(new Vector3(0f, SledDeck + 0.14f, SledLength * 0.5f - 0.1f), new Vector3(30f, -12f, 0f));
            MeshRange cab = b.Box(head, new Vector3(0.36f, 0.22f, 0.2f), PaletteSwatch.Enamel, 0.04f);
            SiteKit.DustCap(b, cab);
            b.Prism(head * At(new Vector3(0f, 0f, 0.105f), new Vector3(90f, 0f, 0f)), 0.05f, 0.02f, 8,
                PaletteSwatch.Charcoal);
            b.Box(At(SledWidth * 0.5f + 0.005f, SledDeck - 0.15f, 0.25f), new Vector3(0.01f, 0.16f, 0.7f),
                PaletteSwatch.Charcoal);
            b.Box(At(SledDoorHinge) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -SledDoorDrop)) *
                  At(SledDoorWidth * 0.5f, 0f, 0f), new Vector3(SledDoorWidth, 0.02f, 0.9f), PaletteSwatch.Sage);
            foreach (Vector3 load in SledLoad)
            {
                b.Box(At(0f, SledDeck + 0.34f, load.z), new Vector3(SledWidth + 0.03f, 0.02f, 0.05f),
                    PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>The strapped crate (piece space: its base's middle at the origin).</summary>
        public static LowPolyMeshBuilder SledCrate()
        {
            var b = new LowPolyMeshBuilder(60);
            MeshRange crate = b.Box(At(0f, 0.16f, 0f), new Vector3(0.6f, 0.32f, 0.45f), PaletteSwatch.WornPanel, 0.02f);
            SiteKit.DustCap(b, crate);
            b.Box(At(0f, 0.16f, 0.228f), new Vector3(0.62f, 0.04f, 0.01f), PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>The strapped cable drum (piece space: its base's middle at the origin).</summary>
        public static LowPolyMeshBuilder SledDrum()
        {
            var b = new LowPolyMeshBuilder(160);
            Matrix4x4 axle = At(new Vector3(0f, 0.2f, 0f), new Vector3(0f, 0f, 90f));
            b.Prism(axle, 0.2f, 0.06f, 10, PaletteSwatch.Wood);
            b.Prism(axle * At(0f, 0.24f, 0f), 0.2f, 0.06f, 10, PaletteSwatch.Wood);
            b.Prism(axle * At(0f, 0.12f, 0f), 0.14f, 0.24f, 10, PaletteSwatch.WarmAccent);
            return b;
        }

        // The drone in the wires: its own length of guy wire rises from the stake toward the mast's collar, and the
        // drone hangs snagged on it by its camera boom, rolled over, one rotor bent.
        private static readonly Vector3 WireTop = new Vector3(0f, 2.9f, 1.85f);
        private const float WireStake = 0.2f;

        /// <summary>Where the camera boom is caught on the wire (drone space), the piece 07 cuts to free the
        /// drone.</summary>
        public static Vector3 DroneSnag => Vector3.Lerp(new Vector3(0f, WireStake, 0f), WireTop, 0.42f);

        /// <summary>Where the drone hangs from its snag (drone space) and how it hangs.</summary>
        public static Vector3 DroneHang => DroneSnag + new Vector3(0.06f, -0.3f, 0.02f);

        public static readonly Vector3 DroneHangEuler = new Vector3(18f, 30f, 128f);

        /// <summary>The wire's stake and the snagged length of wire (drone space).</summary>
        public static LowPolyMeshBuilder DroneWire()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Box(At(0f, -0.1f, 0f), new Vector3(0.24f, 0.4f, 0.24f), PaletteSwatch.Charcoal, 0.02f);
            RecipeKit.Rod(b, new Vector3(0f, WireStake, 0f), WireTop, 0.012f, 5, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The drone itself (Tilly's kin): her body, three rotors and one bent, her eye dark, dust on
        /// top.</summary>
        public static LowPolyMeshBuilder Drone()
        {
            var b = new LowPolyMeshBuilder(1500);
            MeshRange body = b.Append(TillyMeshes.Body(), Matrix4x4.identity);
            SiteKit.DustCap(b, body);
            for (int i = 0; i < TillyMeshes.RotorCount; i++)
            {
                b.Append(i == 1 ? TillyMeshes.BentRotor() : TillyMeshes.Rotor(), Matrix4x4.Translate(
                    TillyMeshes.RotorCentre(i)));
            }

            MeshRange eye = b.Append(TillyMeshes.Eye(), Matrix4x4.Translate(TillyMeshes.EyeCentre));
            b.Repaint(eye, PaletteSwatch.Charcoal);
            b.Append(TillyMeshes.BentAntenna(), Matrix4x4.Translate(TillyMeshes.AntennaBase));
            return b;
        }

        /// <summary>The drone's camera boom caught round the wire (boom space: the snag at the origin).</summary>
        public static LowPolyMeshBuilder DroneBoom()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Torus(At(Vector3.zero, new Vector3(0f, 0f, 90f)), 0.045f, 0.012f, 8, 3, PaletteSwatch.Charcoal);
            RecipeKit.Rod(b, Vector3.zero, new Vector3(0.05f, -0.24f, 0.02f), 0.018f, 6, PaletteSwatch.Metal);
            b.Prism(At(new Vector3(0.05f, -0.26f, 0.02f)), 0.035f, 0.05f, 8, PaletteSwatch.SkyHorizon);
            return b;
        }
    }
}
