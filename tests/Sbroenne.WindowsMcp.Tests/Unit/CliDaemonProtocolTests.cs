using System.Buffers.Binary;
using System.IO.Pipes;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Cli.Service;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class CliDaemonProtocolTests
{
    [Fact]
    public async Task Frame_RoundTripsExactArgumentTokensAndCallerDirectory()
    {
        var expected = new DaemonRequest("execute",
            ["app", "--args=--new-window \"a & b\"", "--path", @"C:\Program Files\App\app.exe"],
            @"C:\Caller's folder", Build: DaemonIdentity.Build);
        await using var stream = new MemoryStream();
        await DaemonProtocol.WriteAsync(stream, expected, TestContextToken);
        stream.Position = 0;
        var actual = await DaemonProtocol.ReadAsync<DaemonRequest>(stream, TestContextToken);
        Assert.Equal(expected.Arguments, actual.Arguments);
        Assert.Equal(expected.WorkingDirectory, actual.WorkingDirectory);
        Assert.Equal(expected.Build, actual.Build);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(DaemonProtocol.MaximumFrameBytes + 1)]
    public async Task InvalidFrameLength_IsRejectedBeforePayloadAllocation(int size)
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, size);
        await using var stream = new MemoryStream(header);
        await Assert.ThrowsAsync<InvalidDataException>(
            () => DaemonProtocol.ReadAsync<DaemonRequest>(stream, TestContextToken));
    }

    [Fact]
    public async Task TruncatedFrame_FailsWithoutReinterpretingArguments()
    {
        await using var stream = new MemoryStream(new byte[] { 10, 0, 0, 0, 123 });
        await Assert.ThrowsAsync<EndOfStreamException>(
            () => DaemonProtocol.ReadAsync<DaemonRequest>(stream, TestContextToken));
    }

    [Fact]
    public void Identity_IsStableForInstallationAndBuild_NotWorkingDirectory()
    {
        Assert.StartsWith("wincli-", DaemonIdentity.PipeName, StringComparison.Ordinal);
        Assert.Equal(47, DaemonIdentity.PipeName.Length);
        Assert.Equal(32, DaemonIdentity.Build.Length);
        Assert.True(Path.IsPathFullyQualified(DaemonIdentity.Installation));
        Assert.StartsWith("S-1-5-5-", DaemonIdentity.LogonSid, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LostResponse_IsUnknownOutcomeAndIsNotReplayed()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await DaemonClient.StopAsync(timeout.Token);
        await using var server = new NamedPipeServerStream(DaemonIdentity.PipeName, PipeDirection.InOut,
            1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var accept = server.WaitForConnectionAsync(timeout.Token);
        var response = DaemonClient.SendAsync(new DaemonRequest("execute", ["ui", "click"],
            Environment.CurrentDirectory), TimeSpan.FromSeconds(3), CancellationToken.None);
        await accept;
        var request = await DaemonProtocol.ReadAsync<DaemonRequest>(server, CancellationToken.None);
        Assert.Equal("execute", request.Operation);
        server.Disconnect();
        var error = await Assert.ThrowsAsync<IOException>(() => response);
        Assert.Contains("outcome is unknown", error.Message, StringComparison.Ordinal);
        Assert.Contains("Do not automatically retry", error.Message, StringComparison.Ordinal);
        Assert.False(server.IsConnected);
    }

    [Theory]
    [InlineData("file-open", "path")]
    [InlineData("file-save", "file")]
    [InlineData("screenshot", "out")]
    [InlineData("macro", "steps-file")]
    public void CallerRelativeFiles_DoNotUseDaemonWorkingDirectory(string group, string option)
    {
        var parsed = ParsedArgs.Parse([group, $"--{option}=relative file.json"]);
        var contextual = CliOperationService.BindWorkingDirectory(parsed, @"C:\Caller");
        Assert.Equal(@"C:\Caller\relative file.json", contextual.GetString(option));
    }

    [Fact]
    public void AppContext_PreservesExactChildArgumentsAndDefaultsCallerDirectory()
    {
        var parsed = ParsedArgs.Parse(["app", "--path=.\\fixture.exe", "--args=--new-window \"local target\""]);
        var contextual = CliOperationService.BindWorkingDirectory(parsed, @"C:\Caller");
        Assert.Equal(@"C:\Caller\fixture.exe", contextual.GetString("path"));
        Assert.Equal(@"C:\Caller", contextual.GetString("working-directory"));
        Assert.Equal("--new-window \"local target\"", contextual.GetString("args"));
    }

    private static CancellationToken TestContextToken => CancellationToken.None;
}
