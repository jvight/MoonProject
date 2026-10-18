using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Lays the opening's old power cables from Art's R1 cable kit at scene build (opening v2 §4): 07's from its dead
    /// post along the crew tracks, through one 45-degree bend, into the lander's port with Art's plug, torn about
    /// midway (two broken ends a short gap apart, the far one kinked, a splice box beside them that was never fitted);
    /// and the port's to the build apron. Every straight's stake clip carries a dim warm halo, wired in order toward
    /// the tear from each side for <see cref="LanderBeacon"/>, with a cold spark's halo at the torn copper.
    /// </summary>
    public static class GroundCableLayer
    {
        // Art's cable kit nodes (tools/art/qa/contracts/cable_*.json).
        private const string StartAnchor = "Start_Anchor";
        private const string EndAnchor = "End_Anchor";
        private const string ClipAnchor = "Clip_Anchor";
        private const string BreakAnchor = "Break_Anchor";
        private const string PlugAnchor = "Plug_Anchor";
        private const string SpliceInput = "Input_Anchor";
        private const string SpliceOutput = "Output_Anchor";
        private const float BendTurn = 45f;

        /// <summary>The halos <see cref="LanderBeacon"/> drives.</summary>
        public sealed class Wiring
        {
            public Wiring(Renderer[] postClips, Renderer[] landerClips, Renderer spark)
            {
                PostClips = postClips;
                LanderClips = landerClips;
                Spark = spark;
            }

            /// <summary>07's side, from its post to the tear.</summary>
            public Renderer[] PostClips { get; }

            /// <summary>The lander's side, from its port to the tear.</summary>
            public Renderer[] LanderClips { get; }

            public Renderer Spark { get; }
        }

        /// <summary>07's cable from its post to the port's <paramref name="socket"/>, torn on the way.</summary>
        public static Wiring LayWakeCable(SceneBuildContext context, Transform parent, MoonSurface surface,
            GroundCableSettings tuning, Vector3 post, Vector3 socket, Mesh haloMesh, Material clipHalo,
            Material sparkHalo)
        {
            Kit kit = Kit.Load(context);
            Transform cable = context.CreateChild("WakeCable", parent).transform;
            CableCorner.Solve(Flat2(post), Flat2(socket), BendTurn, kit.Bend.Chord, kit.Plug.Reach,
                tuning.ApproachLength, out Vector2 bendStart, out Vector2 heading);

            // 07's side: the post to the tear.
            Vector2 tear = Vector2.Lerp(Flat2(post), bendStart, tuning.TearShare);
            var postClips = new List<Renderer>();
            Vector3 tearEnd = LayRun(context, cable, surface, kit, Flat2(post), tear, tuning.Wander, tuning.Bends,
                postClips, haloMesh, clipHalo);
            Vector2 runDirection = (tear - Flat2(post)).normalized;
            GameObject near = Lay(context, cable, kit.BrokenEnd, tearEnd, runDirection, surface);
            postClips.Add(Halo(context, cable, Node(near, ClipAnchor), haloMesh, clipHalo));
            Renderer spark = Halo(context, cable, Node(near, BreakAnchor), haloMesh, sparkHalo);

            // The far torn end snapped back across the gap, kinked, its copper facing 07's.
            Vector3 nearCopper = Node(near, BreakAnchor).position;
            Vector2 farCopper = Flat2(nearCopper) + runDirection * tuning.TearGap;
            Vector2 farFacing = CableCorner.Rotate(-runDirection, tuning.TearKink);
            GameObject far = LayAt(context, cable, kit.BrokenEnd, kit.BrokenEnd.Break, farCopper, farFacing, surface);
            Vector3 farStart = Node(far, StartAnchor).position;
            Vector2 side = CableCorner.Rotate(runDirection, 90f);
            Lay(context, cable, kit.Splice, Ground(surface, Flat2(nearCopper) + side * tuning.SpliceSide),
                runDirection, surface);

            // The lander's side: the far torn end, through the bend, into the port.
            var landerSide = new List<Renderer> { Halo(context, cable, Node(far, ClipAnchor), haloMesh, clipHalo) };
            Vector3 bendIn = LayRun(context, cable, surface, kit, Flat2(farStart), bendStart, tuning.Wander,
                tuning.Bends * (1f - tuning.TearShare), landerSide, haloMesh, clipHalo);
            GameObject bend = Lay(context, cable, kit.Bend, bendIn, heading, surface);
            landerSide.Add(Halo(context, cable, Node(bend, ClipAnchor), haloMesh, clipHalo));
            Vector2 into = CableCorner.Rotate(heading, BendTurn);
            Vector2 plugStart = Flat2(socket) - into * kit.Plug.Reach;
            Vector3 plugIn = LayRun(context, cable, surface, kit, Flat2(Node(bend, EndAnchor).position), plugStart, 0f,
                1f, landerSide, haloMesh, clipHalo);
            Lay(context, cable, kit.Plug, plugIn, into, surface);
            landerSide.Reverse();
            return new Wiring(postClips.ToArray(), landerSide.ToArray(), spark);
        }

        /// <summary>The port's cable from the build apron's channel into the port's apron socket.</summary>
        public static void LayApronCable(SceneBuildContext context, Transform parent, MoonSurface surface,
            GroundCableSettings tuning, Vector3 from, Vector3 socket)
        {
            Kit kit = Kit.Load(context);
            Transform cable = context.CreateChild("ApronCable", parent).transform;
            Vector2 into = (Flat2(socket) - Flat2(from)).normalized;
            Vector2 plugStart = Flat2(socket) - into * kit.Plug.Reach;
            Vector3 plugIn = LayRun(context, cable, surface, kit, Flat2(from), plugStart, tuning.Wander, tuning.Bends,
                null, null, null);
            Lay(context, cable, kit.Plug, plugIn, into, surface);
        }

        /// <summary>
        /// Straights joint to joint from <paramref name="from"/> to <paramref name="to"/>, each stretched to its span;
        /// each one's clip halo goes into <paramref name="clips"/> in order (none when it is null). Returns the last
        /// joint, on the ground.
        /// </summary>
        private static Vector3 LayRun(SceneBuildContext context, Transform parent, MoonSurface surface, Kit kit,
            Vector2 from, Vector2 to, float wander, float bends, List<Renderer> clips, Mesh haloMesh, Material halo)
        {
            Vector3[] joints = CableRoute.Joints(from, to, kit.Straight.Length, wander, bends, surface.SampleHeight);
            for (int i = 1; i < joints.Length; i++)
            {
                GameObject straight = Stretch(context, parent, kit.Straight, joints[i - 1], joints[i]);
                if (clips != null)
                {
                    clips.Add(Halo(context, parent, Node(straight, ClipAnchor), haloMesh, halo));
                }
            }

            return joints[joints.Length - 1];
        }

        /// <summary>A straight from joint <paramref name="from"/> to <paramref name="to"/>, on the slope.</summary>
        private static GameObject Stretch(SceneBuildContext context, Transform parent, Piece piece, Vector3 from,
            Vector3 to)
        {
            Vector3 span = to - from;
            Quaternion turn = Quaternion.LookRotation(span, Vector3.up)
                * Quaternion.Inverse(Quaternion.LookRotation(piece.StartFacing, Vector3.up));
            GameObject instance = context.InstantiatePrefab(piece.Prefab, parent, Vector3.zero, Quaternion.identity);
            Vector3 scale = instance.transform.localScale;
            scale[piece.LengthAxis] *= span.magnitude / piece.Length;
            instance.transform.localScale = scale;
            instance.transform.rotation = turn * piece.Prefab.transform.localRotation;
            instance.transform.position = from;
            instance.transform.position += from - Node(instance, StartAnchor).position + Vector3.up * piece.Start.y;
            return instance;
        }

        /// <summary>A piece with its start over <paramref name="start"/>, facing <paramref name="facing"/>.</summary>
        private static GameObject Lay(SceneBuildContext context, Transform parent, Piece piece, Vector3 start,
            Vector2 facing, MoonSurface surface)
        {
            return LayAt(context, parent, piece, piece.Start, Flat2(start), facing, surface);
        }

        /// <summary>
        /// A piece with its node at <paramref name="node"/> (prefab space) over <paramref name="at"/>, its start facing
        /// <paramref name="facing"/>, resting on the ground there.
        /// </summary>
        private static GameObject LayAt(SceneBuildContext context, Transform parent, Piece piece, Vector3 node,
            Vector2 at, Vector2 facing, MoonSurface surface)
        {
            Quaternion turn = Quaternion.FromToRotation(piece.StartFacing, new Vector3(facing.x, 0f, facing.y));
            turn = Quaternion.Euler(0f, turn.eulerAngles.y, 0f);
            Vector3 offset = turn * node;
            Vector3 root = Ground(surface, at - new Vector2(offset.x, offset.z));
            return context.InstantiatePrefab(piece.Prefab, parent, root, turn * piece.Prefab.transform.localRotation);
        }

        /// <summary>
        /// A halo at <paramref name="at"/>, beside the piece rather than in it, so a stretched straight never stretches
        /// its halo.
        /// </summary>
        private static Renderer Halo(SceneBuildContext context, Transform parent, Transform at, Mesh mesh,
            Material material)
        {
            GameObject halo = context.CreateChild("Halo", parent);
            halo.transform.SetPositionAndRotation(at.position, Quaternion.identity);
            halo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = halo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private static Transform Node(GameObject root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            throw new InvalidOperationException($"Art's '{root.name}' has no '{name}' node (cable kit contract).");
        }

        private static Vector3 Ground(MoonSurface surface, Vector2 at)
        {
            return new Vector3(at.x, surface.SampleHeight(at.x, at.y), at.y);
        }

        private static Vector3 Ground(MoonSurface surface, Vector3 at)
        {
            return Ground(surface, Flat2(at));
        }

        private static Vector2 Flat2(Vector3 point)
        {
            return new Vector2(point.x, point.z);
        }

        /// <summary>One of Art's cable pieces as it lies in its prefab: its nodes, start facing and length.</summary>
        private sealed class Piece
        {
            public Piece(GameObject prefab, string from, string toward)
            {
                Prefab = prefab;
                Start = Node(prefab, from).position;
                Vector3 reach = Node(prefab, toward).position - Start;
                reach.y = 0f;
                Length = reach.magnitude;
                StartFacing = reach.normalized;
                Vector3 local = Quaternion.Inverse(prefab.transform.localRotation) * StartFacing;
                LengthAxis = Mathf.Abs(local.x) > Mathf.Abs(local.y)
                    ? Mathf.Abs(local.x) > Mathf.Abs(local.z) ? 0 : 2
                    : Mathf.Abs(local.y) > Mathf.Abs(local.z) ? 1 : 2;
            }

            public GameObject Prefab { get; }

            /// <summary>The start node, prefab space.</summary>
            public Vector3 Start { get; }

            /// <summary>Flat unit direction the piece leaves its start on.</summary>
            public Vector3 StartFacing { get; private set; }

            /// <summary>Flat distance from the start to the node it was measured toward.</summary>
            public float Length { get; }

            /// <summary>The root's local axis the piece runs along (0 x, 1 y, 2 z).</summary>
            public int LengthAxis { get; }

            public float Chord => Length;

            public float Reach => Length;

            public Vector3 Break => Node(Prefab, BreakAnchor).position;

            /// <summary>
            /// A bend leaves its start <paramref name="turn"/>/2 degrees off its chord, on the side away from its
            /// curve: fails unless Art's bend turns to the right, as the route needs.
            /// </summary>
            public Piece AsBend(float turn)
            {
                Vector3 chord = StartFacing;
                Vector3 clip = Node(Prefab, ClipAnchor).position - Start;
                clip.y = 0f;
                if (Vector3.Dot(clip, Vector3.Cross(chord, Vector3.up)) <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Art's '{Prefab.name}' turns left; the wake cable's route needs a right-hand bend.");
                }

                StartFacing = Quaternion.Euler(0f, -turn * 0.5f, 0f) * chord;
                return this;
            }
        }

        /// <summary>Art's cable kit (Generated/Art/R1/Models/Props).</summary>
        private sealed class Kit
        {
            public Piece Straight { get; private set; }
            public Piece Bend { get; private set; }
            public Piece BrokenEnd { get; private set; }
            public Piece Plug { get; private set; }
            public Piece Splice { get; private set; }

            public static Kit Load(SceneBuildContext context)
            {
                return new Kit
                {
                    Straight = Load(context, WorldPaths.CableStraightPrefab, StartAnchor, EndAnchor),
                    Bend = Load(context, WorldPaths.CableBend45Prefab, StartAnchor, EndAnchor).AsBend(BendTurn),
                    BrokenEnd = Load(context, WorldPaths.CableBrokenEndPrefab, StartAnchor, BreakAnchor),
                    Plug = Load(context, WorldPaths.CablePlugPrefab, StartAnchor, PlugAnchor),
                    Splice = Load(context, WorldPaths.CableSpliceBoxPrefab, SpliceInput, SpliceOutput),
                };
            }

            private static Piece Load(SceneBuildContext context, string path, string from, string toward)
            {
                return new Piece(context.LoadAsset<GameObject>(path), from, toward);
            }
        }
    }
}
