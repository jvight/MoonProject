using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// R1-KITS contract and fit verification for 07's 10 upgrade kits + Kit_TapeDeck + SolarWing on the R1 RoverModel.
    /// Verifies source FBXs, generated prefabs, contract hierarchies, R1 material slot assignments, <= 3,000 triangle
    /// budgets, socket placements/orientations, mounted bounds, clearance against wheels/head/solar-wing, and
    /// turntable reach.
    /// </summary>
    public sealed class ArtRoverKitTests
    {
        private const string FbxFolder = "Assets/_Project/Art/R1/Models/Rover/";
        private const string PrefabFolder = "Assets/_Project/Generated/Art/R1/Models/Rover/";
        private const float TurntableRoom = 0.25f;

        private static readonly string[] TenKitNames =
        {
            RoverModelBuilder.LampBarName,
            RoverModelBuilder.CapacitorDrumName,
            RoverModelBuilder.BatterySpineName,
            RoverModelBuilder.TetherRingName,
            RoverModelBuilder.CargoRackName,
            RoverModelBuilder.SideBinName,
            RoverModelBuilder.HoverCoilsName,
            RoverModelBuilder.FocusLensName,
            RoverModelBuilder.LensGlassName,
            RoverModelBuilder.ScanFanName,
        };

        private GameObject _roverPrefab;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _roverPrefab = LoadPrefab(RoverModelBuilder.ModelName);
        }

        [Test]
        public void AllTenKits_HaveSourceFbx_GeneratedPrefab_AndFitR1TriangleBudget()
        {
            foreach (string kitName in TenKitNames.Concat(new[] { RoverModelBuilder.TapeDeckName, "SolarWing" }))
            {
                string fbxPath = FbxFolder + kitName + ".fbx";
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                Assert.IsNotNull(fbx, $"Missing source FBX at '{fbxPath}'.");

                GameObject prefab = LoadPrefab(kitName);
                Assert.AreEqual(Vector3.zero, prefab.transform.localPosition, $"{kitName} root localPosition");
                Assert.AreEqual(Quaternion.identity, prefab.transform.localRotation, $"{kitName} root localRotation");
                Assert.AreEqual(Vector3.one, prefab.transform.localScale, $"{kitName} root localScale");

                int tris = CountTriangles(prefab);
                Assert.That(tris, Is.InRange(500, 3000), $"{kitName} triangle count ({tris}) must be in [500, 3000].");

                foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Assert.IsNotEmpty(renderer.sharedMaterials, $"{kitName}/{renderer.name} has materials");
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        Assert.IsNotNull(mat, $"{kitName}/{renderer.name} has null material slot");
                        StringAssert.StartsWith("R1_", mat.name, $"{kitName}/{renderer.name} uses R1 material library");
                    }
                }
            }
        }

        [Test]
        public void Kit_HasTheContractNodes_AndGlowMaterials()
        {
            string lampMat = R1MaterialRecipes.Get(R1MaterialSlot.LampAmber).MaterialAssetName;

            GameObject bar = LoadPrefab(RoverModelBuilder.LampBarName);
            AssertHasMesh(bar.transform, RoverModelBuilder.LampBarName);
            for (int i = 0; i < RoverModelBuilder.LampCount; i++)
            {
                Transform lamp = bar.transform.Find(RoverModelBuilder.LampPrefix + i);
                AssertHasMesh(lamp, $"Kit_LampBar/Lamp_{i}");
                AssertUsesSingleMaterial(lamp, lampMat);
            }

            GameObject drum = LoadPrefab(RoverModelBuilder.CapacitorDrumName);
            AssertHasMesh(drum.transform, RoverModelBuilder.CapacitorDrumName);
            Transform drumGlow = drum.transform.Find(RoverModelBuilder.DrumGlowName);
            AssertHasMesh(drumGlow, "Kit_CapacitorDrum/Glow");
            AssertUsesSingleMaterial(drumGlow, lampMat);

            GameObject spine = LoadPrefab(RoverModelBuilder.BatterySpineName);
            AssertHasMesh(spine.transform, RoverModelBuilder.BatterySpineName);

            GameObject ring = LoadPrefab(RoverModelBuilder.TetherRingName);
            AssertHasMesh(ring.transform, RoverModelBuilder.TetherRingName);
            Transform ringGlow = ring.transform.Find("Glow");
            AssertHasMesh(ringGlow, "Kit_TetherRing/Glow");
            AssertUsesSingleMaterial(ringGlow, lampMat);

            GameObject rack = LoadPrefab(RoverModelBuilder.CargoRackName);
            AssertHasMesh(rack.transform, RoverModelBuilder.CargoRackName);
            Transform seat = rack.transform.Find(RoverModelBuilder.RelicSeatName);
            Assert.IsNotNull(seat, "Kit_CargoRack/RelicSeat");
            Assert.IsNull(seat.GetComponent<Renderer>(), "RelicSeat must be an empty transform");
            Assert.AreEqual(Quaternion.identity, seat.localRotation, "RelicSeat +Y is up");
            Assert.Less(seat.localPosition.z, 0f, "RelicSeat rides behind CargoSocket");

            GameObject bin = LoadPrefab(RoverModelBuilder.SideBinName);
            AssertHasMesh(bin.transform, RoverModelBuilder.SideBinName);

            GameObject coils = LoadPrefab(RoverModelBuilder.HoverCoilsName);
            AssertHasMesh(coils.transform, RoverModelBuilder.HoverCoilsName);
            foreach (string corner in new[] { "FL", "FR", "RL", "RR" })
            {
                Transform coil = coils.transform.Find("Coil_" + corner);
                AssertHasMesh(coil, $"HoverCoils/Coil_{corner}");
                Assert.LessOrEqual(coil.GetComponent<MeshFilter>().sharedMesh.bounds.max.y, 1e-3f,
                    $"Coil_{corner} hangs below its pivot");
                Transform glow = coil.Find("Glow_" + corner);
                AssertHasMesh(glow, $"HoverCoils/Coil_{corner}/Glow_{corner}");
                AssertUsesSingleMaterial(glow, lampMat);
            }

            GameObject lens = LoadPrefab(RoverModelBuilder.FocusLensName);
            AssertHasMesh(lens.transform, RoverModelBuilder.FocusLensName);
            Transform lensGlass = lens.transform.Find(RoverModelBuilder.FocusLensGlassName);
            AssertHasMesh(lensGlass, "FocusLens/Glass");
            AssertUsesSingleMaterial(lensGlass, lampMat);

            GameObject eyeGlass = LoadPrefab(RoverModelBuilder.LensGlassName);
            AssertHasMesh(eyeGlass.transform, RoverModelBuilder.LensGlassName);

            GameObject fan = LoadPrefab(RoverModelBuilder.ScanFanName);
            AssertHasMesh(fan.transform, RoverModelBuilder.ScanFanName);
            CollectionAssert.Contains(
                fan.GetComponent<MeshRenderer>().sharedMaterials.Select(m => m != null ? m.name : string.Empty),
                lampMat,
                "ScanFan must include R1_LampAmber material slot.");

            GameObject deck = LoadPrefab(RoverModelBuilder.TapeDeckName);
            AssertHasMesh(deck.transform, RoverModelBuilder.TapeDeckName);
            foreach (string node in new[] { "CassetteDoor", "Reel_L", "Reel_R", "Key_Play", "Key_Stop", "Key_FF", "RunLED" })
            {
                AssertHasMesh(deck.transform.Find(node), $"Kit_TapeDeck/{node}");
            }

            AssertUsesSingleMaterial(deck.transform.Find("RunLED"), lampMat);
        }

        [Test]
        public void Sockets_SitOnTheFlanksAndBody_WithContractPositions()
        {
            Transform root = _roverPrefab.transform;
            foreach (string prefix in new[] { "DrumSocket_", "BinSocket_" })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    string name = prefix + (side < 0 ? "L" : "R");
                    Transform socket = root.Find(name);
                    Assert.IsNotNull(socket, name);
                    Assert.IsNull(socket.GetComponent<Renderer>(), name);
                    Assert.AreEqual(side, Mathf.Sign(socket.localPosition.x), name);
                }
            }

            foreach (string path in new[]
            {
                "HeadlampSocket", "SpineSocket", "CargoSocket", "CoilSocket",
                "Neck/NeckLift/Head/DeckSocket", "Neck/NeckLift/Head/RingSocket",
                "Neck/NeckLift/Head/Eye/LensSocket", "Neck/NeckLift/Head/Eye/ScanFanSocket",
            })
            {
                Transform socket = root.Find(path);
                Assert.IsNotNull(socket, path);
                Assert.IsNull(socket.GetComponent<Renderer>(), path);
            }
        }

        [Test]
        public void Kits_MountOnTheirSockets_WithExpectedSilhouettesAndClearance()
        {
            Transform root = _roverPrefab.transform;
            Bounds body = NodeBoundsInRoverSpace(root.Find("Body"));
            Bounds screen = NodeBoundsInRoverSpace(root.Find("Neck/NeckLift/Head/Screen"));
            Bounds wing = NodeBoundsInRoverSpace(root.Find("SolarWing"));

            Bounds lampBar = MountedBounds(RoverModelBuilder.LampBarName, "HeadlampSocket");
            Assert.Greater(lampBar.max.x, screen.max.x + 0.08f, "Kit_LampBar extends wider than the CRT head right edge");
            Assert.Less(lampBar.min.x, screen.min.x - 0.08f, "Kit_LampBar extends wider than the CRT head left edge");
            Assert.Less(lampBar.max.y, screen.min.y, "Kit_LampBar stays below the CRT face screen");

            Bounds drumL = MountedBounds(RoverModelBuilder.CapacitorDrumName, "DrumSocket_L");
            Bounds drumR = MountedBounds(RoverModelBuilder.CapacitorDrumName, "DrumSocket_R");
            Assert.Less(drumL.min.x, body.min.x - 0.10f, "Left capacitor drum stands out past left flank");
            Assert.Greater(drumR.max.x, body.max.x + 0.10f, "Right capacitor drum stands out past right flank");
            Assert.IsFalse(drumL.Intersects(wing), "Left drum clears folded SolarWing");
            Assert.IsFalse(drumR.Intersects(wing), "Right drum clears folded SolarWing");

            Bounds binL = MountedBounds(RoverModelBuilder.SideBinName, "BinSocket_L");
            Bounds binR = MountedBounds(RoverModelBuilder.SideBinName, "BinSocket_R");
            Assert.Less(binL.min.x, body.min.x - 0.10f, "Left side bin stands out past left flank");
            Assert.Greater(binR.max.x, body.max.x + 0.10f, "Right side bin stands out past right flank");
            Assert.IsFalse(binL.Intersects(drumL), "Left side bin and left drum do not intersect");
            Assert.IsFalse(binR.Intersects(drumR), "Right side bin and right drum do not intersect");

            Bounds spine = MountedBounds(RoverModelBuilder.BatterySpineName, "SpineSocket");
            Assert.Greater(spine.max.y, body.max.y + 0.02f, "Kit_BatterySpine rises above the rear right deck");
            Assert.IsFalse(spine.Intersects(wing), "Kit_BatterySpine clears folded SolarWing");

            Bounds rack = MountedBounds(RoverModelBuilder.CargoRackName, "CargoSocket");
            Assert.Less(rack.min.z, body.min.z - 0.20f, "Kit_CargoRack extends well behind the rear chassis");
            Assert.Greater(rack.size.x, 0.65f, "Kit_CargoRack spans the rear cradle width");

            foreach (string wheelName in new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" })
            {
                Bounds wheel = NodeBoundsInRoverSpace(root.Find(wheelName));
                Assert.IsFalse(wheel.Intersects(drumL), $"{wheelName} clears DrumSocket_L");
                Assert.IsFalse(wheel.Intersects(drumR), $"{wheelName} clears DrumSocket_R");
                Assert.IsFalse(wheel.Intersects(binL), $"{wheelName} clears BinSocket_L");
                Assert.IsFalse(wheel.Intersects(binR), $"{wheelName} clears BinSocket_R");
                Assert.IsFalse(wheel.Intersects(rack), $"{wheelName} clears Kit_CargoRack");
            }
        }

        [Test]
        public void KittedRover_StillFitsKenjisTurntable()
        {
            float radius = BaseModelBuilder.CreateRoverBay().GetDescendant("Turntable").Mesh.Geometry.Bounds.extents.x;
            float reach = MaxHorizontalReach(_roverPrefab, Matrix4x4.identity);

            (string kit, string socket)[] mounts =
            {
                (RoverModelBuilder.LampBarName, "HeadlampSocket"),
                (RoverModelBuilder.CapacitorDrumName, "DrumSocket_L"),
                (RoverModelBuilder.CapacitorDrumName, "DrumSocket_R"),
                (RoverModelBuilder.BatterySpineName, "SpineSocket"),
                (RoverModelBuilder.TetherRingName, "Neck/NeckLift/Head/RingSocket"),
                (RoverModelBuilder.CargoRackName, "CargoSocket"),
                (RoverModelBuilder.SideBinName, "BinSocket_L"),
                (RoverModelBuilder.SideBinName, "BinSocket_R"),
                (RoverModelBuilder.HoverCoilsName, "CoilSocket"),
                (RoverModelBuilder.FocusLensName, "Neck/NeckLift/Head/Eye/LensSocket"),
                (RoverModelBuilder.LensGlassName, "Neck/NeckLift/Head/Eye"),
                (RoverModelBuilder.TapeDeckName, "Neck/NeckLift/Head/DeckSocket"),
            };

            foreach ((string kit, string socketPath) in mounts)
            {
                Matrix4x4 socketToRover = SocketToRoverMatrix(socketPath);
                reach = Mathf.Max(reach, MaxHorizontalReach(LoadPrefab(kit), socketToRover));
            }

            Assert.GreaterOrEqual(radius - reach, TurntableRoom, "a kitted R1 07 still turns on the bay's turntable");
        }

        private Matrix4x4 SocketToRoverMatrix(string socketPath)
        {
            Transform socket = _roverPrefab.transform.Find(socketPath);
            Assert.IsNotNull(socket, socketPath);
            Matrix4x4 socketToRover = _roverPrefab.transform.worldToLocalMatrix * socket.localToWorldMatrix;
            if (socketPath == "DrumSocket_L" || socketPath == "BinSocket_L" || socketPath.EndsWith("RingSocket"))
            {
                socketToRover *= Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));
            }

            return socketToRover;
        }

        private static GameObject LoadPrefab(string name)
        {
            return MeshChecks.LoadGeneratedPrefab(PrefabFolder + name + ".prefab");
        }

        private static void AssertHasMesh(Transform node, string label)
        {
            Assert.IsNotNull(node, $"Missing node '{label}'.");
            Assert.IsNotNull(node.GetComponent<MeshRenderer>(), $"Node '{label}' must have a MeshRenderer.");
            Assert.IsNotNull(node.GetComponent<MeshFilter>()?.sharedMesh, $"Node '{label}' must have a sharedMesh.");
            Assert.Greater(node.GetComponent<MeshFilter>().sharedMesh.triangles.Length, 0, $"Node '{label}' has triangles.");
        }

        private static void AssertUsesSingleMaterial(Transform node, string expectedMaterialName)
        {
            var renderer = node.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, node.name);
            Assert.AreEqual(1, renderer.sharedMaterials.Length, $"{node.name} must have a single material slot.");
            Assert.AreEqual(expectedMaterialName, renderer.sharedMaterial.name, $"{node.name} material");
        }

        private static int CountTriangles(GameObject root)
        {
            int total = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    total += filter.sharedMesh.triangles.Length / 3;
                }
            }

            return total;
        }

        private Bounds NodeBoundsInRoverSpace(Transform node)
        {
            Assert.IsNotNull(node);
            Mesh mesh = node.GetComponent<MeshFilter>()?.sharedMesh;
            Assert.IsNotNull(mesh, node.name);
            Matrix4x4 toRover = _roverPrefab.transform.worldToLocalMatrix * node.localToWorldMatrix;
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = -min;
            foreach (Vector3 v in mesh.vertices)
            {
                Vector3 p = toRover.MultiplyPoint3x4(v);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        private Bounds MountedBounds(string kitName, string socketPath)
        {
            return MeshChecks.PrefabBounds(LoadPrefab(kitName), SocketToRoverMatrix(socketPath));
        }

        private static float MaxHorizontalReach(GameObject root, Matrix4x4 rootToRover)
        {
            return MeshChecks.PrefabPoints(root, rootToRover).Max(p => new Vector2(p.x, p.z).magnitude);
        }
    }
}
