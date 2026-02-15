# MCP Server Evaluation Tests

This directory contains evaluation tests designed to compare different Windows MCP server implementations.

## Tests

| Test | Description |
|------|-------------|
| `test_4sysops_workflow.py` | sbroenne/mcp-windows — multi-file creation, system info queries |
| `test_4sysops_cursortouch.py` | CursorTouch/Windows-MCP — same tasks with coordinate-based tools |

## Running Evaluations

```bash
# Run sbroenne/mcp-windows eval tests
pytest eval/test_4sysops_workflow.py -v

# Run CursorTouch eval tests (requires CursorTouch server installed)
pytest eval/test_4sysops_cursortouch.py -v
```
