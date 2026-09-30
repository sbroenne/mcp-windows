import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SITE = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("site_hooks", SITE / "hooks.py")
hooks = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hooks)


class DocumentationHooksTests(unittest.TestCase):
    def test_current_real_app_results_and_evidence_are_published(self):
        evidence_path = SITE / "docs" / "assets" / "benchmarks" / "real-apps.json"
        evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
        trials = evidence["trials"]
        self.assertEqual(16, len(trials))
        self.assertEqual(
            16, len({(row["app"], row["model"], row["route"]) for row in trials})
        )
        self.assertEqual(["gpt-6.1-sol", "gpt-6-luna"], evidence["manifest"]["models"])
        self.assertEqual("0.6.23", evidence["manifest"]["runtime"]["framework"])
        self.assertEqual(8, evidence["summary"]["routes"]["controls"]["successes"])
        self.assertEqual(7, evidence["summary"]["routes"]["screenshots"]["successes"])
        self.assertEqual(7, evidence["summary"]["successful_pairs"])
        self.assertTrue(all(row["evidence_complete"] for row in trials))
        serialized = json.dumps(evidence).lower()
        for private in ("c:\\\\", "stefa", "request_id", '"content"', '"prompt"', '"reply"'):
            self.assertNotIn(private, serialized)
        for path in (
            SITE.parent / "README.md",
            SITE.parent / "docs" / "real-app-benchmark.md",
            *(SITE / "docs" / name for name in ("index.md", "benchmark.md", "comparison.md")),
        ):
            with self.subTest(page=str(path)):
                text = path.read_text(encoding="utf-8")
                self.assertNotIn("11 of 16", text)
                self.assertNotIn("51.1%", text)
                self.assertIn("34.9%", text)

    def test_docs_do_not_compare_assistant_screen_control_features(self):
        paths = [
            SITE.parent / "README.md",
            SITE.parent / "FEATURES.md",
            SITE.parent / "vscode-extension" / "README.md",
            SITE.parent / ".github" / "documentation.instructions.md",
            SITE / "README.md",
            *(SITE / "docs").glob("*.md"),
            *(SITE.parent / "docs").glob("*benchmark.md"),
            SITE.parent / "docs" / "best-windows-mcp-roadmap.md",
        ]
        for path in paths:
            with self.subTest(page=str(path)):
                text = path.read_text(encoding="utf-8").lower()
                self.assertNotRegex(text, r"computer[\s-]+use")
                self.assertNotIn("assistant's built-in tools", text)

    def test_entry_pages_lead_with_the_real_task_method(self):
        for path in (SITE.parent / "README.md", SITE / "docs" / "index.md"):
            with self.subTest(page=path.name):
                page = path.read_text(encoding="utf-8")
                self.assertLess(page.index("## Real tasks"), page.index("### Read one field"))
                self.assertIn("GPT-6.1 Sol", page)
                self.assertIn("GPT-6 Luna", page)
                self.assertIn("16", page)

    def test_user_guides_do_not_embed_developer_manuals(self):
        for page in (SITE / "docs").glob("*.md"):
            if page.name != "benchmark.md":
                with self.subTest(page=page.name):
                    text = page.read_text(encoding="utf-8")
                    if page.name == "index.md":
                        for chart in ("screenshot-form", "text-updates"):
                            text = text.replace(f'--8<-- "assets/charts/{chart}.html"', "")
                    self.assertNotIn('--8<--', text)

    def test_technical_references_link_to_source_not_user_guides(self):
        sources = (
            "FEATURES.md",
            "CONTRIBUTING.md",
            "vscode-extension/CHANGELOG.md",
            "plugin/skills/windows-automation/SKILL.md",
            "plugin/skills/windows-cli/SKILL.md",
        )
        for source in sources:
            with self.subTest(source=source):
                self.assertEqual(
                    f"[Reference]({hooks.GITHUB_BLOB}{source}#details)",
                    hooks._rewrite_links(f"[Reference]({source}#details)", "README.md"),
                )

    def test_measurement_links_keep_the_site_address(self):
        self.assertEqual(
            "[Evidence](/assets/benchmarks/real-apps.json)",
            hooks._rewrite_links(
                "[Evidence](../gh-pages/docs/assets/benchmarks/real-apps.json)",
                "docs/real-app-benchmark.md",
            ),
        )
        self.assertEqual(
            "[Evidence](/assets/benchmarks/screenshot-readability.json)",
            hooks._rewrite_links(
                "[Evidence](../gh-pages/docs/assets/benchmarks/screenshot-readability.json)",
                "docs/screenshot-ui-automation-benchmark.md",
            ),
        )
        self.assertEqual(
            "[Results](/benchmark/#raw-samples)",
            hooks._rewrite_links(
                "[Results](docs/incremental-snapshot-benchmark.md#raw-samples)",
                "README.md",
            ),
        )
        self.assertEqual(
            "[Results](/benchmark/#reproduce)",
            hooks._rewrite_links(
                "[Results](docs/screenshot-ui-automation-benchmark.md#reproduce)",
                "README.md",
            ),
        )

    def test_screenshot_details_do_not_require_a_published_github_file(self):
        page = (SITE / "docs" / "benchmark.md").read_text(encoding="utf-8")
        self.assertIn('--8<-- "_generated/screenshot-benchmark.md"', page)
        self.assertIn('id="screenshot-comparison-details"', page)
        self.assertNotIn(
            f"{hooks.GITHUB_BLOB}docs/screenshot-ui-automation-benchmark.md", page
        )

    def test_real_app_method_is_included_with_current_evidence(self):
        page = (SITE / "docs" / "benchmark.md").read_text(encoding="utf-8")
        self.assertIn('--8<-- "_generated/real-app-benchmark.md"', page)
        self.assertIn('id="real-app-details"', page)
        self.assertTrue(
            (SITE / "docs" / "assets" / "benchmarks" / "real-apps.json").exists()
        )

    def test_only_measurements_are_included_in_user_pages(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)
            with patch.object(hooks, "GEN_DIR", output):
                hooks.on_pre_build({})
            self.assertEqual(
                ["benchmark.md", "real-app-benchmark.md", "screenshot-benchmark.md"],
                sorted(path.name for path in output.iterdir()),
            )
            text = (output / "benchmark.md").read_text(encoding="utf-8")
            self.assertIn("## Raw samples", text)
            self.assertFalse(text.startswith("# Incremental UI snapshot benchmark"))
            screenshot = (output / "screenshot-benchmark.md").read_text(encoding="utf-8")
            self.assertIn("## Sample totals", screenshot)
            self.assertIn("## Reproduce", screenshot)
            self.assertNotIn("# Screenshots versus UI Automation:", screenshot)
            self.assertIn("[changes-only measurement](/benchmark/)", screenshot)
            real_apps = (output / "real-app-benchmark.md").read_text(encoding="utf-8")
            self.assertIn("## Tasks", real_apps)
            self.assertIn("## Reproduce the real-app comparison", real_apps)
            self.assertNotIn("# Real Windows tasks:", real_apps)


if __name__ == "__main__":
    unittest.main()
