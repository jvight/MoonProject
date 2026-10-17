"""
tools/dead_code.py on synthetic projects: references, cascades, entry points, GUIDs, contract members, allowlist.
Run: python -m unittest discover -s tools/tests -t tools
"""
import json
import tempfile
import unittest
from pathlib import Path

import dead_code

GUID = "0123456789abcdef0123456789abcdef"


class Project:
    """A throwaway Assets/_Project tree: runtime assembly 'Game' in Scripts/Gameplay, tests in Scripts/Gameplay/Tests."""

    def __init__(self, root):
        self.root = Path(root)
        self.file("Scripts/Gameplay/Game.asmdef", json.dumps({"name": "Game"}))
        self.file("Scripts/Gameplay/Tests/Game.Tests.asmdef",
                  json.dumps({"name": "Game.Tests", "includePlatforms": ["Editor"],
                              "defineConstraints": ["UNITY_INCLUDE_TESTS"]}))

    def file(self, relative, text):
        path = self.root / "Assets" / "_Project" / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path

    def cs(self, relative, body, namespace="Game"):
        self.file(f"Scripts/{relative}", f"namespace {namespace}\n{{\n{body}\n}}\n")

    def findings(self, allowlist=()):
        return dead_code.analyse(self.root, allowlist)

    def ids(self, allowlist=()):
        return {f.id for f in self.findings(allowlist)}


class ProjectTestCase(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.p = Project(self._tmp.name)


class TypeTests(ProjectTestCase):
    def test_an_unreferenced_type_is_found_and_a_used_one_is_not(self):
        self.p.cs("Gameplay/Used.cs", "public class Used { }")
        self.p.cs("Gameplay/Unused.cs", "public class Unused { }")
        self.p.cs("Gameplay/User.cs", "public class User : ICallbacks { Used _used; }")
        self.assertEqual(self.p.ids(), {"Unused"})

    def test_a_type_that_only_mentions_itself_is_dead_and_a_sibling_in_its_file_keeps_it_alive(self):
        self.p.cs("Gameplay/Selfish.cs", "public class Selfish { public Selfish Next; static Selfish Make() { "
                                         "return new Selfish(); } }")
        self.p.cs("Gameplay/Pair.cs", "public struct Part { }\npublic class Whole : ICallbacks { Part _part; }")
        self.assertEqual(self.p.ids(), {"Selfish"})

    def test_a_type_used_only_by_dead_code_is_found_one_level_later(self):
        self.p.cs("Gameplay/Outer.cs", "public class Outer { Inner _inner; }")
        self.p.cs("Gameplay/Inner.cs", "public class Inner { Leaf _leaf; }")
        self.p.cs("Gameplay/Leaf.cs", "public class Leaf { }")
        levels = {f.id: f.level for f in self.p.findings()}
        self.assertEqual(levels, {"Outer": 1, "Inner": 2, "Leaf": 3})

    def test_comments_tooltips_and_namespaces_are_not_references(self):
        self.p.cs("Gameplay/Ghost.cs", "public class Ghost { }")
        self.p.cs("Gameplay/Talker.cs", '// Ghost\n/// <see cref="Ghost"/>\npublic class Talker '
                                        '{ string _tip = "a Ghost in the shell"; }', namespace="Game.Ghost")
        self.assertIn("Ghost", self.p.ids())

    def test_an_identifier_string_is_a_reference_the_way_reflection_names_a_type(self):
        self.p.cs("Gameplay/Named.cs", "public class Named { }")
        self.p.cs("Gameplay/Loader.cs", 'public class Loader : ICallbacks { string _type = "Game.Named"; }')
        self.assertEqual(self.p.ids(), set())

    def test_uxml_and_python_tools_count_as_references(self):
        self.p.cs("Gameplay/Element.cs", "public class Element { }")
        self.p.cs("Gameplay/Command.cs", "public class Command { }")
        self.p.file("UI/Screen.uxml", "<Game.Element name='x'/>")
        tool = self.p.root / "tools" / "run.py"
        tool.parent.mkdir()
        tool.write_text("METHOD = 'Game.Command.Run'\n", encoding="utf-8")
        self.assertEqual(self.p.ids(), set())

    def test_an_attribute_class_is_referenced_without_its_suffix(self):
        self.p.cs("Gameplay/Mark.cs", "public sealed class MarkAttribute : System.Attribute { }")
        self.p.cs("Gameplay/Marked.cs", "[Mark] public class Marked { }")
        self.assertEqual(self.p.ids(), {"Marked"})

    def test_types_only_tests_reference_are_marked_and_test_declarations_are_ignored(self):
        self.p.cs("Gameplay/Helper.cs", "public class Helper { }")
        self.p.cs("Gameplay/Tests/HelperTests.cs", "public class HelperTests { Helper _h; }")
        found = {f.id: f for f in self.p.findings()}
        self.assertEqual(set(found), {"Helper"})
        self.assertTrue(found["Helper"].tests_only)


class EntryPointTests(ProjectTestCase):
    def test_reflection_entry_points_are_never_findings(self):
        self.p.cs("Gameplay/Menu.cs", 'public static class Menu { [MenuItem("X")] static void Go() { } }')
        self.p.cs("Gameplay/Boot.cs", "[InitializeOnLoad] public class Boot { }")
        self.p.cs("Gameplay/Batch.cs", 'public static class Batch { public static void Run() { '
                                       'BatchRunner.Run("x", a => true); } }')
        self.p.cs("Gameplay/Contrib.cs", "public class Contrib : ISceneContributor { }")
        self.p.cs("Gameplay/Callbacks.cs", "public class Cb : ICallbacks { }")
        self.p.cs("Gameplay/Post.cs", "public class Post : AssetPostprocessor { }")
        self.p.cs("Gameplay/Builder.cs", 'public static class Builders { [MoonBuilder("A")] public static void B() { } }')
        self.assertEqual(self.p.ids(), set())

    def test_a_unity_object_is_alive_when_an_asset_holds_its_script_guid(self):
        self.p.cs("Gameplay/Wired.cs", "public class Wired : MonoBehaviour { }")
        self.p.cs("Gameplay/Loose.cs", "public class Loose : MonoBehaviour { }")
        folder = self.p.root / "Assets" / "_Project" / "Scripts" / "Gameplay"
        (folder / "Wired.cs.meta").write_text(f"guid: {GUID}\n", encoding="utf-8")
        (folder / "Loose.cs.meta").write_text(f"guid: {'f' * 32}\n", encoding="utf-8")
        self.p.file("Scenes/Main.unity", f"m_Script: {{fileID: 11500000, guid: {GUID}, type: 3}}\n")
        self.assertEqual(self.p.ids(), {"Loose"})

    def test_derived_unity_objects_follow_the_base_chain_to_the_guid_check(self):
        self.p.cs("Gameplay/Base.cs", "public abstract class Base : MonoBehaviour { }")
        self.p.cs("Gameplay/Leaf.cs", "public class Leaf : Base { }")
        self.assertEqual(self.p.ids(), {"Leaf", "Base"})


class ContractTests(ProjectTestCase):
    def setUp(self):
        super().setUp()
        self.p.file("Scripts/Core/Core.asmdef", json.dumps({"name": "Core"}))

    def test_event_members_nobody_reads_and_events_nobody_constructs(self):
        self.p.cs("Core/Events/Events.cs", "public readonly struct Hit { public Hit(int power, int unused) { "
                                          "Power = power; Unused = unused; } public int Power { get; } "
                                          "public int Unused { get; } }\n"
                                          "public readonly struct Ghost { public int Value { get; } }",
                  namespace="Core.Events")
        self.p.cs("Gameplay/Bus.cs", "public class Bus : ICallbacks { void Fire() { Publish(new Hit(1, 2)); } "
                                     "void On(Hit hit) { int p = hit.Power; } void Also(Ghost g) { } }")
        self.assertEqual(self.p.ids(), {"Hit.Unused", "Ghost.Value", "Ghost.new"})

    def test_interface_members_are_used_by_a_member_access_anywhere(self):
        self.p.cs("Core/IThing.cs", "public interface IThing { void Used(); void Idle(); int Count { get; } }")
        self.p.cs("Gameplay/Thing.cs", "public class Thing : IThing, ICallbacks { public void Used() { } "
                                       "public void Idle() { } public int Count => 1; }")
        self.p.cs("Gameplay/Caller.cs", "public class Caller : ICallbacks { IThing _t; "
                                        "void Go() { _t.Used(); int n = _t.Count; } }")
        self.assertEqual(self.p.ids(), {"IThing.Idle"})


class ReportTests(ProjectTestCase):
    def finding(self, id_, domain, allowed=False):
        return dead_code.Finding(id_, domain, "class", f"Assets/_Project/Scripts/{domain}/{id_}.cs", 7, allowed=allowed)

    def test_allowlist_patterns_match_types_members_and_wildcards(self):
        self.p.cs("Gameplay/A.cs", "public class Alpha { }\npublic class Beta { }\npublic class Gamma { }")
        found = {f.id: f.allowed for f in self.p.findings(["Alpha", "Be*"])}
        self.assertEqual(found, {"Alpha": True, "Beta": True, "Gamma": False})

    def test_report_groups_by_domain_filters_and_counts_only_open_findings(self):
        findings = [self.finding("Zed", "UI"), self.finding("Ann", "Core"), self.finding("Ben", "Core", True)]
        text, open_count = dead_code.render(findings)
        self.assertEqual(open_count, 2)
        self.assertLess(text.index("Core (1)"), text.index("UI (1)"))
        self.assertNotIn("Ben", text)
        self.assertIn("1 allowlisted", text)
        text, open_count = dead_code.render(findings, ["ui"])
        self.assertEqual((open_count, "Ann" in text), (1, False))
        self.assertIn("Ben", dead_code.render(findings, ["core"], show_allowed=True)[0])

    def test_allowlist_file_ignores_comments_and_blank_lines(self):
        path = Path(self._tmp.name, "allow.txt")
        path.write_text("# header\n\nFoo  # why\nBar.*\n", encoding="utf-8")
        self.assertEqual(dead_code.load_allowlist(path), ["Foo", "Bar.*"])
        self.assertEqual(dead_code.load_allowlist(Path(self._tmp.name, "missing.txt")), [])


if __name__ == "__main__":
    unittest.main()
