import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPOSITORY_ROOT / "scripts" / "update_star_history.py"


class UpdateStarHistoryTests(unittest.TestCase):
    def run_generator(self, snapshots, *, star_count=5, snapshot_date="2026-08-09"):
        with tempfile.TemporaryDirectory() as temporary_directory:
            temporary_path = Path(temporary_directory)
            history_path = temporary_path / "star-history.json"
            output_path = temporary_path / "star-history.svg"
            history_path.write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "repository": "sbroenne/mcp-windows",
                        "snapshots": snapshots,
                    }
                ),
                encoding="utf-8",
            )

            result = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT_PATH),
                    "--repository",
                    "sbroenne/mcp-windows",
                    "--history",
                    str(history_path),
                    "--output",
                    str(output_path),
                    "--star-count",
                    str(star_count),
                    "--snapshot-date",
                    snapshot_date,
                ],
                capture_output=True,
                check=False,
                text=True,
            )

            history = (
                json.loads(history_path.read_text(encoding="utf-8"))
                if history_path.exists()
                else None
            )
            svg = output_path.read_text(encoding="utf-8") if output_path.exists() else None
            return result, history, svg

    def test_appends_exact_daily_snapshot_and_generates_step_chart(self):
        result, history, svg = self.run_generator(
            [
                {"date": "2026-08-01", "count": 2},
                {"date": "2026-08-05", "count": 4},
            ]
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(
            history["snapshots"],
            [
                {"date": "2026-08-01", "count": 2},
                {"date": "2026-08-05", "count": 4},
                {"date": "2026-08-09", "count": 5},
            ],
        )
        self.assertEqual(
            set().union(*(snapshot.keys() for snapshot in history["snapshots"])),
            {"date", "count"},
        )
        self.assertIn("sbroenne/mcp-windows - 5 stars", svg)
        self.assertIn('class="line"', svg)
        self.assertIn(" H ", svg)
        self.assertIn(" V ", svg)

    def test_updates_same_day_snapshot_without_duplicating_it(self):
        result, history, _ = self.run_generator(
            [
                {"date": "2026-08-01", "count": 2},
                {"date": "2026-08-09", "count": 4},
            ],
            star_count=3,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(
            history["snapshots"],
            [
                {"date": "2026-08-01", "count": 2},
                {"date": "2026-08-09", "count": 3},
            ],
        )

    def test_preserves_real_star_count_decreases(self):
        result, history, _ = self.run_generator(
            [{"date": "2026-08-08", "count": 5}],
            star_count=4,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(history["snapshots"][-1], {"date": "2026-08-09", "count": 4})

    def test_rejects_snapshot_records_with_identity_fields(self):
        result, _, _ = self.run_generator(
            [{"date": "2026-08-08", "count": 5, "login": "not-allowed"}]
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("only 'date' and 'count'", result.stderr)

    def test_rejects_a_new_snapshot_older_than_persisted_history(self):
        result, _, _ = self.run_generator(
            [{"date": "2026-08-10", "count": 5}],
            snapshot_date="2026-08-09",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("older than the latest persisted snapshot", result.stderr)


if __name__ == "__main__":
    unittest.main()
