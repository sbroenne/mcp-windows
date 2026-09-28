import importlib.util
import json
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
spec = importlib.util.spec_from_file_location(
    "real_benchmark", Path(__file__).resolve().parents[1] / "benchmark-real-apps.py"
)
benchmark = importlib.util.module_from_spec(spec)
spec.loader.exec_module(benchmark)


class RealAppBenchmarkTests(unittest.TestCase):
    def test_setup_requires_physical_pixel_coordinates(self):
        for configured in (True, False):
            api = SimpleNamespace(
                SetProcessDpiAwarenessContext=Mock(return_value=configured),
                GetThreadDpiAwarenessContext=Mock(return_value=-4),
                AreDpiAwarenessContextsEqual=Mock(return_value=True),
            )
            benchmark.initialize_dpi(api)
            api.SetProcessDpiAwarenessContext.assert_called_once_with(-4)
            api.AreDpiAwarenessContextsEqual.assert_called_once_with(-4, -4)
        api.AreDpiAwarenessContextsEqual.return_value = False
        with self.assertRaisesRegex(RuntimeError, "physical"):
            benchmark.initialize_dpi(api)

    def guard(self, name, args, route="screenshots", app="notepad"):
        return benchmark.guard_arguments(name, args, route, {"123", "456"}, Path(r"C:\run\completed.txt"), app)

    def test_window_placement_preserves_scaled_benchmark_size(self):
        for dpi in (96, 120, 144, 168, 192):
            with self.subTest(dpi=dpi):
                scale = dpi / 96
                work_area = (0, 0, round(1920 * scale), round(1040 * scale))
                self.assertEqual(
                    tuple(round(value * scale) for value in (32, 32, 1280, 900)),
                    benchmark.window_placement(work_area, dpi),
                )

        self.assertEqual(
            (64, 64, 2560, 1600),
            benchmark.window_placement((0, 0, 2736, 1728), 192),
        )
        self.assertEqual(
            (164, 104, 2560, 1600),
            benchmark.window_placement((100, 40, 2836, 1768), 192),
        )

    def test_window_placement_rejects_invalid_display_settings(self):
        for work_area, dpi in (((0, 0, 2736, 1728), 0), ((0, 0, 64, 64), 96)):
            with self.subTest(work_area=work_area, dpi=dpi):
                with self.assertRaises(ValueError):
                    benchmark.window_placement(work_area, dpi)

    def test_screenshot_route_cannot_use_controls(self):
        with self.assertRaisesRegex(ValueError, "not available"):
            self.guard("ui_read", {"windowHandle": "123"})
        with self.assertRaisesRegex(ValueError, "not available"):
            self.guard("file_save", {"windowHandle": "123"})

    def test_screenshots_never_leak_control_annotations_or_other_windows(self):
        result = self.guard("screenshot_control", {"windowHandle": "123"})
        self.assertFalse(result["annotate"])
        self.assertTrue(result["includeImage"])
        self.assertEqual("window", result["target"])
        self.assertEqual("inline", result["outputMode"])
        with self.assertRaisesRegex(ValueError, "outside"):
            self.guard("screenshot_control", {"windowHandle": "999"})
        with self.assertRaisesRegex(ValueError, "not supported"):
            self.guard("screenshot_control", {"windowHandle": "123", "target": "all_monitors"})

    def test_controls_route_can_read_only_owned_windows_and_save_only_output(self):
        self.guard("ui_read", {"windowHandle": "123"}, "controls")
        with self.assertRaisesRegex(ValueError, "outside"):
            self.guard("ui_read", {"windowHandle": "999"}, "controls")
        with self.assertRaisesRegex(ValueError, "output"):
            self.guard("file_save", {"windowHandle": "123", "filePath": r"C:\other.txt"}, "controls")
        self.guard("file_save", {"windowHandle": "123", "filePath": r"C:\run\completed.txt"}, "controls")

    def test_shell_and_browser_developer_shortcuts_are_blocked(self):
        for args in [
            {"action": "press", "key": "r", "modifiers": "win"},
            {"action": "press", "key": "tab", "modifiers": "alt"},
            {"action": "type", "text": "javascript:document.body.innerText"},
            {"action": "key_down", "key": "ctrl"},
        ]:
            with self.assertRaises(ValueError):
                self.guard("keyboard_control", {"windowHandle": "123", **args})
        with self.assertRaises(ValueError):
            self.guard("keyboard_control", {"windowHandle": "123", "action": "press", "key": "f12"}, app="chrome")
        self.guard("keyboard_control", {"windowHandle": "123", "action": "press", "key": "f12"}, app="word")

    def test_owned_window_can_be_maximized_or_resized(self):
        for action in ("maximize", "resize", "set_bounds"):
            self.guard("window_management", {"action": action, "handle": "123"})
            with self.assertRaisesRegex(ValueError, "outside"):
                self.guard("window_management", {"action": action, "handle": "999"})

    def test_advertised_actions_match_adapter_restrictions(self):
        schema = {"properties": {"action": {"enum": ["maximize", "wait_for"]}}}
        self.assertEqual(["maximize"], benchmark.restricted_schema("window_management", schema)["properties"]["action"]["enum"])
        schema = {"properties": {"action": {"enum": ["press", "key_down"]}}}
        self.assertEqual(["press"], benchmark.restricted_schema("keyboard_control", schema)["properties"]["action"]["enum"])

    def test_notepad_requires_one_tab_with_the_generated_document(self):
        target = {"type": "TabItem", "name": "input-unique.txt. Unmodified."}
        self.assertEqual(target, benchmark.require_single_notepad_tab({"tree": [target]}, "input-unique.txt"))
        with self.assertRaisesRegex(RuntimeError, "another document"):
            benchmark.require_single_notepad_tab(
                {"tree": [target, {"type": "TabItem", "name": "An earlier file"}]}, "input-unique.txt")
        with self.assertRaises(RuntimeError):
            benchmark.require_single_notepad_tab({"tree": []}, "input-unique.txt")

    def test_office_window_scope_excludes_other_restored_documents(self):
        owned = benchmark.OwnedApp.__new__(benchmark.OwnedApp)
        owned.root = "123"
        for app, class_name in (("word", "OpusApp"), ("powerpoint", "PPTFrameClass")):
            owned.app = app
            with patch("win32gui.GetClassName", return_value=class_name):
                self.assertFalse(owned.is_other_office_document(123))
                self.assertTrue(owned.is_other_office_document(456))
            with patch("win32gui.GetClassName", return_value="#32770"):
                self.assertFalse(owned.is_other_office_document(456))
            owned.root = None
            with patch("win32gui.GetClassName", return_value=class_name):
                self.assertFalse(owned.is_other_office_document(456))
            owned.root = "123"

    def test_usage_is_total_conversation_usage_not_last_answer(self):
        usage = [
            {"model": "gpt-6-astra", "input_tokens": 100, "output_tokens": 20, "cache_read_tokens": 40},
            {"model": "gpt-6-astra", "input_tokens": 500, "output_tokens": 30, "cache_read_tokens": 80},
        ]
        result = benchmark.sum_usage(usage, "gpt-6-astra")
        self.assertEqual(600, result["input_tokens"])
        self.assertEqual(50, result["output_tokens"])
        self.assertEqual(120, result["cache_read_tokens"])
        with self.assertRaises(ValueError):
            benchmark.sum_usage(usage, "gpt-6-luna")
        with self.assertRaises(ValueError):
            benchmark.sum_usage([], "gpt-6-astra")


class RealAppBridgeTests(unittest.IsolatedAsyncioTestCase):
    async def test_notepad_detach_observes_new_window_after_focus_change_without_retry(self):
        calls = []
        source = Path("input-test.txt")
        target = {"type": "TabItem", "name": source.name, "click": [80, 40, 0]}

        class Client:
            async def call_tool(self, name, arguments):
                calls.append((name, arguments))
                if name == "ui_snapshot":
                    tabs = [target] if arguments["windowHandle"] == "456" else [
                        target, {"type": "TabItem", "name": "Restored document"}]
                    return SimpleNamespace(
                        is_error=False, content=[SimpleNamespace(text=json.dumps({"tree": tabs}))])
                return SimpleNamespace(is_error=name == "mouse_control", content=[])

        owned = SimpleNamespace(
            root="123", initial_bounds=(0, 0, 1280, 900), excluded_windows=set(),
            windows=lambda: [
                {"handle": "123", "title": "Restored document"},
                {"handle": "456", "title": source.name},
            ],
        )
        with patch("win32gui.MoveWindow"), patch("win32gui.GetWindowRect", return_value=owned.initial_bounds):
            result = await benchmark.isolate_notepad_window(Client(), owned, source)
        self.assertTrue(result["single_document_verified"])
        self.assertTrue(result["detach_reported_error"])
        self.assertEqual("456", owned.root)
        self.assertEqual({"123"}, owned.excluded_windows)
        self.assertEqual(1, sum(name == "mouse_control" for name, _ in calls))

    async def test_current_mcp_text_and_image_results_are_forwarded_and_logged(self):
        from mcp.types import CallToolResult, TextContent, ImageContent
        result = CallToolResult(content=[
            TextContent(type="text", text='{"success":true}'),
            ImageContent(type="image", data="aW1hZ2U=", mime_type="image/jpeg"),
        ], is_error=False)

        class Client:
            async def call_tool(self, *args, **kwargs):
                return result

        with tempfile.TemporaryDirectory() as directory:
            directory = Path(directory)
            owned = SimpleNamespace(app="notepad", windows=lambda: [{"handle": "123"}])
            bridge = benchmark.TrialBridge(Client(), owned, "screenshots", directory / "out.txt", directory, 5)
            reply = await bridge.invoke(SimpleNamespace(
                tool_name="keyboard_control",
                arguments={"windowHandle": "123", "action": "press", "key": "enter"},
            ))
            self.assertEqual("success", reply.result_type)
            self.assertEqual(1, len(reply.binary_results_for_llm))
            self.assertTrue((directory / "tool-calls.json").is_file())
            self.assertEqual(b"image", (directory / bridge.calls[0]["images"][0]["path"]).read_bytes())
            bridge.closed = True
            denied = await bridge.invoke(SimpleNamespace(tool_name="keyboard_control", arguments={}))
            self.assertEqual("denied", denied.result_type)
            self.assertEqual(1, len(bridge.calls))


if __name__ == "__main__":
    unittest.main()
