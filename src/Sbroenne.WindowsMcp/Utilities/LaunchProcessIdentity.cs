using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sbroenne.WindowsMcp.Utilities;

/// <summary>Process lifetime and executable identity, independent of window titles and PID reuse.</summary>
internal sealed partial record LaunchProcessIdentity(int ProcessId, DateTime StartTimeUtc, string ExecutablePath)
{
    internal static string? ExplicitExecutablePath(string programPath)
    {
        // Do not guess ShellExecute's name lookup or resolve Store execution aliases as normal files.
        return Path.IsPathFullyQualified(programPath) &&
            string.Equals(Path.GetExtension(programPath), ".exe", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(programPath) &&
            (File.GetAttributes(programPath) & FileAttributes.ReparsePoint) == 0
                ? Path.GetFullPath(programPath)
                : null;
    }

    internal static LaunchProcessIdentity? TryRead(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return TryRead(process);
        }
        catch (ArgumentException ex)
        {
            Trace.TraceWarning("Launch observation lost process {0}: {1}", processId, ex.Message);
            return null;
        }
    }

    internal static unsafe LaunchProcessIdentity? TryRead(Process process)
    {
        try
        {
            // Query the actual image rather than assuming process names uniquely identify executables.
            var buffer = new char[32768];
            var size = (uint)buffer.Length;
            fixed (char* path = buffer)
            {
                if (!QueryFullProcessImageName(process.SafeHandle, 0, path, ref size))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
            }
            return new(process.Id, process.StartTime.ToUniversalTime(), new string(buffer, 0, (int)size));
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Trace.TraceWarning("Launch process identity unavailable: {0}", ex.Message);
            return null;
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool QueryFullProcessImageName(
        SafeProcessHandle process, uint flags, char* executableName, ref uint size);
}
