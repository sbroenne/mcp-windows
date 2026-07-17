using System.Runtime.Versioning;
using System.Text.Json;

namespace Sbroenne.WindowsMcp.Macros;

/// <summary>
/// Persists and retrieves named UI-automation macros. A macro is simply a saved <c>ui_batch</c>
/// steps array, stored as one JSON file per macro. This service is a "dumb actuator": it validates
/// names and step JSON, then reads/writes files. Replay is delegated to the existing batch engine so
/// a macro run behaves exactly like the equivalent inline ui_batch call.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MacroService
{
    private static readonly JsonSerializerOptions FileOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions StepParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _rootDirectory;

    /// <summary>
    /// Creates a macro service. When <paramref name="rootDirectory"/> is null the macros live under
    /// <c>%LOCALAPPDATA%\Sbroenne.WindowsMcp\macros</c> so they persist across agent sessions.
    /// </summary>
    public MacroService(string? rootDirectory = null)
    {
        _rootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sbroenne.WindowsMcp",
            "macros");
    }

    /// <summary>The directory macros are stored in.</summary>
    public string RootDirectory => _rootDirectory;

    /// <summary>
    /// Saves (or overwrites) a macro named <paramref name="name"/> from a ui_batch steps array.
    /// The steps JSON must parse to a non-empty array of batch step objects.
    /// </summary>
    public async Task<MacroResult> SaveAsync(string name, string stepsJson, CancellationToken cancellationToken = default)
    {
        if (!TryValidateName(name, "save", out var nameError))
        {
            return nameError!;
        }

        if (string.IsNullOrWhiteSpace(stepsJson))
        {
            return MacroResult.Failure("save", "steps is required: a JSON array of ui_batch step objects.");
        }

        BatchStep[]? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<BatchStep[]>(stepsJson, StepParseOptions);
        }
        catch (JsonException ex)
        {
            return MacroResult.Failure("save", $"steps is not valid JSON: {ex.Message}. Expected a JSON array of step objects.");
        }

        if (parsed is null || parsed.Length == 0)
        {
            return MacroResult.Failure("save", "steps must be a non-empty JSON array of step objects.");
        }

        JsonElement stepsElement;
        try
        {
            using var document = JsonDocument.Parse(stepsJson);
            stepsElement = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return MacroResult.Failure("save", $"steps is not valid JSON: {ex.Message}.");
        }

        var definition = new MacroDefinition
        {
            Name = name,
            StepCount = parsed.Length,
            SavedAtUtc = DateTime.UtcNow.ToString("o"),
            Steps = stepsElement
        };

        Directory.CreateDirectory(_rootDirectory);
        var path = PathFor(name);
        var json = JsonSerializer.Serialize(definition, FileOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);

        return new MacroResult { Success = true, Action = "save", Name = name, StepCount = parsed.Length };
    }

    /// <summary>Lists all saved macros (name, step count, timestamp) ordered by name.</summary>
    public async Task<MacroResult> ListAsync(CancellationToken cancellationToken = default)
    {
        var summaries = new List<MacroSummary>();
        if (Directory.Exists(_rootDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(_rootDirectory, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var text = await File.ReadAllTextAsync(file, cancellationToken);
                    var definition = JsonSerializer.Deserialize<MacroDefinition>(text, StepParseOptions);
                    if (definition is not null)
                    {
                        summaries.Add(new MacroSummary
                        {
                            Name = definition.Name,
                            StepCount = definition.StepCount,
                            SavedAtUtc = definition.SavedAtUtc
                        });
                    }
                }
                catch (JsonException)
                {
                    // Skip corrupt files rather than failing the whole listing.
                }
            }
        }

        return new MacroResult { Success = true, Action = "list", Macros = summaries };
    }

    /// <summary>Returns the steps of a single saved macro.</summary>
    public async Task<MacroResult> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!TryValidateName(name, "get", out var nameError))
        {
            return nameError!;
        }

        var path = PathFor(name);
        if (!File.Exists(path))
        {
            return MacroResult.Failure("get", $"Macro '{name}' does not exist. Use ui_macro(action='list') to see saved macros.");
        }

        var definition = await ReadDefinitionAsync(path, cancellationToken);
        if (definition is null)
        {
            return MacroResult.Failure("get", $"Macro '{name}' is corrupt and could not be read.");
        }

        return new MacroResult
        {
            Success = true,
            Action = "get",
            Name = definition.Name,
            StepCount = definition.StepCount,
            Steps = definition.Steps
        };
    }

    /// <summary>Deletes a saved macro.</summary>
    public Task<MacroResult> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!TryValidateName(name, "delete", out var nameError))
        {
            return Task.FromResult(nameError!);
        }

        var path = PathFor(name);
        if (!File.Exists(path))
        {
            return Task.FromResult(MacroResult.Failure("delete", $"Macro '{name}' does not exist."));
        }

        File.Delete(path);
        return Task.FromResult(new MacroResult { Success = true, Action = "delete", Name = name });
    }

    /// <summary>
    /// Loads a macro's steps array serialized back to a JSON string, ready to hand to the batch
    /// engine. Returns null when the macro is missing or corrupt.
    /// </summary>
    public async Task<string?> LoadStepsJsonAsync(string name, CancellationToken cancellationToken = default)
    {
        var path = PathFor(name);
        if (!File.Exists(path))
        {
            return null;
        }

        var definition = await ReadDefinitionAsync(path, cancellationToken);
        return definition?.Steps.GetRawText();
    }

    private async Task<MacroDefinition?> ReadDefinitionAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var text = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<MacroDefinition>(text, StepParseOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string PathFor(string name) => Path.Combine(_rootDirectory, name + ".json");

    private static bool TryValidateName(string? name, string action, out MacroResult? error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = MacroResult.Failure(action, "name is required (a short identifier for the macro).");
            return false;
        }

        if (name.Length > 100)
        {
            error = MacroResult.Failure(action, "name must be 100 characters or fewer.");
            return false;
        }

        foreach (var c in name)
        {
            var ok = char.IsLetterOrDigit(c) || c is '-' or '_' or '.';
            if (!ok)
            {
                error = MacroResult.Failure(action,
                    $"name '{name}' contains invalid characters. Use letters, digits, '-', '_' or '.' only.");
                return false;
            }
        }

        if (name is "." or "..")
        {
            error = MacroResult.Failure(action, "name is reserved. Choose a different macro name.");
            return false;
        }

        error = null;
        return true;
    }
}
