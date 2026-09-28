import importlib.util
import json
import re
import unittest
from pathlib import Path


SITE = Path(__file__).resolve().parents[1]
MODELS = {
    "gpt-6-astra": "GPT-6 Astra",
    "gpt-6-luna": "GPT-6 Luna",
    "gpt-5.6-sol": "GPT-5.6 Sol",
    "gpt-5.6-luna": "GPT-5.6 Luna",
}


def evidence():
    return json.loads(
        (SITE / "docs" / "assets" / "benchmarks" / "screenshot-readability.json").read_text(encoding="utf-8")
    )


def table_rows(path):
    return {
        cells[0]: cells[1:]
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.startswith("| ")
        for cells in [[cell.strip() for cell in line.strip("|").split("|")]]
    }


class BenchmarkChartTests(unittest.TestCase):
    def test_charts_show_canonical_values_on_honest_scales(self):
        updates = table_rows(SITE.parent / "docs" / "incremental-snapshot-benchmark.md")
        expected = {
            "screenshot-form": (
                100,
                [
                    f"{summary['savings_percent']['low']:.1f}%"
                    for summary in evidence()["summary"]
                ],
            ),
            "text-updates": (
                100,
                [
                    updates["Excel worksheet editing"][2],
                    updates["Word document editing"][2],
                    updates["Electron test app navigation"][2],
                ],
            ),
        }
        page = "\n".join(
            path.read_text(encoding="utf-8")
            for path in sorted((SITE / "docs" / "assets" / "charts").glob("*.html"))
        )
        charts = re.findall(
            r'<figure class="benchmark-chart" data-benchmark="([^"]+)" '
            r'style="--chart-max: ([\d.]+)">(.*?)</figure>',
            page,
            re.DOTALL,
        )
        self.assertEqual(set(expected), {chart[0] for chart in charts})
        self.assertEqual(len(expected), len(charts))
        for name, maximum, content in charts:
            with self.subTest(chart=name):
                expected_maximum, expected_values = expected[name]
                self.assertEqual(expected_maximum, float(maximum))
                self.assertIn("<figcaption>", content)
                bars = re.findall(
                    r'<li style="--chart-value: ([\d.]+)">(.*?)</li>',
                    content,
                    re.DOTALL,
                )
                self.assertEqual(len(expected_values), len(bars))
                for (value, bar), expected_value in zip(bars, expected_values):
                    number = float(expected_value.replace(",", "").rstrip("%"))
                    self.assertEqual(number, float(value))
                    self.assertLessEqual(number, float(maximum))
                    self.assertGreaterEqual(number, 0)
                    self.assertIn(f">{expected_value}</span>", bar)
                    self.assertIn('class="benchmark-chart__label"', bar)
                    self.assertIn('aria-hidden="true"', bar)

    def test_homepage_and_benchmark_reuse_both_charts(self):
        for name in ("index.md", "benchmark.md"):
            page = (SITE / "docs" / name).read_text(encoding="utf-8")
            for chart in ("screenshot-form", "text-updates"):
                with self.subTest(page=name, chart=chart):
                    self.assertEqual(1, page.count(f'--8<-- "assets/charts/{chart}.html"'))

    def test_readme_images_match_shared_charts(self):
        spec = importlib.util.spec_from_file_location(
            "benchmark_images", SITE.parent / "scripts" / "generate-benchmark-charts.py"
        )
        images = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(images)
        readme = (SITE.parent / "README.md").read_text(encoding="utf-8")
        for name in ("screenshot-form", "text-updates"):
            with self.subTest(chart=name):
                source = SITE / "docs" / "assets" / "charts" / f"{name}.html"
                image = source.with_suffix(".svg")
                self.assertEqual(images.render_svg(source), image.read_text(encoding="utf-8"))
                self.assertIn(f"(gh-pages/docs/assets/charts/{name}.svg)", readme)

    def test_live_results_match_persisted_model_evidence(self):
        spec = importlib.util.spec_from_file_location(
            "comparison", SITE.parent / "scripts" / "summarize-screenshot-readability.py"
        )
        comparison = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(comparison)
        data = evidence()
        self.assertEqual(data["summary"], comparison.summarize_runs(data["runs"]))
        self.assertEqual(set(MODELS), {run["manifest"]["model"] for run in data["runs"]})
        self.assertEqual(300, sum(len(run["observations"]) for run in data["runs"]))
        document = (SITE.parent / "docs" / "screenshot-ui-automation-benchmark.md").read_text(encoding="utf-8")
        page = (SITE / "docs" / "benchmark.md").read_text(encoding="utf-8")
        for summary in data["summary"]:
            name = MODELS[summary["model"]]
            for arm, label in (("text", "Direct text"), ("low", "Low-detail image"), ("high", "High-detail image")):
                values = summary["arms"][arm]
                self.assertIn(
                    f"| {name} / {label} | {values['correct']}/{values['count']} | "
                    f"{values['median_input_tokens']:g} | {values['median_output_tokens']:g} | "
                    f"{values['median_seconds']:.2f} |", document
                )
            self.assertIn(
                f"| {name} | "
                + " | ".join(f"{summary['arms'][arm]['median_seconds']:.2f}" for arm in ("text", "low", "high"))
                + " |", page
            )
            for arm in ("low", "high"):
                self.assertIn(f"{summary['savings_percent'][arm]:.1f}%", document)

    def test_public_comparison_uses_only_requested_models(self):
        for path in (
            SITE.parent / "README.md", SITE.parent / "docs" / "screenshot-ui-automation-benchmark.md",
            SITE / "docs" / "index.md", SITE / "docs" / "comparison.md", SITE / "docs" / "benchmark.md",
            SITE / "docs" / "assets" / "charts" / "screenshot-form.html",
        ):
            text = path.read_text(encoding="utf-8")
            self.assertNotIn("GPT-4.1", text, path)
            self.assertNotIn("GPT-5.5", text, path)


if __name__ == "__main__":
    unittest.main()
