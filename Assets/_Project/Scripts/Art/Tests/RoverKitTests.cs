using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// 07's visible kit and friends' gifts (M3-11, VISION rulings 11 and 12): the contract nodes and sockets, dark glow
    /// renderers lit through the glow-off material, lamp glasses smaller and lower than the eye, gifts hidden until
    /// earned, every kit piece changing 07's outline from the chase camera and at 30 m and the whole kit reading
    /// clearly at 30 m, the lamp bar seen beside the head, the drum bands and lamp glasses in view, the serials still
    /// read, the kit clear of the wheels at full travel and steer, of the face at every head pose and of the solar
    /// wing, a kitted 07 still on Kenji's turntable, rust at the kit's bolts and seams, and repeatable builds.
    /// </summary>
    public sealed class RoverKitTests
    {
        // The default chase camera (RoverCameraTuning asset): 7.5 m out, 16 degrees up, looking at 1.1 m through a
        // 52 degree lens; straight behind once it recentres, or swung to the rear three-quarter on either side.
        private const float ChaseDistance = 7.5f;
        private const float ChasePitch = 16f;
        private const float ChaseYaw = 35f;
        private const float Broadside = 90f;
        private const float FarDistance = 30f;
        private const float Fov = 52f;
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        // The share the whole kit adds to the bare outline at 30 m from every chase view: minute one and fully kitted
        // must not look alike.
        private const float MinFarKitShare = 0.12f;

        // Seen in front of the rest of 07: a big, mostly unhidden lamp bar from every chase view, each drum band from
        // its best chase view, and each lamp glass through its guard from straight ahead.
        private const int MinLampBarSeenPixels = 1800;
        private const float MinLampBarSeen = 0.6f;
        private const int MinBandPixels = 150;
        private const float MinGlassSeen = 0.6f;
        private const float MinSerialKept = 0.95f;
        private const int MinSerialPixels = 50;
        private const float MinTailLampsKept = 0.9f;

        // A lamp glass is a small light under the face: at most half the lens's radius, its top this far below the eye.
        private const float MaxGlassToLens = 0.5f;
        private const float EyeHeadroom = 0.05f;

        // Rover's rig and character limits (RoverRigTuning, RoverCharacterTuning assets): wheel travel up into the
        // body, front steering and the rear wheels' opposite share, neck yaw, head pitch up and down, the eyelid's
        // closed angle and how far the solar wing opens.
        private const float WheelTravel = RoverModelBuilder.MiddleWheelTravel;
        private const float FrontSteer = 24f;
        private const float RearSteer = 9.6f;
        private const float NeckYawLimit = 110f;
        private const float HeadPitchUp = 55f;
        private const float HeadPitchDown = 25f;
        private const float EyelidClosed = 75f;
        private const float WingOpen = 110f;
        private const float PoseStep = 10f;
        private const float LidStep = 25f;

        private const float WheelClearance = 0.03f;
        private const float FaceClearance = 0.03f;
        private const float WingClearance = 0.01f;
        private const float TurntableRoom = 0.25f;
        private const float MaxRustShare = 0.08f;
        private const float StripePaint = 0.012f;

        private static readonly Vector3 ChaseTarget = new Vector3(0f, 1.1f, 0f);
        private static readonly float[] ChaseYaws = { -ChaseYaw, 0f, ChaseYaw };

        // Pixels each piece must add to 07's outline at 1080p from its best chase view (the drums together): an easy
        // read up close, a clear shape at 30 m. The rack sits behind the body between the rear wheels, so from the
        // chase views it mostly reads by its boards and straps inside the outline rather than by the outline itself.
        private static readonly Dictionary<string, int> ChaseBudget = new Dictionary<string, int>
        {
            { RoverKitBuilder.LampBarName, 2200 }, { RoverKitBuilder.CapacitorDrumName, 3500 },
            { RoverKitBuilder.CargoRackName, 2200 },
        };

        private static readonly Dictionary<string, int> FarBudget = new Dictionary<string, int>
        {
            { RoverKitBuilder.LampBarName, 150 }, { RoverKitBuilder.CapacitorDrumName, 200 },
            { RoverKitBuilder.CargoRackName, 40 },
        };

        private ModelNode _rover;
        private List<Vector3> _bare;
        private List<KitPiece> _pieces;
        private List<Vector3> _frames;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _rover = RoverModelBuilder.CreateModel();
            _bare = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(_rover, Matrix4x4.identity, _bare);
            _pieces = new List<KitPiece>
            {
                new KitPiece(RoverKitBuilder.LampBarName, Fit(RoverKitBuilder.CreateLampBar(), "HeadlampSocket")),
                new KitPiece(RoverKitBuilder.CapacitorDrumName + " L",
                    Fit(RoverKitBuilder.CreateCapacitorDrum(), "DrumSocket_L")),
                new KitPiece(RoverKitBuilder.CapacitorDrumName + " R",
                    Fit(RoverKitBuilder.CreateCapacitorDrum(), "DrumSocket_R")),
                new KitPiece(RoverKitBuilder.CargoRackName, Fit(RoverKitBuilder.CreateCargoRack(), "CargoSocket")),
            };

            // 07 with every kit piece's frame but none of their glow renderers: what a glass or band is seen past.
            _frames = new List<Vector3>(_bare);
            foreach ((ModelNode kit, string socket) in new[]
            {
                (RoverKitBuilder.CreateLampBar(), "HeadlampSocket"), (RoverKitBuilder.CreateCapacitorDrum(),
                    "DrumSocket_L"), (RoverKitBuilder.CreateCapacitorDrum(), "DrumSocket_R"),
                (RoverKitBuilder.CreateCargoRack(), "CargoSocket"),
            })
            {
                Matrix4x4 world = SilhouetteCamera.WorldOf(_rover, socket);
                foreach (Vector3 p in kit.Mesh.Geometry.Positions)
                {
                    _frames.Add(world.MultiplyPoint3x4(p));
                }
            }
        }

        [Test]
        public void Kit_HasTheContractNodes()
        {
            ModelNode bar = RoverKitBuilder.CreateLampBar();
            Assert.AreEqual("Kit_LampBar", bar.Name);
            CollectionAssert.AreEqual(new[] { "Lamp_0", "Lamp_1", "Lamp_2" }, bar.Children.Select(c => c.Name));

            ModelNode drum = RoverKitBuilder.CreateCapacitorDrum();
            Assert.AreEqual("Kit_CapacitorDrum", drum.Name);
            CollectionAssert.AreEqual(new[] { "Glow" }, drum.Children.Select(c => c.Name));

            ModelNode rack = RoverKitBuilder.CreateCargoRack();
            Assert.AreEqual("Kit_CargoRack", rack.Name);
            ModelNode seat = rack.Children.Single();
            Assert.AreEqual("RelicSeat", seat.Name);
            Assert.IsNull(seat.Mesh, "RelicSeat is an empty");
            Assert.AreEqual(Quaternion.identity, seat.LocalRotation, "RelicSeat's +Y is up");

            foreach (ModelNode kit in new[] { bar, drum, rack })
            {
                Assert.AreEqual(Vector3.zero, kit.LocalPosition, $"{kit.Name} parents to its socket with identity");
                Assert.AreEqual(Quaternion.identity, kit.LocalRotation, kit.Name);
                foreach (ModelNode node in Nodes(kit).Where(n => n.Mesh != null))
                {
                    MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                }
            }
        }

        [Test]
        public void DrumSockets_SitOnTheFlanks_PointingOut()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                ModelNode socket = _rover.GetDescendant(side < 0 ? "DrumSocket_L" : "DrumSocket_R");
                Assert.IsNull(socket.Mesh);
                Assert.AreEqual(side, Mathf.Sign(socket.LocalPosition.x), socket.Name);
                Assert.Greater(side * (socket.LocalRotation * Vector3.right).x, 0.999f, $"{socket.Name} +X outward");
                Assert.Greater((socket.LocalRotation * Vector3.up).y, 0.999f, $"{socket.Name} +Y up");
            }

            Vector3 rack = RoverKitBuilder.CreateCargoRack().GetDescendant("RelicSeat").LocalPosition;
            Assert.Less(rack.z, 0f, "the relic rides behind the cargo socket");
        }

        [Test]
        public void GlowRenderers_AreDarkUntilLit_LampsWarm_DrumCyanLikeTheCoils()
        {
            ModelNode bar = RoverKitBuilder.CreateLampBar();
            ModelNode drum = RoverKitBuilder.CreateCapacitorDrum();
            PaletteSwatch coil = MeshChecks.Swatches(RoverModelBuilder.CreateHoverCoils().GetDescendant("Glow_FL")
                .Mesh.Geometry).Single();
            foreach (ModelNode node in Nodes(bar).Concat(Nodes(drum)).Concat(Nodes(RoverKitBuilder.CreateCargoRack()))
                .Where(n => n.Mesh != null))
            {
                List<PaletteSwatch> swatches = MeshChecks.Swatches(node.Mesh.Geometry);
                bool glow = node.Name.StartsWith("Lamp_") || node.Name == "Glow";
                Assert.AreEqual(glow ? ModelMaterial.PaletteGlowOff : ModelMaterial.Palette, node.Material, node.Name);
                Assert.IsTrue(glow ? swatches.All(Palette.IsEmissive) : !swatches.Any(Palette.IsEmissive),
                    $"{node.Name}: only the glow renderers glow");
                Assert.LessOrEqual(swatches.Count, 4, $"{node.Name} keeps to four swatches");
            }

            foreach (ModelNode lamp in bar.Children)
            {
                Color warm = Palette.GetGlow(MeshChecks.Swatches(lamp.Mesh.Geometry).Single());
                Assert.Greater(warm.r, Palette.BloomThreshold, "lit at 1 the lamp blooms");
                Assert.Greater(warm.r, warm.g, "warm");
                Assert.Greater(warm.g, warm.b, "warm");
            }

            Assert.AreEqual(coil, MeshChecks.Swatches(drum.GetDescendant("Glow").Mesh.Geometry).Single(),
                "the drum band glows the coils' cyan");
            Bounds band = drum.GetDescendant("Glow").Mesh.Geometry.Bounds;
            Bounds shell = SwatchBounds(drum.Mesh.Geometry, PaletteSwatch.Enamel);
            Assert.Greater(band.extents.y, shell.extents.y, "the band wraps the whole drum");
        }

        [Test]
        public void LampGlasses_StaySmallerAndLowerThanTheEye_TheOuterOnesBesideTheHead()
        {
            ModelNode eye = _rover.GetDescendant("Eye");
            float lens = SwatchBounds(eye.Mesh.Geometry, PaletteSwatch.WarmLamp).extents.x;
            float eyeBottom = Mounted(eye, "Neck/Head/Eye").min.y;
            float headSide = Mounted(_rover.GetDescendant("Head"), "Neck/Head").max.x;
            Matrix4x4 socket = SilhouetteCamera.WorldOf(_rover, "HeadlampSocket");
            float glassArea = 0f;
            foreach (ModelNode lamp in RoverKitBuilder.CreateLampBar().Children)
            {
                float radius = lamp.Mesh.Geometry.Bounds.extents.x;
                Vector3 centre = socket.MultiplyPoint3x4(lamp.LocalPosition);
                glassArea += radius * radius;
                Assert.LessOrEqual(radius, MaxGlassToLens * lens, $"{lamp.Name} is a small light, not a second eye");
                Assert.LessOrEqual(centre.y + radius, eyeBottom - EyeHeadroom, $"{lamp.Name} sits below the eye");
                if (lamp.Name != "Lamp_1")
                {
                    Assert.GreaterOrEqual(Mathf.Abs(centre.x) - radius, headSide,
                        $"{lamp.Name} stands beside the head, where the chase camera sees it");
                }
            }

            Assert.Less(glassArea, lens * lens, "the three glasses together are less light than the one eye");
        }

        [Test]
        public void Gifts_AreHiddenUntilEarned_EverythingElseShows()
        {
            foreach (ModelNode node in Nodes(_rover))
            {
                bool gift = node.Name == "Decal07Fresh" || node.Name == "CellFilled" || node.Name == "Pennant";
                Assert.AreEqual(!gift, node.Active, node.Name);
                if (gift)
                {
                    MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                }
            }

            foreach (ModelNode kit in new[]
            {
                RoverKitBuilder.CreateLampBar(), RoverKitBuilder.CreateCapacitorDrum(),
                    RoverKitBuilder.CreateCargoRack(),
            })
            {
                Assert.IsTrue(Nodes(kit).All(n => n.Active), $"{kit.Name} shows whole once fitted");
            }
        }

        [Test]
        public void FreshSerial_BuriesTheFadedOne_AndCellFillsTheGap_AndPennantClipsToTheWhip()
        {
            LowPolyMeshBuilder body = _rover.GetDescendant("Body").Mesh.Geometry;
            LowPolyMeshBuilder fresh = _rover.GetDescendant("Decal07Fresh").Mesh.Geometry;
            var patches = new List<Bounds>();
            foreach (System.Func<Vector3, bool> region in new System.Func<Vector3, bool>[]
            {
                p => p.x > 0.3f, p => p.x < -0.3f, p => p.z < -0.5f,
            })
            {
                patches.Add(BoundsOf(fresh, PaletteSwatch.WarmAccent, region));
            }

            int faded = 0;
            for (int t = 0; t < body.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(body, t) != PaletteSwatch.FadedPaint)
                {
                    continue;
                }

                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = body.Positions[t * 3 + k];
                    faded++;
                    Assert.IsTrue(patches.Any(patch => patch.Contains(p)), $"faded serial point {p} shows through");
                }
            }

            Assert.Greater(faded, 0, "07 starts with a faded serial");
            CollectionAssert.Contains(MeshChecks.Swatches(fresh), PaletteSwatch.Cream, "the fresh numerals are crisp");

            ModelNode cell = _rover.GetDescendant("CellFilled");
            LowPolyMeshBuilder wing = _rover.GetDescendant("SolarWing").Mesh.Geometry;
            for (int t = 0; t < wing.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(wing, t) == PaletteSwatch.SkyHorizon)
                {
                    Vector3 p = wing.Positions[t * 3];
                    Assert.Greater(new Vector2(p.x - cell.LocalPosition.x, p.z - cell.LocalPosition.z).magnitude,
                        0.08f, "the filled cell sits in the gap, not over a cell");
                }
            }

            Assert.IsTrue(wing.Bounds.Contains(cell.LocalPosition), "the gap is inside the wing");
            Vector3 clip = _rover.GetDescendant("Pennant").LocalPosition;
            Assert.Less(DistanceToSurface(clip, _rover.GetDescendant("Antenna").Mesh.Geometry), 0.012f,
                "the pennant clips onto the whip");
        }

        [Test]
        public void Kit_IsSizedToRead_PastTheFlanks_OverTheShoulders_BehindTheBody()
        {
            Bounds body = _rover.GetDescendant("Body").Mesh.Geometry.Bounds;
            Bounds bar = _pieces[0].Bounds;
            Assert.Greater(bar.max.x, body.max.x + 0.05f, "the lamp bar runs past the right flank");
            Assert.Less(bar.min.x, body.min.x - 0.05f, "and past the left one");
            foreach (KitPiece drum in _pieces.Skip(1).Take(2))
            {
                Assert.Greater(drum.Bounds.max.y, body.max.y + 0.25f, $"{drum.Name} rides high on the shoulder");
                Assert.Greater(Mathf.Max(drum.Bounds.max.x, -drum.Bounds.min.x), body.max.x + 0.3f,
                    $"{drum.Name} stands out past the flank");
                Assert.Greater(drum.Bounds.size.z, 0.45f, $"{drum.Name} is a big battery, not a tin");
            }

            Bounds rack = _pieces[3].Bounds;
            Assert.Less(rack.min.z, body.min.z - 0.45f, "the rack reaches well behind the body");
            Assert.Greater(rack.size.x, body.size.x - 0.15f, "and nearly as wide as it");
        }

        [Test]
        public void Kit_KeepsClearOfTheWheels_AtFullTravelAndSteer()
        {
            foreach (string side in new[] { "L", "R" })
            {
                foreach (string row in new[] { "F", "M", "R" })
                {
                    ModelNode wheel = _rover.GetDescendant($"Wheel_{row}{side}");
                    float steer = row == "F" ? FrontSteer : row == "R" ? RearSteer : 0f;
                    foreach (float travel in new[] { 0f, WheelTravel })
                    {
                        foreach (float turn in new[] { -steer, 0f, steer })
                        {
                            var triangles = new List<Vector3>();
                            Collect(wheel, Pose(wheel.LocalPosition + Vector3.up * travel, new Vector3(0f, turn, 0f)),
                                null, triangles);
                            foreach (KitPiece piece in _pieces)
                            {
                                float gap = piece.GapTo(triangles, 2f * WheelClearance, out Vector3 where);
                                Assert.GreaterOrEqual(gap, WheelClearance,
                                    $"{piece.Name} and wheel {row}{side} (travel {travel}, steer {turn}) are only " +
                                    $"{gap:F3} m apart at {where}");
                            }
                        }
                    }
                }

                ModelNode bogie = _rover.GetDescendant("Bogie_" + side);
                var arm = new List<Vector3>();
                SilhouetteCamera.CollectTriangles(bogie, Matrix4x4.identity, arm);
                foreach (KitPiece piece in _pieces)
                {
                    Assert.GreaterOrEqual(piece.GapTo(arm, 2f * WheelClearance), WheelClearance,
                        $"{piece.Name} and bogie {side}");
                }
            }
        }

        [Test]
        public void Kit_KeepsClearOfTheFace_AtEveryHeadPose()
        {
            ModelNode neck = _rover.GetDescendant("Neck");
            var turns = new Dictionary<string, Quaternion>();
            var face = new List<Vector3>();
            for (float yaw = -NeckYawLimit; yaw <= NeckYawLimit; yaw += PoseStep)
            {
                foreach (float pitch in Steps(-HeadPitchUp, HeadPitchDown, PoseStep))
                {
                    foreach (float lid in Steps(0f, EyelidClosed, LidStep))
                    {
                        turns["Head"] = Quaternion.Euler(pitch, 0f, 0f);
                        turns["Eyelid"] = Quaternion.Euler(lid, 0f, 0f);
                        face.Clear();
                        Collect(neck, Pose(neck.LocalPosition, new Vector3(0f, yaw, 0f)), turns, face);
                        foreach (KitPiece piece in _pieces)
                        {
                            float gap = piece.GapTo(face, 2f * FaceClearance, out Vector3 where);
                            Assert.GreaterOrEqual(gap, FaceClearance,
                                $"{piece.Name} comes {gap:F3} m from the face at {where} (neck yaw {yaw}, head pitch " +
                                $"{pitch}, lid {lid})");
                        }
                    }
                }
            }
        }

        [Test]
        public void Drums_KeepClearOfTheSolarWing_FoldedToFullyOpen()
        {
            ModelNode wing = _rover.GetDescendant("SolarWing");
            var panel = new List<Vector3>();
            foreach (float angle in Steps(0f, WingOpen, PoseStep))
            {
                panel.Clear();
                Collect(wing, Pose(wing.LocalPosition, new Vector3(angle, 0f, 0f)), null, panel);
                foreach (KitPiece drum in _pieces.Skip(1).Take(2))
                {
                    float gap = drum.GapTo(panel, 2f * WingClearance, out Vector3 where);
                    Assert.GreaterOrEqual(gap, WingClearance,
                        $"{drum.Name} comes {gap:F3} m from the wing at {where}, open {angle} degrees");
                }
            }
        }

        [Test]
        public void EveryKitPiece_ChangesTheOutline_FromTheChaseCamera()
        {
            AssertEveryPieceAdds(ChaseDistance, ChaseBudget);
        }

        [Test]
        public void EveryKitPiece_StillShapesTheOutline_At30Metres()
        {
            AssertEveryPieceAdds(FarDistance, FarBudget);
        }

        [Test]
        public void TheWholeKit_ReadsClearly_At30Metres_FromEveryChaseView()
        {
            var kitted = new List<Vector3>(_bare);
            foreach (KitPiece piece in _pieces)
            {
                kitted.AddRange(piece.Triangles);
            }

            SilhouetteCamera.CollectTriangles(RoverModelBuilder.CreateHoverCoils(),
                SilhouetteCamera.WorldOf(_rover, "CoilSocket"), kitted);
            var shares = new Dictionary<float, float>();
            foreach (float yaw in ChaseYaws.Concat(new[] { -Broadside, Broadside }))
            {
                SilhouetteCamera camera = Chase(FarDistance, yaw);
                bool[] bare = camera.NewMask();
                camera.Rasterize(_bare, bare);
                bool[] full = camera.NewMask();
                camera.Rasterize(kitted, full);
                int outline = SilhouetteCamera.Added(bare, camera.NewMask());
                shares[yaw] = (float)SilhouetteCamera.Added(full, bare) / outline;
                TestContext.WriteLine($"the whole kit adds {shares[yaw]:P1} to 07's {outline} px outline at 30 m, " +
                    $"yaw {yaw}");
            }

            foreach (float yaw in ChaseYaws)
            {
                Assert.GreaterOrEqual(shares[yaw], MinFarKitShare,
                    $"minute one and fully kitted look alike at yaw {yaw}");
            }
        }

        [Test]
        public void LampBar_IsSeenBesideTheHead_FromTheChaseCamera()
        {
            // What of the bar stands over the lid (bar, lamps, end caps): the part the chase camera can see at all.
            float lid = _rover.GetDescendant("Body").Mesh.Geometry.Bounds.max.y;
            List<Vector3> bar = _pieces[0].Triangles;
            var top = new List<Vector3>();
            for (int t = 0; t + 2 < bar.Count; t += 3)
            {
                if (bar[t].y > lid && bar[t + 1].y > lid && bar[t + 2].y > lid)
                {
                    top.Add(bar[t]);
                    top.Add(bar[t + 1]);
                    top.Add(bar[t + 2]);
                }
            }

            var seen = new List<(float, int, int)>();
            foreach (float yaw in ChaseYaws)
            {
                SilhouetteCamera camera = Chase(ChaseDistance, yaw);
                bool[] alone = camera.NewMask();
                camera.Rasterize(top, alone);
                int whole = SilhouetteCamera.Added(alone, camera.NewMask());
                int shown = SilhouetteCamera.Count(Owners(camera, _bare, top), 2);
                TestContext.WriteLine($"{shown} of the lamp bar's {whole} px over the lid are seen at yaw {yaw}");
                seen.Add((yaw, whole, shown));
            }

            foreach ((float yaw, int whole, int shown) in seen)
            {
                Assert.GreaterOrEqual(shown, MinLampBarSeenPixels, $"the lamp bar is too small to read at yaw {yaw}");
                Assert.GreaterOrEqual(shown, MinLampBarSeen * whole,
                    $"the lamp bar hides behind the head at yaw {yaw}");
            }
        }

        [Test]
        public void DrumBands_ShowFromTheChaseCamera()
        {
            ModelNode band = RoverKitBuilder.CreateCapacitorDrum().GetDescendant(RoverKitBuilder.DrumGlowName);

            foreach (string socket in new[] { "DrumSocket_L", "DrumSocket_R" })
            {
                var glow = new List<Vector3>();
                SilhouetteCamera.CollectTriangles(band, SilhouetteCamera.WorldOf(_rover, socket), glow);
                int best = 0;
                foreach (float yaw in ChaseYaws)
                {
                    int seen = SilhouetteCamera.Count(Owners(Chase(ChaseDistance, yaw), _frames, glow), 2);
                    TestContext.WriteLine($"{socket}'s band shows {seen} px at yaw {yaw}");
                    best = Mathf.Max(best, seen);
                }

                Assert.GreaterOrEqual(best, MinBandPixels, $"{socket}'s band hides from the chase camera");
            }
        }

        [Test]
        public void LampGlasses_ShowThroughTheirGuards_FromAhead()
        {
            ModelNode bar = RoverKitBuilder.CreateLampBar();
            Matrix4x4 socket = SilhouetteCamera.WorldOf(_rover, "HeadlampSocket");
            SilhouetteCamera camera = SilhouetteCamera.Orbit(new Vector3(0f, 1f, 0f), 4f, 8f, 180f, Fov, ScreenWidth,
                ScreenHeight);
            foreach (ModelNode lamp in bar.Children)
            {
                var glass = new List<Vector3>();
                SilhouetteCamera.CollectTriangles(lamp, socket, glass);
                bool[] alone = camera.NewMask();
                camera.Rasterize(glass, alone);
                int whole = SilhouetteCamera.Added(alone, camera.NewMask());
                float seen = (float)SilhouetteCamera.Count(Owners(camera, _frames, glass), 2) / whole;
                TestContext.WriteLine($"{lamp.Name}: {seen:P0} of its {whole} px glass shows through the guard");
                Assert.GreaterOrEqual(seen, MinGlassSeen, $"{lamp.Name}'s guard hides its light");
            }
        }

        [Test]
        public void The07_StillReads_WithTheKitFitted()
        {
            var kit = new List<Vector3>();
            foreach (KitPiece piece in _pieces)
            {
                kit.AddRange(piece.Triangles);
            }

            // The back "07" is who the chase camera follows; the flank ones are read from the side (Bell's fresh paint
            // moment). From the rear three-quarter the near drum overlaps the top of the flank numbers: logged only.
            var serials = new[]
            {
                ("back", (System.Func<Vector3, bool>)(p => p.z < -0.5f), new[] { 0f, -ChaseYaw, ChaseYaw },
                    new float[0]),
                ("right flank", p => p.x > 0.3f, new[] { Broadside }, new[] { ChaseYaw }),
                ("left flank", p => p.x < -0.3f, new[] { -Broadside }, new[] { -ChaseYaw }),
            };
            var kept = new List<(string, float, int, int)>();
            foreach ((string where, System.Func<Vector3, bool> region, float[] read, float[] logged) in serials)
            {
                var serial = new List<Vector3>();
                var rest = new List<Vector3>();
                SplitSerial(region, serial, rest);
                var kitted = new List<Vector3>(rest);
                kitted.AddRange(kit);
                foreach (float yaw in read.Concat(logged))
                {
                    SilhouetteCamera camera = Chase(ChaseDistance, yaw);
                    int bare = SilhouetteCamera.Count(Owners(camera, rest, serial), 2);
                    int fitted = SilhouetteCamera.Count(Owners(camera, kitted, serial), 2);
                    TestContext.WriteLine($"the {where} \"07\" shows {fitted} of {bare} px with the kit, yaw {yaw}");
                    if (System.Array.IndexOf(read, yaw) >= 0)
                    {
                        kept.Add((where, yaw, bare, fitted));
                    }
                }
            }

            foreach ((string where, float yaw, int bare, int fitted) in kept)
            {
                Assert.GreaterOrEqual(bare, MinSerialPixels, $"the {where} \"07\" should read bare at yaw {yaw}");
                Assert.GreaterOrEqual(fitted, MinSerialKept * bare, $"the kit covers the {where} \"07\" at yaw {yaw}");
            }
        }

        [Test]
        public void TailLamps_StillGlowPastTheRack_FromBehind()
        {
            var lamps = new List<Vector3>();
            var rest = new List<Vector3>();
            SplitBody(PaletteSwatch.PilotLight, p => p.z < 0f, lamps, rest);
            var kitted = new List<Vector3>(rest);
            foreach (KitPiece piece in _pieces)
            {
                kitted.AddRange(piece.Triangles);
            }

            var kept = new List<(float, int, int)>();
            foreach (float distance in new[] { ChaseDistance, FarDistance })
            {
                SilhouetteCamera camera = Chase(distance, 0f);
                int bare = SilhouetteCamera.Count(Owners(camera, rest, lamps), 2);
                int fitted = SilhouetteCamera.Count(Owners(camera, kitted, lamps), 2);
                TestContext.WriteLine($"the tail lamps show {fitted} of {bare} px with the kit at {distance} m");
                kept.Add((distance, bare, fitted));
            }

            foreach ((float distance, int bare, int fitted) in kept)
            {
                Assert.Greater(bare, 0, $"the tail lamps should show bare at {distance} m");
                Assert.GreaterOrEqual(fitted, MinTailLampsKept * bare,
                    $"the rack hides the tail lamps at {distance} m");
            }
        }

        [Test]
        public void KittedRover_StillFitsKenjisTurntable()
        {
            float radius = BaseModelBuilder.CreateRoverBay().GetDescendant("Turntable").Mesh.Geometry.Bounds.extents.x;
            float reach = 0f;
            foreach (Vector3 p in _bare.Concat(_pieces.SelectMany(piece => piece.Triangles)))
            {
                reach = Mathf.Max(reach, new Vector2(p.x, p.z).magnitude);
            }

            Assert.GreaterOrEqual(radius - reach, TurntableRoom, "a kitted 07 still turns on the bay's turntable");
        }

        [Test]
        public void Kit_IsWellLoved_RustAtItsBoltsAndSeams()
        {
            foreach (ModelNode kit in new[]
            {
                RoverKitBuilder.CreateLampBar(), RoverKitBuilder.CreateCapacitorDrum(),
                    RoverKitBuilder.CreateCargoRack(),
            })
            {
                LowPolyMeshBuilder mesh = kit.Mesh.Geometry;
                float rust = 0f;
                float total = 0f;
                for (int t = 0; t < mesh.TriangleCount; t++)
                {
                    Vector3 a = mesh.Positions[t * 3];
                    float area = 0.5f * Vector3.Cross(mesh.Positions[t * 3 + 1] - a, mesh.Positions[t * 3 + 2] - a)
                        .magnitude;
                    total += area;
                    if (MeshChecks.SwatchOf(mesh, t) == PaletteSwatch.Rust)
                    {
                        rust += area;
                    }
                }

                Assert.Greater(rust, 0f, $"{kit.Name} shows its salvage past: rust at its bolts and seams");
                Assert.Less(rust / total, MaxRustShare, $"{kit.Name} is well-loved, not derelict");
            }
        }

        [Test]
        public void Rover_AndKit_BuildIdentically()
        {
            MeshChecks.AssertSameModel(_rover, RoverModelBuilder.CreateModel());
            MeshChecks.AssertSameModel(RoverKitBuilder.CreateLampBar(), RoverKitBuilder.CreateLampBar());
            MeshChecks.AssertSameModel(RoverKitBuilder.CreateCapacitorDrum(), RoverKitBuilder.CreateCapacitorDrum());
            MeshChecks.AssertSameModel(RoverKitBuilder.CreateCargoRack(), RoverKitBuilder.CreateCargoRack());
        }

        /// <summary>
        /// Every piece adds at least its <paramref name="budget"/> of pixels to 07's outline from its best chase view
        /// at <paramref name="distance"/> (the drums together); each view's count is logged.
        /// </summary>
        private void AssertEveryPieceAdds(float distance, Dictionary<string, int> budget)
        {
            var pieces = new Dictionary<string, List<Vector3>>
            {
                { RoverKitBuilder.LampBarName, _pieces[0].Triangles },
                { RoverKitBuilder.CapacitorDrumName, _pieces[1].Triangles.Concat(_pieces[2].Triangles).ToList() },
                { RoverKitBuilder.CargoRackName, _pieces[3].Triangles },
            };

            var best = new Dictionary<string, int>();
            foreach (KeyValuePair<string, List<Vector3>> piece in pieces)
            {
                best[piece.Key] = 0;
                foreach (float yaw in ChaseYaws)
                {
                    SilhouetteCamera camera = Chase(distance, yaw);
                    bool[] outline = camera.NewMask();
                    camera.Rasterize(_bare, outline);
                    bool[] kitted = camera.NewMask();
                    camera.Rasterize(_bare, kitted);
                    camera.Rasterize(piece.Value, kitted);
                    int added = SilhouetteCamera.Added(kitted, outline);
                    TestContext.WriteLine($"{piece.Key} adds {added} px to 07's outline at {distance} m, yaw {yaw}");
                    best[piece.Key] = Mathf.Max(best[piece.Key], added);
                }
            }

            foreach (KeyValuePair<string, int> piece in best)
            {
                Assert.GreaterOrEqual(piece.Value, budget[piece.Key],
                    $"{piece.Key} must read in silhouette at {distance} m");
            }
        }

        private static SilhouetteCamera Chase(float distance, float yaw)
        {
            return SilhouetteCamera.Orbit(ChaseTarget, distance, ChasePitch, yaw, Fov, ScreenWidth, ScreenHeight);
        }

        /// <summary>
        /// Depth-tests <paramref name="rest"/> (owner 1) and <paramref name="subject"/> (owner 2) from
        /// <paramref name="camera"/>.
        /// </summary>
        private static int[] Owners(SilhouetteCamera camera, List<Vector3> rest, List<Vector3> subject)
        {
            float[] depth = camera.NewDepth();
            int[] owners = camera.NewOwners();
            camera.RasterizeNearest(rest, depth, owners, 1);
            camera.RasterizeNearest(subject, depth, owners, 2);
            return owners;
        }

        /// <summary>
        /// The bare 07's triangles split into the faded serial painted where <paramref name="region"/> holds, and
        /// everything else but the body's weather skins (they hug the paint, so only the kit can hide it here).
        /// </summary>
        private void SplitSerial(System.Func<Vector3, bool> region, List<Vector3> serial, List<Vector3> rest)
        {
            SplitBody(PaletteSwatch.FadedPaint, region, serial, rest);
        }

        /// <summary>
        /// The bare 07's triangles split into the body's faces painted <paramref name="swatch"/> where
        /// <paramref name="region"/> holds, and everything else but the body's weather skins.
        /// </summary>
        private void SplitBody(PaletteSwatch swatch, System.Func<Vector3, bool> region, List<Vector3> picked,
            List<Vector3> rest)
        {
            ModelNode body = _rover.GetDescendant("Body");
            LowPolyMeshBuilder shell = body.Mesh.Geometry;
            for (int t = 0; t < shell.TriangleCount; t++)
            {
                bool painted = MeshChecks.SwatchOf(shell, t) == swatch && region(shell.Positions[t * 3]);
                List<Vector3> into = painted ? picked : rest;
                into.Add(shell.Positions[t * 3]);
                into.Add(shell.Positions[t * 3 + 1]);
                into.Add(shell.Positions[t * 3 + 2]);
            }

            foreach (ModelNode child in _rover.Children.Where(child => child != body))
            {
                SilhouetteCamera.CollectTriangles(child, Matrix4x4.identity, rest);
            }
        }

        private List<Vector3> Fit(ModelNode kit, string socket)
        {
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(kit, SilhouetteCamera.WorldOf(_rover, socket), triangles);
            return triangles;
        }

        /// <summary>
        /// World bounds of <paramref name="node"/>'s visible meshes placed at the rover node at path.
        /// </summary>
        private Bounds Mounted(ModelNode node, string path)
        {
            Matrix4x4 parent = SilhouetteCamera.WorldOf(_rover, path) * node.LocalMatrix.inverse;
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(node, parent, triangles);
            return Envelope(triangles);
        }

        private static Bounds Envelope(List<Vector3> points)
        {
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points)
            {
                bounds.Encapsulate(p);
            }

            return bounds;
        }

        private static Matrix4x4 Pose(Vector3 position, Vector3 euler)
        {
            return Matrix4x4.Translate(position) * Matrix4x4.Rotate(Quaternion.Euler(euler));
        }

        /// <summary>
        /// Every visible triangle under <paramref name="node"/>, the node itself placed at <paramref name="world"/>
        /// and each descendant named in <paramref name="turns"/> turned that way instead of its authored rotation.
        /// </summary>
        private static void Collect(ModelNode node, Matrix4x4 world, Dictionary<string, Quaternion> turns,
            List<Vector3> triangles)
        {
            if (!node.Active)
            {
                return;
            }

            if (node.Mesh != null)
            {
                foreach (Vector3 p in node.Mesh.Geometry.Positions)
                {
                    triangles.Add(world.MultiplyPoint3x4(p));
                }
            }

            foreach (ModelNode child in node.Children)
            {
                Quaternion turn = turns != null && turns.TryGetValue(child.Name, out Quaternion q)
                    ? q
                    : child.LocalRotation;
                Collect(child, world * Matrix4x4.Translate(child.LocalPosition) * Matrix4x4.Rotate(turn), turns,
                    triangles);
            }
        }

        /// <summary>
        /// From <paramref name="from"/> to <paramref name="to"/> in <paramref name="step"/>s, both ends included.
        /// </summary>
        private static IEnumerable<float> Steps(float from, float to, float step)
        {
            for (float value = from; value < to; value += step)
            {
                yield return value;
            }

            yield return to;
        }

        private static Bounds SwatchBounds(LowPolyMeshBuilder geometry, PaletteSwatch swatch)
        {
            var points = new List<Vector3>();
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(geometry, t) == swatch)
                {
                    points.Add(geometry.Positions[t * 3]);
                    points.Add(geometry.Positions[t * 3 + 1]);
                    points.Add(geometry.Positions[t * 3 + 2]);
                }
            }

            Assert.Greater(points.Count, 0, $"no {swatch} faces");
            return Envelope(points);
        }

        private static Bounds BoundsOf(LowPolyMeshBuilder geometry, PaletteSwatch swatch,
            System.Func<Vector3, bool> region)
        {
            Bounds? bounds = null;
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(geometry, t) != swatch || !region(geometry.Positions[t * 3]))
                {
                    continue;
                }

                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = geometry.Positions[t * 3 + k];
                    if (bounds.HasValue)
                    {
                        Bounds grown = bounds.Value;
                        grown.Encapsulate(p);
                        bounds = grown;
                    }
                    else
                    {
                        bounds = new Bounds(p, Vector3.zero);
                    }
                }
            }

            Assert.IsTrue(bounds.HasValue, "a fresh patch is missing");
            // The old numerals sit half sunk in the stripe; whatever lies inside the stripe paint is hidden anyway.
            Bounds result = bounds.Value;
            result.Expand(2f * StripePaint);
            return result;
        }

        /// <summary>
        /// Distance from <paramref name="point"/> to the nearest triangle of <paramref name="geometry"/>.
        /// </summary>
        private static float DistanceToSurface(Vector3 point, LowPolyMeshBuilder geometry)
        {
            float best = float.MaxValue;
            for (int v = 0; v < geometry.VertexCount; v += 3)
            {
                Vector3 closest = ClosestOnTriangle(point, geometry.Positions[v], geometry.Positions[v + 1],
                    geometry.Positions[v + 2]);
                best = Mathf.Min(best, Vector3.Distance(point, closest));
            }

            return best;
        }

        /// <summary>
        /// Closest point to <paramref name="p"/> on triangle abc (Ericson, Real-Time Collision Detection).
        /// </summary>
        private static Vector3 ClosestOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                return a;
            }

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                return b;
            }

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                return a + ab * (d1 / (d1 - d3));
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                return c;
            }

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                return a + ac * (d2 / (d2 - d6));
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                return b + (c - b) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
            }

            float denominator = 1f / (va + vb + vc);
            return a + ab * (vb * denominator) + ac * (vc * denominator);
        }

        private static IEnumerable<ModelNode> Nodes(ModelNode node)
        {
            yield return node;
            foreach (ModelNode child in node.Children)
            {
                foreach (ModelNode descendant in Nodes(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// One kit piece fitted to 07 (rover space), with its bounds and its triangles bucketed in a grid for
        /// nearby-distance queries.
        /// </summary>
        private sealed class KitPiece
        {
            private const float Cell = 0.05f;
            private const float GridReach = 0.06f;

            private readonly Dictionary<Vector3Int, List<int>> _cells;

            public KitPiece(string name, List<Vector3> triangles)
                : this(name, triangles, GridReach)
            {
            }

            private KitPiece(string name, List<Vector3> triangles, float reach)
            {
                Name = name;
                Triangles = triangles;
                Bounds = Envelope(triangles);
                _cells = new Dictionary<Vector3Int, List<int>>();
                var margin = new Vector3(reach, reach, reach);
                for (int t = 0; t + 2 < triangles.Count; t += 3)
                {
                    Vector3Int from = CellOf(Vector3.Min(triangles[t], Vector3.Min(triangles[t + 1],
                        triangles[t + 2])) - margin);
                    Vector3Int to = CellOf(Vector3.Max(triangles[t], Vector3.Max(triangles[t + 1],
                        triangles[t + 2])) + margin);
                    for (int x = from.x; x <= to.x; x++)
                    {
                        for (int y = from.y; y <= to.y; y++)
                        {
                            for (int z = from.z; z <= to.z; z++)
                            {
                                var key = new Vector3Int(x, y, z);
                                if (!_cells.TryGetValue(key, out List<int> bucket))
                                {
                                    bucket = new List<int>();
                                    _cells.Add(key, bucket);
                                }

                                bucket.Add(t);
                            }
                        }
                    }
                }
            }

            public string Name { get; }

            public List<Vector3> Triangles { get; }

            public Bounds Bounds { get; }

            /// <summary>
            /// The narrowest gap between this piece and <paramref name="other"/> (vertex to triangle, both ways), or
            /// <paramref name="reach"/> if they are at least that far apart (at most the grid's reach).
            /// </summary>
            public float GapTo(List<Vector3> other, float reach)
            {
                return GapTo(other, reach, out _);
            }

            /// <summary>
            /// As <see cref="GapTo(List{Vector3}, float)"/>, with the point of the pair nearest the other.
            /// </summary>
            public float GapTo(List<Vector3> other, float reach, out Vector3 where)
            {
                float best = NearestFrom(other, reach, out where);
                Bounds zone = Bounds;
                zone.Expand(2f * reach);
                var near = new List<Vector3>();
                for (int t = 0; t + 2 < other.Count; t += 3)
                {
                    var face = new Bounds(other[t], Vector3.zero);
                    face.Encapsulate(other[t + 1]);
                    face.Encapsulate(other[t + 2]);
                    if (zone.Intersects(face))
                    {
                        near.Add(other[t]);
                        near.Add(other[t + 1]);
                        near.Add(other[t + 2]);
                    }
                }

                if (near.Count > 0)
                {
                    float back = new KitPiece(Name, near, reach).NearestFrom(Triangles, reach, out Vector3 point);
                    if (back < best)
                    {
                        best = back;
                        where = point;
                    }
                }

                return best;
            }

            /// <summary>How near any of <paramref name="points"/> comes to this piece, capped at the reach.</summary>
            private float NearestFrom(List<Vector3> points, float reach, out Vector3 where)
            {
                float best = reach;
                where = Vector3.zero;
                foreach (Vector3 p in points)
                {
                    if (!_cells.TryGetValue(CellOf(p), out List<int> bucket))
                    {
                        continue;
                    }

                    foreach (int t in bucket)
                    {
                        float distance = Vector3.Distance(p, ClosestOnTriangle(p, Triangles[t], Triangles[t + 1],
                            Triangles[t + 2]));
                        if (distance < best)
                        {
                            best = distance;
                            where = p;
                        }
                    }
                }

                return best;
            }

            private static Vector3Int CellOf(Vector3 p)
            {
                return new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell),
                    Mathf.FloorToInt(p.z / Cell));
            }
        }
    }
}
