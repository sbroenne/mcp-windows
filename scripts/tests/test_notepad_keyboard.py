"""Opt-in, no-model regression against the installed modern Notepad."""

import asyncio
import ctypes
from datetime import timedelta
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
import uuid

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
spec = importlib.util.spec_from_file_location(
    "real_app_benchmark", Path(__file__).resolve().parents[1] / "benchmark-real-apps.py")
benchmark = importlib.util.module_from_spec(spec)
spec.loader.exec_module(benchmark)


@unittest.skipUnless(
    os.environ.get("MCP_TEST_DESKTOP_INPUT") == "1" and os.environ.get("MCP_TEST_NOTEPAD_SERVER"),
    "Requires an available desktop, no existing Notepad process, and MCP_TEST_NOTEPAD_SERVER",
)
class NotepadKeyboardTests(unittest.IsolatedAsyncioTestCase):
    async def test_exact_text_through_keyboard_tool(self):
        from mcp import ClientSession
        from mcp.client.stdio import StdioServerParameters, stdio_client

        with tempfile.TemporaryDirectory(prefix="notepad-keyboard-") as temporary:
            directory = Path(temporary)
            source = directory / f"input-{uuid.uuid4().hex}.txt"
            source.write_text("Keyboard regression fixture\n", encoding="utf-8")
            with (directory / "server.log").open("w", encoding="utf-8") as log:
                async with stdio_client(StdioServerParameters(
                    command=os.environ["MCP_TEST_NOTEPAD_SERVER"], env=dict(os.environ),
                ), errlog=log) as streams:
                    async with ClientSession(*streams, read_timeout_seconds=timedelta(minutes=3)) as mcp:
                        await mcp.initialize()
                        owned = benchmark.OwnedApp("notepad", source, directory)
                        try:
                            activated = await mcp.call_tool(
                                "window_management", {"action": "activate", "handle": owned.root})
                            self.assertFalse(activated.is_error, str(activated.content))
                            await benchmark.isolate_notepad_window(mcp, owned, source)

                            async def call(name, **arguments):
                                result = await mcp.call_tool(name, arguments)
                                self.assertFalse(result.is_error, str(result.content))
                                data = json.loads(result.content[0].text)
                                self.assertTrue(data.get("success"), data)
                                return data

                            observed = await call("ui_snapshot", windowHandle=owned.root, maxDepth=20)
                            editors = [node for node in benchmark.flatten_tree(observed["tree"])
                                       if node.get("type") == "Document" and node.get("name") == "Text editor"]
                            self.assertEqual(1, len(editors))
                            editor = editors[0]
                            x, y, monitor = editor["click"]
                            self.assertEqual(0, monitor)
                            left, top, _, _ = owned.initial_bounds
                            await call("window_management", action="activate", handle=owned.root)
                            await call("mouse_control", action="click", windowHandle=owned.root,
                                       x=x - left, y=y - top)
                            async def read_editor():
                                read = await call("ui_read", windowHandle=owned.root, elementId=editor["id"])
                                return read["text"].replace("\r\n", "\n").replace("\r", "\n").removesuffix("\n")

                            for text in (
                                "VISIBLE FRAME FRESHNESS MARKER 123456789",
                                "Project Cedar\nReview date: 21 October 2026\nReady for review",
                                "Accents: \u00e9\u00f6\u00fc; CJK: \u4e2d\u6587; emoji: \U0001f680",
                                "Abc XYZ 123 !? " * 80,
                                "Column A\tColumn B\nOne\tTwo",
                            ):
                                with self.subTest(text=text):
                                    await call("keyboard_control", action="press", key="a",
                                               modifiers="ctrl", windowHandle=owned.root)
                                    clipboard_sequence = ctypes.windll.user32.GetClipboardSequenceNumber()
                                    await call("keyboard_control", action="type", text=text, windowHandle=owned.root)
                                    self.assertEqual(clipboard_sequence, ctypes.windll.user32.GetClipboardSequenceNumber())
                                    expected = text.replace("\r\n", "\n").replace("\r", "\n")
                                    actual = None
                                    for _ in range(40):
                                        actual = await read_editor()
                                        if actual == expected:
                                            break
                                        await asyncio.sleep(0.05)
                                    self.assertEqual(expected, actual)
                            await call("keyboard_control", action="press", key="end", modifiers="ctrl",
                                       windowHandle=owned.root)
                            await call("keyboard_control", action="type", text=" appended", windowHandle=owned.root)
                            self.assertEqual(text + " appended", await read_editor())
                        finally:
                            owned.close()


if __name__ == "__main__":
    unittest.main()
