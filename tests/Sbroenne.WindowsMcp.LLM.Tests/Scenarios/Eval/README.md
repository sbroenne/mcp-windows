# MCP Server Evaluation Tests

This directory contains evaluation tests designed to compare different Windows MCP server implementations.

## Purpose

These tests are inspired by real-world scenarios like the [4sysops review](https://4sysops.com/archives/windows-automation-with-ai-via-mcp/) and test common automation tasks that users would expect any Windows MCP server to handle.

## Test Scenarios

| Test | Description | Tools Required |
|------|-------------|----------------|
| `4sysops-workflow-test.yaml` | Multi-file creation, system info queries | File operations, terminal/keyboard |

## Server Configuration

### sbroenne/mcp-windows (Default)

```yaml
servers:
  - name: windows-mcp
    type: stdio
    command: "{{SERVER_COMMAND}}"
```

### CursorTouch/Windows-MCP

To test with CursorTouch's implementation, modify the server command:

```yaml
servers:
  - name: windows-mcp
    type: stdio
    command: "uv"
    args: ["run", "windows-mcp"]
```

## Running Evaluations

```powershell
# Run with sbroenne/mcp-windows (default)
.\Run-LLMTests.ps1 -Scenario Eval/4sysops-workflow-test.yaml

# Compare results between servers by running against different configurations
```

## Key Differences Being Evaluated

| Aspect | sbroenne/mcp-windows | CursorTouch/Windows-MCP |
|--------|---------------------|-------------------------|
| **UI Interaction** | Semantic (`ui_click(nameContains='Save')`) | Coordinate-based (`Click-Tool(loc=[x,y])`) |
| **Element Discovery** | Built into click/type tools | Requires `State-Tool` first |
| **Token Efficiency** | Optimized short property names | Standard JSON |
| **Testing** | LLM-tested with agent-benchmark | Manual testing |

## Notes

- Tests avoid UAC-triggering operations (no `winget install`)
- All tests should pass on both servers if tools are compatible
- Differences in tool naming require separate test files per server
