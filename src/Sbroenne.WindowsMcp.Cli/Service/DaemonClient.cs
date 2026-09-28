using System.Diagnostics;
using System.IO.Pipes;

namespace Sbroenne.WindowsMcp.Cli.Service;

internal static class DaemonClient
{
    // Match Excel CLI's readiness, probe, control and startup-lock policy.
    internal static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan ControlTimeout = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan StartupLockTimeout = TimeSpan.FromSeconds(11);
    internal static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(10);

    internal static async Task<DaemonResponse> SendAsync(
        DaemonRequest request, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        await using var pipe = new NamedPipeClientStream(
            ".", request.Operation == "execute" ? DaemonIdentity.PipeName : DaemonIdentity.PipeName + "-control", PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var sent = false;
        try
        {
            await pipe.ConnectAsync((int)ControlTimeout.TotalMilliseconds, deadline.Token);
            // A partial send can already reach the daemon. Never replay after this point.
            sent = true;
            await DaemonProtocol.WriteAsync(pipe, request with { Build = DaemonIdentity.Build }, deadline.Token);
            return await DaemonProtocol.ReadAsync<DaemonResponse>(pipe, deadline.Token);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException)
        {
            throw new IOException(sent
                ? "CLI service response lost or cancelled; operation outcome is unknown. Do not automatically retry."
                : "CLI service connection failed before dispatch; no operation was sent.", ex);
        }
    }

    internal static async Task<bool> ProbeAsync(CancellationToken token)
    {
        try
        {
            return (await SendAsync(new("status"), ProbeTimeout, token)).ExitCode == ExitCodes.Success;
        }
        catch (IOException) when (!token.IsCancellationRequested)
        {
            return false;
        }
    }

    internal static async Task EnsureRunningAsync(CancellationToken token)
    {
        if (await ProbeAsync(token))
        {
            return;
        }

        // Windows mutex ownership is thread-affine; keep acquisition and release on one worker.
        await Task.Run(() =>
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(StartupTimeout);
            using var startup = DaemonIdentity.CreateMutex(DaemonIdentity.StartupMutex);
            if (!DaemonIdentity.Acquire(startup, StartupLockTimeout))
            {
                throw new TimeoutException("CLI service startup lock timed out; no operation was sent.");
            }

            try
            {
                if (ProbeAsync(deadline.Token).GetAwaiter().GetResult())
                {
                    return;
                }

                Process? launched = null;
                try
                {
                    if (!DaemonIdentity.IsOwnerRunning())
                    {
                        launched = Process.Start(DaemonIdentity.CreateStartInfo())
                            ?? throw new IOException("CLI service could not be started.");
                    }

                    while (!deadline.IsCancellationRequested)
                    {
                        if (ProbeAsync(deadline.Token).GetAwaiter().GetResult())
                        {
                            return;
                        }

                        if (launched?.HasExited == true)
                        {
                            throw new IOException("CLI service exited during startup; no operation was sent.");
                        }

                        Task.Delay(100, deadline.Token).GetAwaiter().GetResult();
                    }

                    throw new TimeoutException("CLI service is unresponsive; no second owner was started.");
                }
                finally
                {
                    launched?.Dispose();
                }
            }
            finally
            {
                startup.ReleaseMutex();
            }
        }, token);
    }

    internal static async Task<DaemonResponse> StatusAsync(CancellationToken token)
    {
        try
        {
            return await SendAsync(new("status"), ProbeTimeout, token);
        }
        catch (IOException) when (!token.IsCancellationRequested)
        {
            var running = DaemonIdentity.IsOwnerRunning();
            return new(running ? 1 : 0, running ? """{"state":"unresponsive"}""" : """{"state":"stopped"}""");
        }
    }

    internal static Task<DaemonResponse> StopAsync(CancellationToken token) => Task.Run(() =>
    {
        using var startup = DaemonIdentity.CreateMutex(DaemonIdentity.StartupMutex);
        if (!DaemonIdentity.Acquire(startup, StartupLockTimeout))
        {
            throw new TimeoutException("CLI service startup/stop lock timed out.");
        }

        try
        {
            if (!DaemonIdentity.IsOwnerRunning())
            {
                return new DaemonResponse(0, """{"state":"stopped"}""");
            }

            var response = SendAsync(new("stop"), ControlTimeout, token).GetAwaiter().GetResult();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(ControlTimeout);
            while (DaemonIdentity.IsOwnerRunning())
            {
                Task.Delay(50, deadline.Token).GetAwaiter().GetResult();
            }

            return response;
        }
        finally
        {
            startup.ReleaseMutex();
        }
    }, token);
}
