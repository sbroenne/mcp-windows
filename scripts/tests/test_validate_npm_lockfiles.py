import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "validate_npm_lockfiles.py"


class ValidateNpmLockfilesTests(unittest.TestCase):
    def setUp(self):
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary_directory.cleanup)
        self.root = Path(self.temporary_directory.name)
        self.git("init", "--quiet")

    def git(self, *arguments):
        return subprocess.run(
            ["git", *arguments],
            cwd=self.root,
            capture_output=True,
            check=True,
            timeout=10,
        )

    def write_lockfile(self, path="package-lock.json", *, data=None, stage=True):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(
            json.dumps(data if data is not None else {
                "lockfileVersion": 3,
                "packages": {"node_modules/example": {
                    "version": "1.2.3",
                    "integrity": "sha512-example",
                }},
            }),
            encoding="utf-8",
        )
        if stage:
            self.git("add", "--force", "--", path)
        return target

    def validate(self, *arguments, cwd=None):
        return subprocess.run(
            [sys.executable, str(SCRIPT_PATH), *arguments],
            cwd=cwd or self.root,
            capture_output=True,
            text=True,
            timeout=15,
        )

    def test_accepts_portable_lockfiles_and_discovers_new_nested_projects(self):
        self.write_lockfile()
        self.write_lockfile("new project/deep/package-lock.json")
        self.write_lockfile("another/npm-shrinkwrap.json")
        result = self.validate(cwd=self.root / "new project")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("3 lockfile(s)", result.stdout)

    def test_rejects_download_urls_in_all_lockfile_formats_without_leaking_them(self):
        for version in (1, 2, 3):
            for url in (
                "https://user:secret@mirror.invalid/example.tgz?token=private",
                "http://mirror.invalid/example.tgz",
                "git+https://mirror.invalid/example.git#abc",
                "//mirror.invalid/example.tgz",
                "ftp://mirror.invalid/example.tgz",
            ):
                with self.subTest(version=version, url=url):
                    package = {"version": "1.2.3", "resolved": url}
                    data = {"lockfileVersion": version}
                    if version < 3:
                        data["dependencies"] = {"parent": {"dependencies": {"example": package}}}
                    if version > 1:
                        data["packages"] = {"node_modules/example": package}
                    self.write_lockfile(data=data)
                    result = self.validate()
                    self.assertEqual(result.returncode, 1)
                    self.assertIn("package-lock.json", result.stderr)
                    self.assertIn("fixed download URL", result.stderr)
                    for secret in ("mirror.invalid", "secret", "private", url):
                        self.assertNotIn(secret, result.stdout + result.stderr)

    def test_accepts_local_links_and_non_download_metadata_urls(self):
        self.write_lockfile(data={
            "lockfileVersion": 3,
            "packages": {
                "node_modules/local": {"resolved": "file:../local", "link": True},
                "node_modules/workspace": {"resolved": "packages/workspace", "link": True},
                "node_modules/example": {
                    "version": "1.2.3",
                    "funding": {"url": "https://example.org"},
                },
            },
        })
        result = self.validate()
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_ignores_untracked_and_node_modules_lockfiles(self):
        bad = {"packages": {"example": {"resolved": "https://mirror.invalid/example.tgz"}}}
        self.write_lockfile()
        self.write_lockfile("untracked/package-lock.json", data=bad, stage=False)
        self.write_lockfile("node_modules/tracked/package-lock.json", data=bad)
        self.write_lockfile("nested/node_modules/tracked/npm-shrinkwrap.json", data=bad)
        result = self.validate()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("1 lockfile(s)", result.stdout)

    def test_staged_check_cannot_be_bypassed_by_unstaged_repair(self):
        self.write_lockfile(data={
            "packages": {"example": {"resolved": "https://mirror.invalid/example.tgz"}},
        })
        self.write_lockfile(stage=False)
        self.assertEqual(self.validate().returncode, 0)
        result = self.validate("--staged")
        self.assertEqual(result.returncode, 1)
        self.assertIn("fixed download URL", result.stderr)

    def test_staged_check_ignores_unstaged_damage(self):
        self.write_lockfile()
        self.write_lockfile(data={
            "packages": {"example": {"resolved": "https://mirror.invalid/example.tgz"}},
        }, stage=False)
        result = self.validate("--staged")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.validate().returncode, 1)

    def test_staged_deletions_are_not_checked(self):
        self.write_lockfile()
        self.git("rm", "--cached", "--force", "--", "package-lock.json")
        result = self.validate("--staged")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("0 lockfile(s)", result.stdout)

    def test_rejects_malformed_json_without_echoing_content(self):
        target = self.write_lockfile()
        target.write_text('{"https://secret.invalid":', encoding="utf-8")
        result = self.validate()
        self.assertEqual(result.returncode, 1)
        self.assertIn("invalid JSON", result.stderr)
        self.assertNotIn("secret.invalid", result.stdout + result.stderr)

    def test_rejects_non_object_lockfile(self):
        self.write_lockfile(data=[])
        result = self.validate()
        self.assertEqual(result.returncode, 1)
        self.assertIn("JSON object", result.stderr)

    def test_missing_tracked_file_fails_instead_of_skipping(self):
        self.write_lockfile().unlink()
        result = self.validate()
        self.assertEqual(result.returncode, 1)
        self.assertIn("cannot read", result.stderr)

    def test_running_outside_git_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            result = self.validate(cwd=directory)
        self.assertEqual(result.returncode, 1)
        self.assertIn("Git", result.stderr)

    def test_pre_commit_hook_checks_staged_lockfiles(self):
        scripts = self.root / "scripts"
        scripts.mkdir()
        shutil.copyfile(SCRIPT_PATH, scripts / SCRIPT_PATH.name)
        hooks = self.root / ".githooks"
        hooks.mkdir()
        hook = hooks / "pre-commit"
        shutil.copyfile(SCRIPT_PATH.parents[1] / ".githooks" / "pre-commit", hook)
        hook.chmod(0o755)
        self.write_lockfile(data={
            "packages": {"example": {"resolved": "https://mirror.invalid/example.tgz"}},
        })
        self.write_lockfile(stage=False)
        command = ["git", "-c", "core.hooksPath=.githooks", "hook", "run", "pre-commit"]
        result = subprocess.run(
            command, cwd=self.root, capture_output=True, text=True, timeout=15,
        )
        self.assertEqual(result.returncode, 1)
        self.assertIn("fixed download URL", result.stderr)
        self.assertNotIn("mirror.invalid", result.stdout + result.stderr)
        self.git("add", "--", "package-lock.json")
        result = subprocess.run(
            command, cwd=self.root, capture_output=True, text=True, timeout=15,
        )
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
