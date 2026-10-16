"""
tools/director_review.py: compact review sheet for a box branch against main on a tiny temp git repo.
Run: python -m unittest discover -s tools/tests -t tools
"""
import subprocess
import tempfile
import unittest
from pathlib import Path

import director_review


class DirectorReviewTests(unittest.TestCase):
    def run_git(self, root, *args):
        subprocess.run(["git", "-c", "safe.directory=*", "-c", "user.name=t", "-c", "user.email=t@t", *args],
                       cwd=root, check=True, capture_output=True)

    def make_repo(self, tmp):
        root = Path(tmp)
        self.run_git(root, "init", "-q", "-b", "main")
        rover_tuning = root / "Assets/_Project/Scripts/Rover/Tuning/RoverRigTuning.cs"
        rover_tuning.parent.mkdir(parents=True, exist_ok=True)
        rover_tuning.write_text(
            "using UnityEngine;\nnamespace MoonProject.Rover\n{\n"
            "    public sealed class RoverRigTuning : ScriptableObject\n    {\n"
            "        [SerializeField] private float _wheelRadius = 0.24f;\n"
            "        public float WheelRadius => _wheelRadius;\n    }\n}\n",
            encoding="utf-8",
        )
        cabin_tuning = root / "Assets/_Project/Scripts/Rover/Tuning/CabinTuning.cs"
        cabin_tuning.write_text(
            "using UnityEngine;\nnamespace MoonProject.Rover\n{\n"
            "    public sealed class CabinTuning : ScriptableObject\n    {\n"
            "        [SerializeField] private float _glow = 1.0f;\n"
            "        public float Glow => _glow;\n    }\n}\n",
            encoding="utf-8",
        )
        rig_asset = root / "Assets/_Project/Data/Tuning/RoverRigTuning.asset"
        rig_asset.parent.mkdir(parents=True, exist_ok=True)
        rig_asset.write_text("_wheelRadius: 0.24\n", encoding="utf-8")
        self.run_git(
            root, "add",
            "Assets/_Project/Scripts/Rover/Tuning/RoverRigTuning.cs",
            "Assets/_Project/Scripts/Rover/Tuning/CabinTuning.cs",
            "Assets/_Project/Data/Tuning/RoverRigTuning.asset",
        )
        self.run_git(root, "commit", "-q", "-m", "feat(rover): initial tuning")
        return root

    def test_review_sheet_reports_commits_domains_lint_tuning_assets_residue_and_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = self.make_repo(tmp)
            self.run_git(root, "checkout", "-q", "-b", "box/rover")

            # Commit 1: valid conventional commit changing RoverRigTuning.cs + matching RoverRigTuning.asset,
            # plus a new .inputactions and .fbx file.
            (root / "Assets/_Project/Scripts/Rover/Tuning/RoverRigTuning.cs").write_text(
                "using UnityEngine;\nnamespace MoonProject.Rover\n{\n"
                "    public sealed class RoverRigTuning : ScriptableObject\n    {\n"
                "        [SerializeField] private float _wheelRadius = 0.35f;\n"
                "        public float WheelRadius => _wheelRadius;\n    }\n}\n",
                encoding="utf-8",
            )
            (root / "Assets/_Project/Data/Tuning/RoverRigTuning.asset").write_text(
                "_wheelRadius: 0.35\n", encoding="utf-8"
            )
            actions = root / "Assets/_Project/Settings/InputActions.inputactions"
            actions.parent.mkdir(parents=True, exist_ok=True)
            actions.write_text("{}", encoding="utf-8")
            mesh = root / "Assets/_Project/Art/Rover/Chassis.fbx"
            mesh.parent.mkdir(parents=True, exist_ok=True)
            mesh.write_text("fbx", encoding="utf-8")
            self.run_git(
                root, "add",
                "Assets/_Project/Scripts/Rover/Tuning/RoverRigTuning.cs",
                "Assets/_Project/Data/Tuning/RoverRigTuning.asset",
                "Assets/_Project/Settings/InputActions.inputactions",
                "Assets/_Project/Art/Rover/Chassis.fbx",
            )
            self.run_git(root, "commit", "-q", "-m", "feat(rover): update wheel radius and input asset")

            # Commit 2: non-conventional commit message, changing CabinTuning.cs WITHOUT updating CabinTuning.asset,
            # and introducing a lint violation on a changed file.
            (root / "Assets/_Project/Scripts/Rover/Tuning/CabinTuning.cs").write_text(
                "using UnityEngine;\nnamespace MoonProject.Rover\n{\n"
                "    public sealed class CabinTuning : ScriptableObject\n    {\n"
                "        // TODO: remove placeholder\n"
                "        [SerializeField] private float _glow = 2.5f;\n"
                "        public float Glow => _glow;\n    }\n}\n",
                encoding="utf-8",
            )
            self.run_git(root, "add", "Assets/_Project/Scripts/Rover/Tuning/CabinTuning.cs")
            self.run_git(root, "commit", "-q", "-m", "quick tweak without conventional prefix")

            # Leave untracked residue and write Logs/REPORT.md with gate lines.
            residue_file = root / "Assets/InitTestScene123.unity"
            residue_file.write_text("temp", encoding="utf-8")
            report = root / "Logs/REPORT.md"
            report.parent.mkdir(parents=True, exist_ok=True)
            report.write_text(
                "# Report\n\n## Gate\n- `python tools/compile_check.py` -> RESULT: PASS\n"
                "- `python -m unittest discover -s tools/tests -t tools` -> OK\n",
                encoding="utf-8",
            )

            sheet = director_review.build_review_sheet(root, "rover")
            self.assertIn("# Director Review: box/rover (vs main)", sheet)
            self.assertIn("1 non-conventional [WARN]", sheet)
            self.assertIn("[OK]", sheet)
            self.assertIn("feat(rover): update wheel radius and input asset", sheet)
            self.assertIn("[WARN: missing Conventional type]", sheet)
            self.assertIn("quick tweak without conventional prefix", sheet)
            self.assertIn("Scripts/Rover: 2 file(s)", sheet)
            self.assertIn("Data/Tuning: 1 file(s)", sheet)
            self.assertIn("Art/Rover: 1 file(s)", sheet)
            self.assertIn("[debt-marker]", sheet)
            self.assertIn(
                "[OK] Assets/_Project/Scripts/Rover/Tuning/RoverRigTuning.cs changed (_wheelRadius); "
                "matching .asset: Assets/_Project/Data/Tuning/RoverRigTuning.asset",
                sheet,
            )
            self.assertIn(
                "[WARN] Assets/_Project/Scripts/Rover/Tuning/CabinTuning.cs changed (_glow) "
                "but no matching .asset changed",
                sheet,
            )
            self.assertIn("M Assets/_Project/Data/Tuning/RoverRigTuning.asset", sheet)
            self.assertIn("A Assets/_Project/Settings/InputActions.inputactions", sheet)
            self.assertIn("A Assets/_Project/Art/Rover/Chassis.fbx", sheet)
            self.assertIn("Assets/InitTestScene123.unity", sheet)
            self.assertIn("RESULT: PASS", sheet)


if __name__ == "__main__":
    unittest.main()
