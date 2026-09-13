using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Cli.Service;

/// <summary>
/// Persistent CLI-only owner. MCP continues calling the same tools in its independent process.
/// The process-local static tool services are deliberately not shared across these owners.
/// </summary>
internal sealed class DaemonHost : IDisposable
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly SemaphoreSlim _actionGate = new(1, 1);
    private readonly ConcurrentDictionary<int, Task> _clients = new();
    private readonly string _generation = Guid.NewGuid().ToString("N");
    private int _nextClient;
    private bool _runtimeUsed;

    internal static int Run(CancellationToken token)
    {
        // Run synchronously so the lifetime mutex is released by its acquiring thread.
        using var owner = DaemonIdentity.CreateMutex(DaemonIdentity.OwnerMutex);
        if (!DaemonIdentity.Acquire(owner, TimeSpan.Zero))
        {
            return ExitCodes.ToolError;
        }

        try
        {
            using var host = new DaemonHost();
            host.RunAsync(token).GetAwaiter().GetResult();
            return ExitCodes.Success;
        }
        finally
        {
            owner.ReleaseMutex();
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        WindowsToolsBase.RequireExplicitSnapshotBaseline = true;
        _runtimeUsed = true;
        using var registration = token.Register(_shutdown.Cancel);
        try
        {
            await Task.WhenAll(
                ListenAsync(DaemonIdentity.PipeName, control: false),
                ListenAsync(DaemonIdentity.PipeName + "-control", control: true));
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // A normal stop cancels queued and active requests, but never terminates target apps.
        }
        finally
        {
            await _shutdown.CancelAsync();
            try
            {
                await Task.WhenAll(_clients.Values).WaitAsync(DaemonClient.ControlTimeout, CancellationToken.None);
            }
            catch (TimeoutException)
            {
                // Only this process exits. Non-cooperating provider work is not replayed.
            }
        }
    }

    private async Task ListenAsync(string name, bool control)
    {
        using var capacity = new SemaphoreSlim(control ? 4 : 16);
        while (!_shutdown.IsCancellationRequested)
        {
            await capacity.WaitAsync(_shutdown.Token);
            NamedPipeServerStream? pipe = null;
            try
            {
                var security = new PipeSecurity();
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
                    PipeAccessRights.FullControl, AccessControlType.Deny));
                security.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(DaemonIdentity.LogonSid),
                    PipeAccessRights.FullControl, AccessControlType.Allow));
                pipe = NamedPipeServerStreamAcl.Create(
                    name, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                    4096, 4096, security);
                await pipe.WaitForConnectionAsync(_shutdown.Token);
                var id = Interlocked.Increment(ref _nextClient);
                var connected = pipe;
                pipe = null;
                var task = HandleAsync(connected, control);
                _clients[id] = task;
                _ = CompleteClientAsync(task, id, capacity);
            }
            finally
            {
                pipe?.Dispose();
            }
        }
    }

    private async Task CompleteClientAsync(Task task, int id, SemaphoreSlim capacity)
    {
        try
        {
            await task;
        }
        finally
        {
            _clients.TryRemove(id, out _);
            // The accept loop's semaphore remains alive until outstanding requests finish.
            try
            {
                capacity.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task HandleAsync(NamedPipeServerStream pipe, bool control)
    {
        await using var ownedPipe = pipe;
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        requestCts.CancelAfter(DaemonClient.ControlTimeout);
        try
        {
            var request = await DaemonProtocol.ReadAsync<DaemonRequest>(pipe, requestCts.Token);
            using var serverIdentity = WindowsIdentity.GetCurrent();
            var elevated = new WindowsPrincipal(serverIdentity).IsInRole(WindowsBuiltInRole.Administrator);
            var sameElevation = false;
            pipe.RunAsClient(() =>
            {
                using var clientIdentity = WindowsIdentity.GetCurrent();
                sameElevation = new WindowsPrincipal(clientIdentity).IsInRole(WindowsBuiltInRole.Administrator) == elevated;
            });
            if (await RejectElevationMismatchAsync(pipe, sameElevation, requestCts.Token))
            {
                return;
            }

            if (request.Protocol != DaemonIdentity.ProtocolVersion || request.Build != DaemonIdentity.Build)
            {
                await DaemonProtocol.WriteAsync(pipe,
                    new DaemonResponse(1, "", "CLI service build/protocol mismatch; no operation was dispatched."),
                    requestCts.Token);
                return;
            }

            if (control)
            {
                await HandleControlAsync(pipe, request, requestCts.Token);
                return;
            }

            if (request.Operation != "execute" || request.Arguments is null
                || string.IsNullOrWhiteSpace(request.WorkingDirectory)
                || !Path.IsPathFullyQualified(request.WorkingDirectory))
            {
                await DaemonProtocol.WriteAsync(pipe, new DaemonResponse(2, "", "Invalid operation request."), requestCts.Token);
                return;
            }

            requestCts.CancelAfter(DaemonClient.RequestTimeout);
            using var disconnectCts = CancellationTokenSource.CreateLinkedTokenSource(requestCts.Token);
            var disconnected = WatchDisconnectAsync(pipe, requestCts, disconnectCts.Token);
            try
            {
                var parsed = ParsedArgs.Parse(request.Arguments);
                var wait = parsed.Group == "ui" && parsed.Action == "wait";
                var acquired = false;
                string[]? priorKeys = null;
                var responseWritten = false;
                try
                {
                    if (!wait)
                    {
                        acquired = await _actionGate.WaitAsync(DaemonClient.StartupTimeout, requestCts.Token);
                        if (!acquired)
                        {
                            await DaemonProtocol.WriteAsync(pipe,
                                new DaemonResponse(1, "", "CLI service action queue timed out before dispatch."),
                                requestCts.Token);
                            return;
                        }
                    }

                    if (!wait)
                    {
                        priorKeys = [.. WindowsToolsBase.KeyboardInputService.GetHeldKeyNames()];
                    }

                    var result = await CliOperationService.ExecuteAsync(parsed, request.WorkingDirectory, requestCts.Token);
                    await DaemonProtocol.WriteAsync(pipe, result, requestCts.Token);
                    responseWritten = true;
                }
                finally
                {
                    if (!responseWritten && requestCts.IsCancellationRequested && priorKeys is not null)
                    {
                        foreach (var key in WindowsToolsBase.KeyboardInputService.GetHeldKeyNames().Except(priorKeys))
                        {
                            await WindowsToolsBase.KeyboardInputService.KeyUpAsync(key, CancellationToken.None);
                        }
                    }

                    if (acquired)
                    {
                        _actionGate.Release();
                    }
                }
            }
            finally
            {
                await disconnectCts.CancelAsync();
                await disconnected;
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or JsonException or ArgumentException)
        {
            // Never log request payloads or desktop text. Disconnect cancels, it does not replay.
        }
    }

    internal static async Task<bool> RejectElevationMismatchAsync(
        Stream pipe, bool sameElevation, CancellationToken cancellationToken)
    {
        if (sameElevation)
        {
            return false;
        }

        await DaemonProtocol.WriteAsync(pipe,
            new DaemonResponse(ExitCodes.ToolError, "", "CLI service elevation mismatch; no operation was dispatched."),
            cancellationToken);
        return true;
    }

    private async Task HandleControlAsync(Stream pipe, DaemonRequest request, CancellationToken token)
    {
        if (request.Operation == "status")
        {
            var output = JsonSerializer.Serialize(new
            {
                state = "running",
                processId = Environment.ProcessId,
                generation = _generation,
                protocol = DaemonIdentity.ProtocolVersion,
                build = DaemonIdentity.Build,
            }, DaemonProtocol.JsonOptions);
            await DaemonProtocol.WriteAsync(pipe, new DaemonResponse(0, output), token);
        }
        else if (request.Operation == "stop")
        {
            await DaemonProtocol.WriteAsync(pipe, new DaemonResponse(0, """{"state":"stopped"}"""), token);
            await _shutdown.CancelAsync();
        }
        else
        {
            await DaemonProtocol.WriteAsync(pipe, new DaemonResponse(2, "", "Unknown service control request."), token);
        }
    }

    private static async Task WatchDisconnectAsync(Stream stream, CancellationTokenSource request, CancellationToken token)
    {
        try
        {
            // One request per connection; EOF or additional input cancels this request.
            await stream.ReadExactlyAsync(new byte[1], token);
            await request.CancelAsync();
        }
        catch (IOException)
        {
            await request.CancelAsync();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        if (_runtimeUsed)
        {
            // Tracker disposal only forgets keys; emit key-up events before clearing it.
            WindowsToolsBase.KeyboardInputService.ReleaseAllKeysAsync(CancellationToken.None).GetAwaiter().GetResult();
            WindowsToolsBase.KeyboardInputService.Dispose();
            WindowsToolsBase.UIAutomationService.Dispose();
            WindowsToolsBase.UIAutomationThread.Dispose();
        }

        _actionGate.Dispose();
        _shutdown.Dispose();
    }
}
