# Contributing to Windows MCP

Thank you for your interest in contributing to the Windows MCP Server! This document provides guidelines and instructions for contributing.

## Code of Conduct

This project adheres to the Contributor Covenant. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 10.0 SDK
- Node.js 18+ (for VS Code extension development)
- Visual Studio 2022 or VS Code with C# extension

### Setup Development Environment

1. **Clone the repository:**
   ```bash
   git clone https://github.com/sbroenne/mcp-windows.git
   cd mcp-windows
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

4. **Run tests:**
   ```bash
   dotnet test
   ```

## Development Workflow

### Branch Naming

Use descriptive branch names following this pattern:

```
<type>/<short-description>
```

Examples:
- `feature/keyboard-macro-support`
- `fix/window-activation-timeout`
- `docs/update-readme`
- `test/add-integration-tests`

Types:
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `test/` - Test additions
- `refactor/` - Code refactoring
- `perf/` - Performance improvements

### Commit Messages

Follow conventional commits format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

Examples:
```
feat(keyboard): add macro recording support

Implement ability to record and replay keyboard sequences.
Fixes #123
```

```
fix(window): resolve activation timeout on slow systems

Increase default timeout and add exponential backoff retry logic.
```

Types:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation
- `test:` - Test-related
- `refactor:` - Code refactoring
- `perf:` - Performance improvement
- `chore:` - Maintenance tasks

### Pull Requests

1. **Create a feature branch** from `main`
2. **Make your changes** with clear, focused commits
3. **Write or update tests** for your changes
4. **Update documentation** if needed
5. **Push to your fork** and create a pull request

**Pull Request Guidelines:**
- Use a clear, descriptive title
- Link related issues (`Fixes #123`)
- Provide context and motivation for changes
- Include testing instructions if applicable
- Ensure all CI checks pass

## Testing

### Running Tests

```bash
# All tests
dotnet test

# Specific category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Specific test class
dotnet test --filter "FullyQualifiedName~MouseInputServiceTests"

# With code coverage (Debug config so PDBs exist for instrumentation;
# Release strips symbols via DebugType=none, which yields empty coverage)
dotnet test tests/Sbroenne.WindowsMcp.Tests/Sbroenne.WindowsMcp.Tests.csproj `
  --configuration Debug --collect:"XPlat Code Coverage" --settings .runsettings
# Cobertura reports land under TestResults/<guid>/coverage.cobertura.xml
# CI collects the same report and uploads it as the "code-coverage" artifact.
```

### Writing Tests

- Place unit tests in `tests/Sbroenne.WindowsMcp.Tests/Unit/`
- Place integration tests in `tests/Sbroenne.WindowsMcp.Tests/Integration/`
- Use descriptive test names: `public void MethodName_WithCondition_ReturnsExpected()`
- Mark integration tests with `[Trait("Category", "Integration")]`
- Mock external dependencies in unit tests

Example:
```csharp
[Fact]
public void Click_WithValidCoordinates_SendsClickInput()
{
    // Arrange
    var service = new MouseInputService();
    
    // Act
    var result = service.Click(100, 200);
    
    // Assert
    Assert.True(result.Success);
}
```

### LLM Integration Tests

LLM integration tests verify that AI agents can correctly use the MCP tools with **real AI models**. These tests are critical because:

- **Tool descriptions must be LLM-friendly** — If an AI misunderstands a parameter, it fails silently
- **Response formats affect reasoning** — Structured hints guide the LLM to correct next steps  
- **Edge cases surface quickly** — Real models find ambiguities that unit tests miss

These tests use [pytest-skill-engineering](https://github.com/sbroenne/pytest-skill-engineering) and require:
- GitHub authentication (`GITHUB_TOKEN` or an existing `gh auth login`)
- Windows desktop session with GUI access
- Python 3.12+ and uv

**LLM tests are intentionally manual-only.** They never run as part of PR, CI, or release workflows. Run them from the dedicated **LLM Integration Tests** workflow in GitHub Actions, or locally as described below. See [.github/RELEASE_SETUP.md](.github/RELEASE_SETUP.md) for details.

#### Running LLM Tests

```powershell
# Run all LLM tests
cd tests/Sbroenne.WindowsMcp.LLM.Tests
uv run pytest -v

# Run specific test
uv run pytest test_notepad_ui.py -v

# Run integration tests
uv run pytest integration/ -v
```

#### Available Test Suites

| Test File | Description |
|-----------|-------------|
| `integration/test_window_management.py` | Find, activate, move, resize windows |
| `integration/test_window_activate.py` | Window activation |
| `test_notepad_ui.py` / `integration/test_file_dialog.py` | Notepad UI automation and Save As dialogs |
| `test_paint_workflow.py` / `integration/test_paint_ui.py` | Paint ribbon UI and canvas drawing |
| `integration/test_screenshot.py` / `test_screenshot_workflow.py` | Screenshot capture with annotations |
| `integration/test_keyboard_mouse.py` | Keyboard and mouse control |
| `integration/test_run_dialog.py` / `integration/test_app_tool_uwp.py` | App launch (classic + UWP) |
| `test_calculator_workflow.py` / `eval/` | Multi-step, real-world workflows |

#### Test Design Principles

- **Use well-known apps**: Tests target Notepad, Paint, Calculator (apps LLMs recognize)
- **Manual-only execution**: Run the suite explicitly through the dedicated workflow or locally
- **Real model**: Tests run against GPT-5.5 via GitHub Copilot
- **Token tracking**: Tests report token usage to validate optimization

#### Adding New LLM Tests

1. Create a new YAML file in `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/`
2. Follow the existing test structure with sessions, tests, and assertions
3. Use `app` tool to launch applications LLMs know (not custom paths)
4. Include assertions for `tool_called`, `no_hallucinated_tools`, and `output_regex`
5. Run locally before committing

See [`tests/Sbroenne.WindowsMcp.LLM.Tests/README.md`](tests/Sbroenne.WindowsMcp.LLM.Tests/README.md) for complete documentation.

## Code Style

### C# Standards

- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` for obvious types, explicit types for clarity
- Use nullable reference types (`#nullable enable`)
- Prefer records for immutable data types
- Use dependency injection for services

### EditorConfig

The repository includes `.editorconfig` for consistent formatting. Most editors will apply these settings automatically.

### Code Review Checklist

Before submitting a PR, ensure:

- [ ] Code follows project style guidelines
- [ ] All tests pass (`dotnet test`)
- [ ] New tests added for new functionality
- [ ] Documentation updated (README, comments, etc.)
- [ ] No commented-out code or debug statements
- [ ] Commit messages follow conventional commits
- [ ] Branch is up-to-date with `main`

## Documentation

### Updating README

Update [README.md](https://github.com/sbroenne/mcp-windows/blob/main/README.md) when:
- Adding new features or tools
- Changing configuration options
- Updating installation instructions
- Adding new examples

### Code Comments

- Add XML documentation comments to public APIs
- Explain complex logic with clear comments
- Avoid obvious comments ("increment i")

Example:
```csharp
/// <summary>
/// Sends a mouse click at the specified coordinates.
/// </summary>
/// <param name="x">X coordinate in screen space</param>
/// <param name="y">Y coordinate in screen space</param>
/// <returns>Result indicating success or failure</returns>
public ClickResult Click(int x, int y)
{
    // Implementation
}
```

## Extension Development

### VS Code Extension Structure

```
vscode-extension/
├── src/
│   └── extension.ts          # Main extension entry point
├── package.json              # Extension manifest
├── tsconfig.json             # TypeScript configuration
└── bin/                       # Compiled MCP server binaries
```

### Building the Extension

```bash
cd vscode-extension

# Install dependencies
npm install

# Build TypeScript
npm run compile

# Package VSIX
npm run package

# Run in development mode
npm run watch
```

### Extension Guidelines

- Keep extension code minimal
- Focus on server implementation in .NET
- Use TypeScript for type safety
- Follow VS Code extension best practices

## Releases

Releases are triggered by git tags following semantic versioning:

- **MCP Server**: `mcp-v<major>.<minor>.<patch>` (e.g., `mcp-v1.2.0`)
- **VS Code Extension**: `vscode-v<major>.<minor>.<patch>` (e.g., `vscode-v1.2.0`)

Release workflows automatically:
1. Build and test
2. Create GitHub Release with notes
3. Publish VS Code extension to Marketplace

### Release Checklist

Before creating a release tag:

- [ ] All changes merged to `main`
- [ ] Version numbers updated in relevant files
- [ ] CHANGELOG updated with release notes
- [ ] All tests passing
- [ ] Documentation up-to-date

## Reporting Issues

### Bug Reports

Include:
- Windows version (10/11, build number)
- .NET version (`dotnet --version`)
- Steps to reproduce
- Expected behavior
- Actual behavior
- Relevant error messages or logs

### Feature Requests

Include:
- Use case and motivation
- Proposed solution (if any)
- Alternative approaches
- Example scenarios

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

- Open a Discussion on GitHub
- Check existing Issues and PRs
- Review Architecture in README

Thank you for contributing! 🚀
