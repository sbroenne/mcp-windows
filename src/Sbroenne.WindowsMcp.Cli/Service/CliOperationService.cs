using System.Globalization;

namespace Sbroenne.WindowsMcp.Cli.Service;

/// <summary>Host adapter over the shared tool dispatcher; never redirects process-wide Console.</summary>
internal static class CliOperationService
{
    internal static async Task<DaemonResponse> ExecuteAsync(
        ParsedArgs arguments, string workingDirectory, CancellationToken token)
    {
        using var output = new BoundedWriter();
        using var error = new BoundedWriter();
        try
        {
            var contextual = BindWorkingDirectory(arguments, workingDirectory);
            var code = await CommandDispatcher.DispatchAsync(contextual, output, error, token);
            return new(code, output.ToString(), error.ToString());
        }
        catch (ArgumentException ex)
        {
            return new(ExitCodes.UsageError, "", $"error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new(ExitCodes.ToolError, "", $"error: {ex.Message}");
        }
    }

    internal static ParsedArgs BindWorkingDirectory(ParsedArgs arguments, string workingDirectory)
    {
        var pathOptions = arguments.Group switch
        {
            "app" => "working-dir cwd working-directory",
            "screenshot" => "output-path out",
            "file-open" or "fileopen" or "open" or "file-save" or "filesave" or "save" => "path file-path file",
            "ui" or "macro" or "ui-macro" => "steps-file",
            _ => "",
        };
        var paths = pathOptions.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var tokens = arguments.CommandPath.ToList();
        foreach (var option in arguments.OptionNames)
        {
            var value = arguments.GetString(option);
            if (!string.IsNullOrEmpty(value))
            {
                if (paths.Contains(option)
                    || (arguments.Group == "app" && option is "path" or "program" or "program-path"
                        && (value.Contains('\\') || value.Contains('/') || File.Exists(Path.Combine(workingDirectory, value)))))
                {
                    value = Path.GetFullPath(value, workingDirectory);
                }
            }

            tokens.Add(value is null ? $"--{option}" : $"--{option}={value}");
        }

        if (arguments.Group == "app" && !arguments.Has("working-dir")
            && !arguments.Has("cwd") && !arguments.Has("working-directory"))
        {
            tokens.Add($"--working-directory={workingDirectory}");
        }

        return ParsedArgs.Parse([.. tokens]);
    }

    private sealed class BoundedWriter() : StringWriter(CultureInfo.InvariantCulture)
    {
        // Leave room for JSON escaping and transport metadata inside the 4 MiB frame.
        private const int MaximumCharacters = 256 * 1024;

        public override void WriteLine(string? value)
        {
            if (GetStringBuilder().Length + (value?.Length ?? 0) + NewLine.Length > MaximumCharacters)
            {
                throw new InvalidDataException(
                    "CLI operation ran, but its response exceeds the transport output limit. Do not automatically retry.");
            }

            base.WriteLine(value);
        }
    }
}
