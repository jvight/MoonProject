using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using MoonProject.Editor.Automation;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// Concept frames for the owner's 0.6+ review, preview only (nothing is written to a prefab or the game):
    /// <c>ecology</c> (panel mites, the sweeper before and after repair, the survey flock, the trencher), <c>later</c>
    /// (cable climber, ice tapper, echo beacons), <c>elders</c> (01, 06 and 07), <c>friends</c> (Atlas, Moss) and
    /// <c>regions</c> (one mood frame per region, <see cref="ConceptRegions"/>).
    /// Each design stands on a regolith stage at night beside 07 for scale, seen front, three-quarter and side, and
    /// each group again in a row from 20 m.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Art.Editor.Concept.ConceptPreview.Capture
    ///     --arg "scene=ecology;later;elders;friends;regions"
    /// </code>
    /// PNGs land in the run's output folder under concept/.
    /// </summary>
    public static class ConceptPreview
    {
        private const float Spacing = 11f;
        private const float RowZ = -30f;
        private const float RowGap = 3.4f;
        private const float RowDistance = 20f;

        public static void Capture()
        {
            BatchRunner.Run(nameof(ConceptPreview), args =>
            {
                string[] scenes = args.GetList("scene");
                Material material = PaletteAssetBuilder.LoadMaterial();
                string output = Path.Combine(args.OutputDirectory, "concept");
                bool captured = true;
                foreach (string scene in scenes.Length > 0 ? scenes : new[] { "ecology", "later", "elders", "friends",
                    "regions" })
                {
                    if (scene == "regions")
                    {
                        foreach (string region in ConceptRegions.Names)
                        {
                            captured &= ConceptRegions.Capture(region, material, output);
                        }

                        continue;
                    }

                    captured &= CaptureScene(scene, material, output);
                }

                return captured;
            });
        }

        private static bool CaptureScene(string scene, Material material, string output)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Station[] stations;
                switch (scene)
                {
                    case "ecology":
                        stations = Ecology(material, temporary);
                        break;
                    case "later":
                        stations = Later(material, temporary);
                        break;
                    case "elders":
                        stations = Elders(material, temporary);
                        break;
                    case "friends":
                        stations = Friends(material, temporary);
                        break;
                    default:
                        Debug.LogError($"ConceptPreview: unknown scene '{scene}' (ecology|later|elders|friends).");
                        return false;
                }

                ArtLightingPreview.NightSetting(material, temporary, 120f, 80f);
                CameraPoseSet poses = Lineup(stations, material, temporary);
                return CaptureAutomation.CapturePoses(poses, output, "concept_" + scene) > 0;
            }
            finally
            {
                temporary.Dispose();
            }
        }

        /// <summary>One design on the stage: what to build, how big it reads and what it stands beside.</summary>
        private sealed class Station
        {
            public string Name { get; set; }

            /// <summary>Its widest extent (m), which sets how far the cameras stand.</summary>
            public float Size { get; set; }

            public float Height { get; set; }

            public Action<Vector3> Build { get; set; }

            /// <summary>What stands in the 20 m row, when it differs from the stage (null: the same).</summary>
            public Action<Vector3> BuildInRow { get; set; }

            public bool BesideRover { get; set; } = true;

            /// <summary>The height the cameras look at (m); 0: half its height.</summary>
            public float Focus { get; set; }

            /// <summary>
            /// Where the front camera stands across (m from its middle); null: a little to its right.
            /// </summary>
            public float? FrontX { get; set; }

            /// <summary>How close a close-up stands (m) for a design too small to read otherwise; 0: none.</summary>
            public float Close { get; set; }
        }

        private static Station[] Ecology(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            return new[]
            {
                new Station
                {
                    Name = "mites", Size = 2.4f, Height = 1.2f, Focus = 0.8f, Close = 1.1f,
                    Build = at => Mites(at, 14, material, temporary),
                    BuildInRow = at => Mites(at, 14, material, temporary),
                },
                new Station
                {
                    Name = "sweeper_before", Size = 1.4f, Height = 0.6f,
                    Build = at => Put("Sweeper", ConceptRobotMeshes.Sweeper(false), at, 20f, material, temporary),
                },
                new Station
                {
                    Name = "sweeper_after", Size = 1.6f, Height = 0.6f,
                    Build = at =>
                    {
                        Put("Sweeper", ConceptRobotMeshes.Sweeper(true), at, 0f, material, temporary);
                        Put("Swath", ConceptStageMeshes.Swath(9f), at + new Vector3(0f, 0f, -0.6f), 0f, material,
                            temporary);
                    },
                    BuildInRow = at => Put("Sweeper", ConceptRobotMeshes.Sweeper(true), at, 0f, material, temporary),
                },
                new Station
                {
                    Name = "flock", Size = 2f, Height = 2.8f, Focus = 2.1f,
                    Build = at => Flock(at, material, temporary),
                    BuildInRow = at => Flock(at, material, temporary),
                },
                new Station
                {
                    Name = "trencher", Size = 3.4f, Height = 1.3f,
                    Build = at =>
                    {
                        Put("Trencher", ConceptRobotMeshes.Trencher(), at, 0f, material, temporary);
                        Put("Trench", ConceptStageMeshes.Trench(12f), at + new Vector3(0f, 0f, -1.3f), 0f, material,
                            temporary);
                    },
                    BuildInRow = at => Put("Trencher", ConceptRobotMeshes.Trencher(), at, 0f, material, temporary),
                },
            };
        }

        private static Station[] Later(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            Action<Vector3> climber = at =>
            {
                Put("CableWall", ConceptStageMeshes.CableWall(), at, 0f, material, temporary);
                Put("Climber", ConceptRobotMeshes.CableClimber(), at, 0f, material, temporary);
            };
            Action<Vector3> tapper = at =>
            {
                Put("FrostSheet", ConceptStageMeshes.FrostSheet(), at, 0f, material, temporary);
                Put("Tapper", ConceptRobotMeshes.IceTapper(), at + new Vector3(-0.3f, 0.04f, 0f), 25f, material,
                    temporary);
            };
            Action<Vector3> beacons = at =>
            {
                Put("CaveWall", ConceptStageMeshes.CaveWall(), at, 0f, material, temporary);
                for (int i = 0; i < 5; i++)
                {
                    Put("Beacon", ConceptRobotMeshes.EchoBeacon(), at + ConceptStageMeshes.BeaconSpot(i), 0f,
                        material, temporary);
                }
            };
            return new[]
            {
                new Station
                {
                    Name = "cable_climber", Size = 3.2f, Height = 2.2f, Build = climber, BuildInRow = climber,
                },
                new Station { Name = "ice_tapper", Size = 2.8f, Height = 0.7f, Build = tapper, BuildInRow = tapper },
                new Station
                {
                    Name = "echo_beacons", Size = 4.5f, Height = 1f, Focus = 0.7f, Close = 1.4f, Build = beacons,
                    BuildInRow = beacons,
                },
            };
        }

        private static Station[] Elders(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            Action<Vector3> strip = at =>
            {
                Put("Elder01", ConceptRobotMeshes.Elder01(), at + new Vector3(1.7f, 0f, 0f), 0f, material, temporary);
                Put("Elder06", DerelictMeshes.Elder(), at, 0f, material, temporary);
                temporary.Add(ArtLightingPreview.Rover(temporary, at + new Vector3(-1.9f, 0f, 0f), 0f).gameObject);
            };
            return new[]
            {
                new Station
                {
                    Name = "evolution_01_06_07", Size = 4.2f, Height = 1f, Build = strip, BuildInRow = strip,
                    BesideRover = false, FrontX = 0f,
                },
            };
        }

        private static Station[] Friends(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            return new[]
            {
                new Station
                {
                    Name = "atlas", Size = 3.6f, Height = 3.4f,
                    Build = at => Put("Atlas", ConceptRobotMeshes.Atlas(), at, -15f, material, temporary),
                    BuildInRow = at => Put("Atlas", ConceptRobotMeshes.Atlas(), at, -15f, material, temporary),
                },
                new Station
                {
                    Name = "moss", Size = 1f, Height = 0.6f,
                    Build = at => Put("Moss", ConceptRobotMeshes.Moss(), at, 15f, material, temporary),
                    BuildInRow = at => Put("Moss", ConceptRobotMeshes.Moss(), at, 15f, material, temporary),
                },
            };
        }

        /// <summary>
        /// Stands every station along X (07 to its left, a little back, for scale) with its three views, then the
        /// group again in a row at <see cref="RowZ"/>, with 07 at its end, seen from 20 m.
        /// </summary>
        private static CameraPoseSet Lineup(Station[] stations, Material material,
            ArtLightingPreview.TemporaryObjects temporary)
        {
            var poses = new List<CameraPose>();
            float first = -(stations.Length - 1) * Spacing * 0.5f;
            for (int i = 0; i < stations.Length; i++)
            {
                Station station = stations[i];
                var at = new Vector3(first + i * Spacing, 0f, 0f);
                station.Build(at);
                if (station.BesideRover)
                {
                    Vector3 rover = at + new Vector3(station.Size * 0.5f + 1.6f, 0f, -2f);
                    temporary.Add(ArtLightingPreview.Rover(temporary, rover, -25f).gameObject);
                }

                Fill(at, station, temporary);
                float s = station.Size;
                float h = station.Height;
                float focus = station.Focus > 0f ? station.Focus : h * 0.45f;
                Vector3 look = at + Vector3.up * focus;
                float frontX = station.FrontX ?? s * 0.3f;
                poses.Add(View(station.Name + "_front", at + new Vector3(frontX, focus + 0.35f, s * 1.15f + 1.6f),
                    look));
                poses.Add(View(station.Name + "_three_quarter",
                    at + new Vector3(-(s * 0.95f + 1.4f), focus + 0.6f, s * 0.95f + 1.4f), look));
                poses.Add(View(station.Name + "_side", at + new Vector3(-(s * 1.3f + 2f), focus + 0.3f, 0.2f), look));
                if (station.Close > 0f)
                {
                    poses.Add(View(station.Name + "_close",
                        at + new Vector3(-station.Close * 0.5f, focus + station.Close * 0.6f, station.Close), look));
                }
            }

            float x = 0f;
            var row = new List<float>();
            foreach (Station station in stations)
            {
                row.Add(x + station.Size * 0.5f);
                x += station.Size + RowGap;
            }

            float centre = (x + 1.6f) * 0.5f;
            for (int i = 0; i < stations.Length; i++)
            {
                (stations[i].BuildInRow ?? stations[i].Build)(new Vector3(row[i] - centre, 0f, RowZ));
            }

            temporary.Add(ArtLightingPreview.Rover(temporary, new Vector3(x - centre + 0.6f, 0f, RowZ), 20f)
                .gameObject);
            poses.Add(View("row_20m", new Vector3(0f, 3f, RowZ + RowDistance), new Vector3(0f, 1f, RowZ)));
            return new CameraPoseSet { width = 1600, height = 900, postProcessing = true, poses = poses.ToArray() };
        }

        // A soft warm key from the front left, so the shapes read at night like the game's lamp-lit finds.
        private static void Fill(Vector3 at, Station station, ArtLightingPreview.TemporaryObjects temporary)
        {
            float reach = (station.Size + 4f) * 1.6f;
            Light key = ArtLightingPreview.NewLight("ConceptKey", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp),
                3f);
            key.transform.position = at + new Vector3(-station.Size * 0.6f - 1f, station.Height + 1.2f,
                station.Size * 0.6f + 1.5f);
            key.range = reach;
            key.shadows = LightShadows.Soft;
            temporary.Add(key.gameObject);
        }

        private static CameraPose View(string name, Vector3 eye, Vector3 look)
        {
            return ArtLightingPreview.Pose(name, ArtLightingPreview.Point(eye), ArtLightingPreview.Point(look), 42f);
        }

        private static void Put(string name, LowPolyMeshBuilder mesh, Vector3 at, float yaw, Material material,
            ArtLightingPreview.TemporaryObjects temporary)
        {
            GameObject placed = ArtLightingPreview.MeshObject(name, mesh.ToMesh("Concept_" + name), material,
                temporary);
            placed.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            temporary.Add(placed);
        }

        private static void Mites(Vector3 at, int count, Material material,
            ArtLightingPreview.TemporaryObjects temporary)
        {
            Put("Panel", ConceptStageMeshes.SolarPanel(), at, 0f, material, temporary);
            LowPolyMeshBuilder mite = ConceptRobotMeshes.PanelMite();
            for (int i = 0; i < count; i++)
            {
                Vector3 spot = ConceptStageMeshes.MiteSpot(i, count, out Quaternion turn);
                GameObject placed = ArtLightingPreview.MeshObject("Mite", mite.ToMesh("Concept_Mite"), material,
                    temporary);
                placed.transform.SetPositionAndRotation(at + spot, turn);
                temporary.Add(placed);
            }
        }

        private static void Flock(Vector3 at, Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            Vector3[] drones =
            {
                new Vector3(0f, 2.3f, 0f), new Vector3(-0.9f, 1.8f, -0.5f), new Vector3(0.8f, 2f, -0.8f),
                new Vector3(-0.3f, 2.7f, -1.4f), new Vector3(1.1f, 1.5f, 0.4f),
            };
            for (int i = 0; i < drones.Length; i++)
            {
                Put("Drone", ConceptRobotMeshes.SurveyDrone(), at + drones[i], i * 23f - 30f, material, temporary);
            }
        }
    }
}
