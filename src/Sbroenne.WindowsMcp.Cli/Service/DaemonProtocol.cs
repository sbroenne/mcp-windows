using System.Buffers.Binary;
using System.Text.Json;

namespace Sbroenne.WindowsMcp.Cli.Service;

internal sealed record DaemonRequest(
    string Operation,
    string[]? Arguments = null,
    string? WorkingDirectory = null,
    int Protocol = DaemonIdentity.ProtocolVersion,
    string? Build = null);

internal sealed record DaemonResponse(
    int ExitCode,
    string Output,
    string Error = "");

/// <summary>Bounded, length-prefixed JSON. Arguments are tokens, never shell commands.</summary>
internal static class DaemonProtocol
{
    internal const int MaximumFrameBytes = 4 * 1024 * 1024;
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task WriteAsync<T>(Stream stream, T value, CancellationToken token)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        if (data.Length > MaximumFrameBytes)
        {
            throw new InvalidDataException("CLI service message exceeds the 4 MiB limit.");
        }

        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, data.Length);
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(data, token);
        await stream.FlushAsync(token);
    }

    internal static async Task<T> ReadAsync<T>(Stream stream, CancellationToken token)
    {
        var header = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(header, token);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length <= 0 || length > MaximumFrameBytes)
        {
            throw new InvalidDataException("Invalid CLI service message length.");
        }

        var data = new byte[length];
        await stream.ReadExactlyAsync(data, token);
        return JsonSerializer.Deserialize<T>(data, JsonOptions)
            ?? throw new InvalidDataException("Empty CLI service message.");
    }
}
