using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// The salvage site contract (M3-13): the nodes gameplay detaches and aims at, pieces that fly off without seams,
    /// footprints and a clear way in, a sheltered heart, Kestrel-3 tall enough to read from the base even picked
    /// clean, the colour language that tells what is left (violet optics, copper wiring, weathered metal), the
    /// bundles and the trail's loose bits.
    /// </summary>
    public sealed class SiteModelTests
    {
        // Site footprint radii (World, docs/ARCHITECTURE.md).
        private static readonly Dictionary<string, float> Footprints = new Dictionary<string, float>
        {
            { "depot", 10f }, { "kestrel", 8f }, { "drill", 9f }, { "garage", 10f }, { "lander", 9f },
        };

        // The approach lane: 3 m wide on the -Z side, from the footprint edge to a metre short of the anchor.
        private const float LaneHalfWidth = 1.5f;
        private const float LaneEnd = -1f;
        private const float LaneHeight = 0.4f;

        private const float KestrelLandmark = 6f;
        private const int MinPieces = 4;
        private const int MaxPieces = 8;
        private const float WeldGrid = 1e4f;
        private const float PivotSlack = 0.05f;
        private const float CutReach = 0.3f;
        private const float HeartCover = 0.25f;
        private const float HeartMaxHeight = 1.5f;
        private const int SiteTriangleBudget = 9000;
        private const int PieceTriangleBudget = 1200;

        private const float MinBundle = 0.3f;
        private const float MaxBundle = 0.4f;
        private const int MinDebris = 4;
        private const int MaxDebris = 6;
        private const float MinDebrisSize = 0.3f;
        private const float MaxDebrisSize = 1.2f;
        private const float PivotTolerance = 1e-3f;

        private static readonly Regex PieceName = new Regex(@"^Salvage_(\d+)_(Metal|Wiring|Optics)(_Drag)?$");
        private static readonly Regex DebrisName = new Regex(@"^Debris_(Metal|Wiring|Optics)_\d+$");

        private static IEnumerable<string> Ids => SiteModelBuilder.Ids;

        [Test]
        public void Ids_AreTheFiveBasinSites()
        {
            CollectionAssert.AreEqual(new[] { "depot", "kestrel", "drill", "garage", "lander" }, SiteModelBuilder.Ids);
        }

        [TestCaseSource(nameof(Ids))]
        public void Site_HasTheContractNodes(string id)
        {
            ModelNode site = SiteModelBuilder.CreateSite(id);
            Assert.AreEqual("Site_" + id, site.Name);
            Assert.IsNull(site.Mesh, "the root is an empty on the ground at the anchor");
            Assert.AreEqual(Vector3.zero, site.LocalPosition);

            ModelNode skeleton = site.Children.Single(node => node.Name == "Skeleton");
            Assert.IsNotNull(skeleton.Mesh);
            Assert.AreEqual(Vector3.zero, skeleton.LocalPosition);
            Assert.AreEqual(Quaternion.identity, skeleton.LocalRotation);
            CollectionAssert.IsEmpty(skeleton.Children);

            ModelNode heart = site.Children.Single(node => node.Name == "Heart");
            Assert.IsNull(heart.Mesh, "Heart is an empty");
            CollectionAssert.IsEmpty(heart.Children);

            List<ModelNode> pieces = Pieces(site);
            Assert.AreEqual(site.Children.Count - 2, pieces.Count, "only Skeleton, Salvage_* and Heart");
            Assert.That(pieces.Count, Is.InRange(MinPieces, MaxPieces));
            Assert.LessOrEqual(pieces.Count(IsDrag), 1, "at most one drag piece");
            for (int i = 0; i < pieces.Count; i++)
            {
                Match name = PieceName.Match(pieces[i].Name);
                Assert.AreEqual(i, int.Parse(name.Groups[1].Value), "pieces are numbered in order from 0");
                Assert.IsNotNull(pieces[i].Mesh, pieces[i].Name);
                Assert.AreEqual(Quaternion.identity, pieces[i].LocalRotation, pieces[i].Name);
                ModelNode cut = pieces[i].Children.Single();
                Assert.AreEqual("CutPoint", cut.Name, pieces[i].Name);
                Assert.IsNull(cut.Mesh, $"{pieces[i].Name}'s CutPoint is an empty");
                MeshChecks.AssertWellFormed(pieces[i].Mesh.Geometry);
            }

            MeshChecks.AssertWellFormed(skeleton.Mesh.Geometry);
        }

        [TestCaseSource(nameof(Ids))]
        public void Pieces_AreSelfContained_PivotedAtTheirAttachPoints_CutWhereTheyHold(string id)
        {
            ModelNode site = SiteModelBuilder.CreateSite(id);
            var owners = new Dictionary<Vector3Int, string>();
            foreach (ModelNode node in site.Children.Where(node => node.Mesh != null))
            {
                foreach (Vector3 point in MeshChecks.Points(node, Matrix4x4.identity))
                {
                    Vector3Int key = Weld(point);
                    if (owners.TryGetValue(key, out string owner) && owner != node.Name)
                    {
                        Assert.Fail($"{node.Name} shares a vertex with {owner} at {point}: it would tear a seam");
                    }

                    owners[key] = node.Name;
                }
            }

            foreach (ModelNode piece in Pieces(site))
            {
                Bounds bounds = piece.Mesh.Geometry.Bounds;
                Bounds slack = bounds;
                slack.Expand(PivotSlack * 2f);
                Assert.IsTrue(slack.Contains(Vector3.zero), $"{piece.Name}'s pivot is not on the piece ({bounds})");
                Bounds reach = bounds;
                reach.Expand(CutReach * 2f);
                ModelNode cut = piece.GetDescendant("CutPoint");
                Assert.IsTrue(reach.Contains(cut.LocalPosition), $"{piece.Name}'s CutPoint is off the piece");
                Assert.AreEqual(1f, (cut.LocalRotation * Vector3.forward).magnitude, 1e-4f);
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void Site_StaysInItsFootprint_AndKeepsTheWayInClear(string id)
        {
            ModelNode site = SiteModelBuilder.CreateSite(id);
            float footprint = Footprints[id];
            foreach (Vector3 point in MeshChecks.Points(site, Matrix4x4.identity))
            {
                Assert.LessOrEqual(new Vector2(point.x, point.z).magnitude, footprint, $"{point} is off the footprint");
                bool inLane = Mathf.Abs(point.x) <= LaneHalfWidth && point.z >= -footprint && point.z <= LaneEnd;
                Assert.IsFalse(inLane && point.y > LaneHeight, $"{point} stands in the approach lane");
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void Heart_RestsInASheltered_SpotInsideTheWreck(string id)
        {
            ModelNode site = SiteModelBuilder.CreateSite(id);
            Vector3 heart = site.GetDescendant("Heart").LocalPosition;
            Assert.That(heart.y, Is.InRange(0f, HeartMaxHeight), "the relic rests on or near the ground");
            Assert.Less(new Vector2(heart.x, heart.z).magnitude, Footprints[id]);

            LowPolyMeshBuilder skeleton = site.GetDescendant("Skeleton").Mesh.Geometry;
            bool covered = false;
            for (int v = 0; v < skeleton.VertexCount && !covered; v += 3)
            {
                covered = HeightAbove(skeleton.Positions[v], skeleton.Positions[v + 1], skeleton.Positions[v + 2],
                    heart, out float height) && height > heart.y + HeartCover;
            }

            Assert.IsTrue(covered, $"nothing of the wreck shelters the heart at {heart}");
        }

        [Test]
        public void Kestrel_StandsSixMetresTall_EvenPickedClean()
        {
            ModelNode site = SiteModelBuilder.CreateSite("kestrel");
            float top = site.GetDescendant("Skeleton").Mesh.Geometry.Bounds.max.y;
            Assert.GreaterOrEqual(top, KestrelLandmark, "the base must see Kestrel-3 at 185 m");
        }

        [TestCaseSource(nameof(Ids))]
        public void Skeleton_IsWeathered_AndPiecesShowWhatTheyYield(string id)
        {
            ModelNode site = SiteModelBuilder.CreateSite(id);
            List<PaletteSwatch> skeleton = MeshChecks.Swatches(site.GetDescendant("Skeleton").Mesh.Geometry);
            CollectionAssert.Contains(skeleton, PaletteSwatch.Rust, "rust patches and streaks");
            CollectionAssert.Contains(skeleton, PaletteSwatch.CakedDust, "dust caps and drifts");
            CollectionAssert.DoesNotContain(skeleton, PaletteSwatch.SkyHorizon, "violet glass marks optics to take");
            CollectionAssert.DoesNotContain(skeleton, PaletteSwatch.WarmAccent, "copper marks wiring to take");
            Assert.IsFalse(skeleton.Any(Palette.IsEmissive), "a wreck does not glow");

            foreach (ModelNode piece in Pieces(site))
            {
                List<PaletteSwatch> swatches = MeshChecks.Swatches(piece.Mesh.Geometry);
                Assert.IsFalse(swatches.Any(Palette.IsEmissive), $"{piece.Name} must not glow");
                string material = PieceName.Match(piece.Name).Groups[2].Value;
                Assert.AreEqual(material == "Optics", swatches.Contains(PaletteSwatch.SkyHorizon),
                    $"{piece.Name}: violet glass shows optics, and only optics");
                Assert.AreEqual(material == "Wiring", swatches.Contains(PaletteSwatch.WarmAccent),
                    $"{piece.Name}: copper shows wiring, and only wiring");
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void Site_FitsItsTriangleBudget(string id)
        {
            ModelNode site = SiteModelBuilder.CreateSite(id);
            int total = 0;
            foreach (ModelNode node in site.Children.Where(node => node.Mesh != null))
            {
                int triangles = node.Mesh.Geometry.TriangleCount;
                total += triangles;
                if (node.Name != "Skeleton")
                {
                    Assert.LessOrEqual(triangles, PieceTriangleBudget, node.Name);
                }
            }

            Assert.LessOrEqual(total, SiteTriangleBudget);
        }

        [TestCaseSource(nameof(Ids))]
        public void Site_BuildsIdentically(string id)
        {
            MeshChecks.AssertSameModel(SiteModelBuilder.CreateSite(id), SiteModelBuilder.CreateSite(id));
        }

        [TestCase(SalvageMaterial.Metal)]
        [TestCase(SalvageMaterial.Wiring)]
        [TestCase(SalvageMaterial.Optics)]
        public void Bundle_IsAPickupSizedSingleMesh_PivotedAtItsCentreOfMass(SalvageMaterial material)
        {
            ModelNode bundle = SiteModelBuilder.CreateBundle(material);
            Assert.AreEqual("Material_" + material, bundle.Name);
            CollectionAssert.IsEmpty(bundle.Children);
            LowPolyMeshBuilder geometry = bundle.Mesh.Geometry;
            MeshChecks.AssertWellFormed(geometry);
            Vector3 size = geometry.Bounds.size;
            Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(MinBundle, MaxBundle));
            Assert.That(geometry.VolumeCentroid().magnitude, Is.LessThan(PivotTolerance));
            Assert.IsFalse(MeshChecks.Swatches(geometry).Any(Palette.IsEmissive));
            MeshChecks.AssertSameModel(bundle, SiteModelBuilder.CreateBundle(material));
        }

        [Test]
        public void Bundles_ReadApart_ByColourAndSilhouette()
        {
            LowPolyMeshBuilder metal = SiteModelBuilder.CreateBundle(SalvageMaterial.Metal).Mesh.Geometry;
            LowPolyMeshBuilder wiring = SiteModelBuilder.CreateBundle(SalvageMaterial.Wiring).Mesh.Geometry;
            LowPolyMeshBuilder optics = SiteModelBuilder.CreateBundle(SalvageMaterial.Optics).Mesh.Geometry;
            Assert.AreEqual(PaletteSwatch.WarmAccent, Dominant(wiring), "copper coil");
            Assert.AreEqual(PaletteSwatch.SkyHorizon, Dominant(optics, Vector3.up), "violet cells seen from above");
            CollectionAssert.DoesNotContain(MeshChecks.Swatches(metal), PaletteSwatch.WarmAccent);
            CollectionAssert.DoesNotContain(MeshChecks.Swatches(metal), PaletteSwatch.SkyHorizon);

            // A flat plate stack, a round coil and a fan wider than it is deep.
            Assert.Less(metal.Bounds.size.y / metal.Bounds.size.x, 0.5f);
            Assert.AreEqual(1f, wiring.Bounds.size.x / wiring.Bounds.size.z, 0.2f);
            Assert.Greater(optics.Bounds.size.x / optics.Bounds.size.z, 1.25f);
        }

        [Test]
        public void Debris_AreSmallLooseBits_PivotedAtTheirCentreOfMass()
        {
            IReadOnlyList<string> names = SiteModelBuilder.DebrisNames;
            Assert.That(names.Count, Is.InRange(MinDebris, MaxDebris));
            foreach (string name in names)
            {
                Match match = DebrisName.Match(name);
                Assert.IsTrue(match.Success, name);
                ModelNode debris = SiteModelBuilder.CreateDebris(name);
                Assert.AreEqual(name, debris.Name);
                CollectionAssert.IsEmpty(debris.Children);
                LowPolyMeshBuilder geometry = debris.Mesh.Geometry;
                MeshChecks.AssertWellFormed(geometry);
                Vector3 size = geometry.Bounds.size;
                Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(MinDebrisSize, MaxDebrisSize),
                    name);
                Assert.That(geometry.VolumeCentroid().magnitude, Is.LessThan(PivotTolerance), name);
                List<PaletteSwatch> swatches = MeshChecks.Swatches(geometry);
                Assert.IsFalse(swatches.Any(Palette.IsEmissive), name);
                string material = match.Groups[1].Value;
                Assert.AreEqual(material == "Optics", swatches.Contains(PaletteSwatch.SkyHorizon), name);
                Assert.AreEqual(material == "Wiring", swatches.Contains(PaletteSwatch.WarmAccent), name);
                MeshChecks.AssertSameModel(debris, SiteModelBuilder.CreateDebris(name));
            }

            CollectionAssert.AllItemsAreUnique(names);
        }

        private static List<ModelNode> Pieces(ModelNode site)
        {
            return site.Children.Where(node => PieceName.IsMatch(node.Name)).ToList();
        }

        private static bool IsDrag(ModelNode piece)
        {
            return PieceName.Match(piece.Name).Groups[3].Success;
        }

        private static Vector3Int Weld(Vector3 point)
        {
            return new Vector3Int(Mathf.RoundToInt(point.x * WeldGrid), Mathf.RoundToInt(point.y * WeldGrid),
                Mathf.RoundToInt(point.z * WeldGrid));
        }

        /// <summary>
        /// The height of triangle abc straight above or below <paramref name="point"/>, if it spans it.
        /// </summary>
        private static bool HeightAbove(Vector3 a, Vector3 b, Vector3 c, Vector3 point, out float height)
        {
            height = 0f;
            float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(denominator) < 1e-8f)
            {
                return false;
            }

            float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / denominator;
            float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / denominator;
            float w = 1f - u - v;
            if (u < 0f || v < 0f || w < 0f)
            {
                return false;
            }

            height = u * a.y + v * b.y + w * c.y;
            return true;
        }

        /// <summary>The swatch covering the most area (facing <paramref name="view"/> when given).</summary>
        private static PaletteSwatch Dominant(LowPolyMeshBuilder geometry, Vector3 view = default)
        {
            var areas = new Dictionary<PaletteSwatch, float>();
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                Vector3 a = geometry.Positions[t * 3];
                Vector3 cross = Vector3.Cross(geometry.Positions[t * 3 + 1] - a, geometry.Positions[t * 3 + 2] - a);
                float area = view == Vector3.zero ? cross.magnitude : Mathf.Max(0f, Vector3.Dot(cross, view));
                PaletteSwatch swatch = MeshChecks.SwatchOf(geometry, t);
                areas.TryGetValue(swatch, out float sum);
                areas[swatch] = sum + area;
            }

            return areas.OrderByDescending(pair => pair.Value).First().Key;
        }
    }
}
