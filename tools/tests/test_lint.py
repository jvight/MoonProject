"""
tools/lint.py rules on small C# sources. Run: python -m unittest discover -s tools/tests -t tools
"""
import json
import tempfile
import unittest
from pathlib import Path

import lint

RUNTIME_PATH = "Assets/_Project/Scripts/World/Lighting/Probe.cs"


def findings(text, kind=lint.RUNTIME, path=RUNTIME_PATH):
    return lint.lint_sources([lint.SourceFile(path, text, kind)])


def rules_lines(text, kind=lint.RUNTIME, path=RUNTIME_PATH):
    return [(f.rule, f.line) for f in findings(text, kind, path)]


class StripTests(unittest.TestCase):
    def test_comments_strings_and_directives_are_blanked_offsets_kept(self):
        text = ('#if UNITY_EDITOR\nvar a = "GameObject.Find"; // SendMessage(\n'
                'var b = @"x ""q"" y"; /* Resources.Load\n */ var c = \'"\';\n')
        code = lint.strip_comments_and_strings(text)
        self.assertEqual(len(code), len(text))
        self.assertEqual(code.count("\n"), text.count("\n"))
        for gone in ("UNITY_EDITOR", "GameObject", "SendMessage", "Resources", "q"):
            self.assertNotIn(gone, code)
        self.assertIn("var c =", code)

    def test_interpolated_and_escaped_strings_end_where_they_should(self):
        code = lint.strip_comments_and_strings('var s = $"a\\"{x}"; WheelCollider w;')
        self.assertIn("WheelCollider w;", code)


class DebtMarkerTests(unittest.TestCase):
    def test_markers_in_comments_are_reported_with_their_line(self):
        self.assertEqual(rules_lines("class A\n{\n    // TODO: tune\n    // FIXME later\n}\n"),
                         [("debt-marker", 3), ("debt-marker", 4)])

    def test_markers_apply_to_tests_and_shaders_too(self):
        self.assertEqual(rules_lines("// HACK\n", lint.TESTS), [("debt-marker", 1)])
        self.assertEqual(rules_lines("// TODO\n", path="Assets/_Project/Shaders/World/Sky.hlsl"),
                         [("debt-marker", 1)])

    def test_words_that_merely_contain_a_marker_pass(self):
        self.assertEqual(rules_lines("class Todo { int HACKS; } // todo list\n"), [])


class FindApiTests(unittest.TestCase):
    def test_scene_searches_are_reported_in_runtime_and_editor_code(self):
        text = ("class A\n{\n    void F()\n    {\n        GameObject.Find(\"x\");\n"
                "        Object.FindObjectsByType<Light>(FindObjectsSortMode.None);\n"
                "        FindFirstObjectByType<Camera>();\n        GameObject.FindWithTag(\"t\");\n    }\n}\n")
        self.assertEqual(rules_lines(text), [("find-api", 5), ("find-api", 6), ("find-api", 7), ("find-api", 8)])
        self.assertEqual(len(findings(text, lint.EDITOR)), 4)

    def test_tests_may_inspect_the_scene(self):
        self.assertEqual(rules_lines("class T { void F() { Object.FindObjectsByType<Light>(default); } }",
                                     lint.TESTS), [])

    def test_list_find_and_comments_pass(self):
        self.assertEqual(rules_lines("class A { void F() { _items.Find(i => i.Ok); } } // GameObject.Find\n"), [])


class BannedApiTests(unittest.TestCase):
    def test_each_banned_api_is_reported(self):
        text = ("class A : MonoBehaviour\n{\n    void Update()\n    {\n        SendMessage(\"Go\");\n"
                "        var t = Resources.Load<Texture>(\"t\");\n        if (Input.GetKeyDown(KeyCode.A)) { }\n"
                "        var p = UnityEngine.Input.mousePosition;\n    }\n    void OnGUI() { }\n"
                "    WheelCollider _wheel;\n}\n")
        self.assertEqual(rules_lines(text), [("banned-api", n) for n in (5, 6, 7, 8, 10, 11)])

    def test_input_system_and_lookalikes_pass(self):
        text = ("using UnityEngine.InputSystem;\nclass A\n{\n    void F()\n    {\n"
                "        Keyboard.current.aKey.wasPressedThisFrame.ToString();\n        _input.GetKey();\n"
                "        RoverInput.GetAxis();\n        Core.Input.GetKey();\n        BroadcastMessages = 1;\n    }\n}\n")
        self.assertEqual(rules_lines(text), [])


class LinqTests(unittest.TestCase):
    def test_linq_is_reported_in_runtime_assemblies_only(self):
        text = "using System;\nusing System.Linq;\nclass A { }\n"
        self.assertEqual(rules_lines(text), [("linq-runtime", 2)])
        self.assertEqual(rules_lines(text, lint.EDITOR), [])
        self.assertEqual(rules_lines(text, lint.TESTS), [])

    def test_using_static_linq_is_reported(self):
        self.assertEqual(rules_lines("using static System.Linq.Enumerable;\n"), [("linq-runtime", 1)])


class StaticMutableTests(unittest.TestCase):
    def test_mutable_static_state_is_reported(self):
        text = ("public sealed class A\n{\n    private static int _count;\n"
                "    [SerializeField] private static float _scale = 1f, _other;\n"
                "    public static A Instance { get; private set; }\n"
                "    public static event System.Action Changed;\n"
                "    private static readonly int Id = 3;\n"
                "    private static List<int> _seen = new List<int> { 1, 2 };\n}\n")
        self.assertEqual(rules_lines(text), [("static-mutable", n) for n in (3, 4, 5, 6, 8)])

    def test_constants_readonly_methods_and_computed_properties_pass(self):
        text = ("public static class A\n{\n    public const float Speed = 2f;\n"
                "    private static readonly System.Collections.Generic.Dictionary<string, int> Table = new()\n"
                "    {\n        { \"a\", 1 },\n    };\n    public static int Twice(int x) => x * 2;\n"
                "    public static int Count => Table.Count;\n    public static int Get { get { return 1; } }\n"
                "    public static IReadOnlyList<string> Names { get; } = new[] { \"a\" };\n"
                "    static A() { }\n    public static void Run()\n    {\n        static int Local() => 1;\n"
                "        int x = 0;\n    }\n    private enum E { One, Two }\n}\n")
        self.assertEqual(rules_lines(text), [])

    def test_line_points_at_the_declaration_not_its_attributes(self):
        text = "class A\n{\n    [Tooltip(\"x\")]\n    [Range(0, 1)]\n    public static float Shared;\n}\n"
        self.assertEqual(rules_lines(text), [("static-mutable", 5)])


class InitializerTests(unittest.TestCase):
    def test_world_regression_property_block_in_a_monobehaviour_field(self):
        text = ("namespace MoonProject.World\n{\n    public sealed class HomeLighting : MonoBehaviour, IGameSystem\n"
                "    {\n        [SerializeField] private Renderer _pilot;\n\n"
                "        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();\n    }\n}\n")
        found = findings(text)
        self.assertEqual([(f.rule, f.line) for f in found], [("unity-api-in-initializer", 7)])
        self.assertIn("MaterialPropertyBlock", found[0].message)
        self.assertEqual(found[0].owner, "world")

    def test_target_typed_new_property_initializer_and_unity_calls_are_reported(self):
        text = ("class B : ScriptableObject\n{\n    private Mesh _mesh = new();\n"
                "    public Material Mat { get; } = new Material(Shader.Find(\"x\"));\n"
                "    private int _id = Shader.PropertyToID(\"_Color\");\n    private float _t = Time.time;\n"
                "    private Material _named = new Material(_shader) { name = \"x\" };\n"
                "    private string _path = Application.persistentDataPath;\n}\n")
        self.assertEqual(rules_lines(text), [("unity-api-in-initializer", n) for n in (3, 4, 5, 6, 7, 8)])

    def test_inherited_unity_base_is_followed_across_files(self):
        base = lint.SourceFile("Assets/_Project/Scripts/Rover/Base.cs",
                               "public abstract class RoverPart : UnityEngine.MonoBehaviour { }\n")
        leaf = lint.SourceFile("Assets/_Project/Scripts/Rover/Wheel.cs",
                               "public sealed class Wheel : RoverPart, IFoo\n{\n    private Mesh _m = new Mesh();\n}\n")
        self.assertEqual([(f.path, f.line) for f in lint.lint_sources([base, leaf])],
                         [("Assets/_Project/Scripts/Rover/Wheel.cs", 3)])

    def test_a_plain_class_that_builds_a_unity_object_counts_when_newed_in_a_field(self):
        text = ("public sealed class Glow\n{\n    private readonly MaterialPropertyBlock _block = "
                "new MaterialPropertyBlock();\n}\npublic sealed class Pulse\n{\n    private Mesh _m;\n"
                "    public Pulse() { _m = new Mesh(); }\n}\npublic sealed class Lamp : MonoBehaviour\n{\n"
                "    private readonly Glow _glow = new Glow();\n    private Pulse _pulse = new Pulse();\n}\n")
        found = findings(text)
        self.assertEqual([(f.rule, f.line) for f in found], [("unity-api-in-initializer", 12),
                                                             ("unity-api-in-initializer", 13)])
        self.assertIn("which creates a Unity object", found[0].message)

    def test_awake_bodies_static_fields_plain_classes_and_values_pass(self):
        text = ("public sealed class C : MonoBehaviour\n{\n    private static readonly int Id = "
                "Shader.PropertyToID(\"_X\");\n    private const int N = 3;\n"
                "    private readonly List<int> _list = new List<int>();\n    private Color _c = Color.white;\n"
                "    private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);\n"
                "    private System.Random _rng = new System.Random(4);\n"
                "    private Mesh Built => new Mesh();\n    private MaterialPropertyBlock _block;\n"
                "    private System.Func<Mesh> _make = () => new Mesh();\n"
                "    internal System.Action QuitAction { get; set; } = Application.Quit;\n"
                "    private void Awake()\n    {\n        _block = new MaterialPropertyBlock();\n"
                "        var go = new GameObject(\"x\");\n    }\n}\n"
                "public sealed class Plain\n{\n    private Mesh _mesh = new Mesh();\n}\n")
        self.assertEqual(rules_lines(text), [])


class DuplicateTypeTests(unittest.TestCase):
    def test_same_public_enum_in_two_namespaces_is_reported_at_both(self):
        core = lint.SourceFile("Assets/_Project/Scripts/Core/Events/GameplayEvents.cs",
                               "namespace MoonProject.Core.Events\n{\n    public struct LiftMoved { }\n\n"
                               "    public enum LiftCue { Start, Stop }\n}\n")
        lift = lint.SourceFile("Assets/_Project/Scripts/Gameplay/Lift/LiftCue.cs",
                               "namespace MoonProject.Gameplay.Lift;\n\npublic enum LiftCue\n{\n    Start,\n}\n")
        found = lint.lint_sources([core, lift])
        self.assertEqual([(f.rule, f.path, f.line, f.owner) for f in found],
                         [("duplicate-type", core.path, 5, "director"), ("duplicate-type", lift.path, 3, "gameplay")])
        self.assertIn("MoonProject.Gameplay.Lift", found[0].message)

    def test_partials_nested_internal_and_foreign_types_pass(self):
        a = lint.SourceFile("Assets/_Project/Scripts/UI/A.cs",
                            "namespace MoonProject.UI\n{\n    public partial class Hud { public class Settings { } }\n"
                            "    internal class Cache { }\n    public delegate void Moved(int x);\n}\n")
        b = lint.SourceFile("Assets/_Project/Scripts/UI/B.cs",
                            "namespace MoonProject.UI\n{\n    public partial class Hud { }\n}\n"
                            "namespace Vendor.Thing\n{\n    public class Hud { }\n}\n")
        c = lint.SourceFile("Assets/_Project/Scripts/World/C.cs",
                            "namespace MoonProject.World\n{\n    public class Rig { public class Settings { } }\n"
                            "    internal class Cache { }\n}\n")
        self.assertEqual(lint.lint_sources([a, b, c]), [])

    def test_delegates_count_as_types(self):
        a = lint.SourceFile("Assets/_Project/Scripts/UI/A.cs", "namespace MoonProject.UI { public delegate void "
                                                               "Moved(int x); }\n")
        b = lint.SourceFile("Assets/_Project/Scripts/Rover/B.cs", "namespace MoonProject.Rover { public class "
                                                                  "Moved { } }\n")
        self.assertEqual([f.rule for f in lint.lint_sources([a, b])], ["duplicate-type", "duplicate-type"])

    def test_the_rule_is_enforced_now_that_main_is_clean(self):
        self.assertTrue(lint.RULES["duplicate-type"].enforced)
        a = lint.SourceFile("Assets/_Project/Scripts/UI/A.cs", "namespace MoonProject.UI { public class X { } }")
        b = lint.SourceFile("Assets/_Project/Scripts/Art/B.cs", "namespace MoonProject.Art { public class X { } }")
        self.assertTrue(all(f.enforced and "(report-only)" not in f.format() for f in lint.lint_sources([a, b])))


class OwnershipAndLoadingTests(unittest.TestCase):
    def test_owner_of_follows_the_roadmap_table(self):
        cases = {"Assets/_Project/Scripts/Gameplay/Lift/A.cs": "gameplay",
                 "Assets/_Project/Scripts/Core/Events/A.cs": "director",
                 "Assets/_Project/Scripts/Editor/Automation/A.cs": "foundation",
                 "Assets/_Project/Tests/PlayMode/Harness/A.cs": "foundation",
                 "Assets/_Project/Tests/EditMode/A.cs": "director",
                 "Assets/_Project/Shaders/World/Sky.shader": "world",
                 "Assets/_Project/Shaders/LowPoly.shader": "art"}
        for path, box in cases.items():
            self.assertEqual(lint.owner_of(path), box, path)

    def test_load_sources_takes_the_kind_from_the_nearest_asmdef(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            layout = {"Scripts/World": {"name": "MoonProject.World"},
                      "Scripts/World/Editor": {"name": "MoonProject.World.Editor", "includePlatforms": ["Editor"]},
                      "Scripts/World/Tests": {"name": "MoonProject.World.Tests", "includePlatforms": ["Editor"],
                                              "defineConstraints": ["UNITY_INCLUDE_TESTS"]}}
            for folder, asmdef in layout.items():
                d = root / "Assets/_Project" / folder
                d.mkdir(parents=True)
                (d / f"{asmdef['name']}.asmdef").write_text(json.dumps(asmdef), encoding="utf-8")
                (d / "A.cs").write_text("using System.Linq;\n", encoding="utf-8")
            kinds = {s.path: s.kind for s in lint.load_sources(root)}
            self.assertEqual(kinds, {"Assets/_Project/Scripts/World/A.cs": lint.RUNTIME,
                                     "Assets/_Project/Scripts/World/Editor/A.cs": lint.EDITOR,
                                     "Assets/_Project/Scripts/World/Tests/A.cs": lint.TESTS})
            self.assertEqual([f.path for f in lint.lint_root(root)], ["Assets/_Project/Scripts/World/A.cs"])


if __name__ == "__main__":
    unittest.main()
