using System.Globalization;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>
/// Parsed command line: an ordered list of leading command tokens (the command path, e.g.
/// <c>ui click</c>) followed by <c>--option value</c> / <c>--flag</c> pairs.
/// </summary>
internal sealed class ParsedArgs
{
    private readonly Dictionary<string, string?> _options;

    private ParsedArgs(IReadOnlyList<string> commandPath, Dictionary<string, string?> options)
    {
        CommandPath = commandPath;
        _options = options;
    }

    /// <summary>Leading non-option tokens, e.g. ["ui", "click"].</summary>
    public IReadOnlyList<string> CommandPath { get; }

    /// <summary>First command token (group), lower-cased, or empty when none.</summary>
    public string Group => CommandPath.Count > 0 ? CommandPath[0].ToLowerInvariant() : string.Empty;

    /// <summary>Second command token (action/operation), lower-cased, or null.</summary>
    public string? Action => CommandPath.Count > 1 ? CommandPath[1].ToLowerInvariant() : null;

    /// <summary>
    /// Parses raw args. Options may be written <c>--key value</c>, <c>--key=value</c>, or a bare
    /// <c>--flag</c> (treated as present/true). Keys are normalized to lower-case kebab.
    /// </summary>
    public static ParsedArgs Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var commandPath = new List<string>();
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var seenOption = false;

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                seenOption = true;
                var body = token[2..];
                var eq = body.IndexOf('=', StringComparison.Ordinal);
                if (eq >= 0)
                {
                    var k = body[..eq].ToLowerInvariant();
                    options[k] = body[(eq + 1)..];
                    continue;
                }

                var key = body.ToLowerInvariant();
                // Look ahead: if the next token is a value (not another --option), consume it.
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    options[key] = args[++i];
                }
                else
                {
                    options[key] = null; // bare flag
                }
            }
            else if (!seenOption)
            {
                commandPath.Add(token);
            }
            else
            {
                // Positional token after options started: attach to a numbered slot so it is not lost.
                options[$"_arg{options.Count}"] = token;
            }
        }

        return new ParsedArgs(commandPath, options);
    }

    /// <summary>True when the option was supplied (with or without a value).</summary>
    public bool Has(string key) => _options.ContainsKey(key);

    /// <summary>Returns the raw string value for an option, or null when absent.</summary>
    public string? GetString(string key) => _options.TryGetValue(key, out var v) ? v : null;

    /// <summary>Returns the first present option value among the given aliases.</summary>
    public string? GetString(params string[] keys)
    {
        foreach (var k in keys)
        {
            if (_options.TryGetValue(k, out var v))
            {
                return v;
            }
        }

        return null;
    }

    /// <summary>Parses an int option; returns null when absent or unparseable.</summary>
    public int? GetInt(params string[] keys)
    {
        var raw = GetString(keys);
        return raw is not null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }

    /// <summary>
    /// Resolves a boolean option. A bare flag (<c>--foo</c>) or <c>--foo true</c> is true;
    /// <c>--foo false</c> is false; absent returns <paramref name="defaultValue"/>.
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (!_options.TryGetValue(key, out var v))
        {
            return defaultValue;
        }

        if (v is null)
        {
            return true; // bare flag
        }

        return !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(v, "0", StringComparison.Ordinal);
    }

    /// <summary>
    /// True when any of the given alias flags is present and not explicitly "false"/"0".
    /// Used for false-default boolean flags that accept several spellings.
    /// </summary>
    public bool GetFlag(params string[] keys)
    {
        foreach (var k in keys)
        {
            if (_options.TryGetValue(k, out var v))
            {
                return v is null
                       || (!string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(v, "0", StringComparison.Ordinal));
            }
        }

        return false;
    }

    /// <summary>Nullable boolean tri-state: null when absent, else parsed.</summary>
    public bool? GetNullableBool(string key)
    {
        if (!_options.TryGetValue(key, out var v))
        {
            return null;
        }

        if (v is null)
        {
            return true;
        }

        return !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(v, "0", StringComparison.Ordinal);
    }
}
