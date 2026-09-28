using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Cli.Service;

/// <summary>One CLI owner per installation/build, Windows logon, and elevation level.</summary>
internal static class DaemonIdentity
{
    internal const int ProtocolVersion = 1;
    internal static readonly string Build = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{typeof(DaemonIdentity).Assembly.ManifestModule.ModuleVersionId:N}|" +
        $"{typeof(WindowsToolsBase).Assembly.ManifestModule.ModuleVersionId:N}")))[..32];
    internal static readonly string Installation = Path.GetFullPath(
        string.IsNullOrEmpty(typeof(DaemonIdentity).Assembly.Location)
            ? Environment.ProcessPath!
            : typeof(DaemonIdentity).Assembly.Location).ToUpperInvariant();
    internal static readonly string PipeName = CreatePipeName();

    internal static string OwnerMutex => $"Local\\{PipeName}-owner";
    internal static string StartupMutex => $"Local\\{PipeName}-startup";

    internal static string LogonSid
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            // WindowsIdentity.Groups filters logon SIDs on some CloudAP tokens.
            // TokenLogonSid returns the actual logon group used for the pipe ACL.
            _ = GetTokenInformation(identity.AccessToken, 28, IntPtr.Zero, 0, out var length);
            if (length <= 0 || length > 4096)
            {
                throw new InvalidOperationException("Cannot determine the Windows logon identity.");
            }

            var buffer = Marshal.AllocHGlobal(length);
            try
            {
                if (!GetTokenInformation(identity.AccessToken, 28, buffer, length, out _)
                    || Marshal.ReadInt32(buffer) != 1)
                {
                    throw new InvalidOperationException("A Windows logon SID is required for secure CLI IPC.");
                }

                var sid = Marshal.ReadIntPtr(buffer, IntPtr.Size);
                return new SecurityIdentifier(sid).Value;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static string CreatePipeName()
    {
        using var identity = WindowsIdentity.GetCurrent();
        using var process = Process.GetCurrentProcess();
        var elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        var key = $"{identity.User?.Value}|{LogonSid}|{process.SessionId}|{elevated}|{Installation}|{Build}|{ProtocolVersion}";
        return $"wincli-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..40]}";
    }

    internal static ProcessStartInfo CreateStartInfo()
    {
        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot locate the CLI executable.");
        var info = new ProcessStartInfo(executable)
        {
            // ShellExecute starts an independent hidden process, without inheriting the
            // client's redirected handles (the same launch pattern as Excel's CLI daemon).
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            info.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
        }

        info.ArgumentList.Add("service");
        info.ArgumentList.Add("run");
        return info;
    }

    internal static Mutex CreateMutex(string name) =>
        new(false, name, new NamedWaitHandleOptions { CurrentUserOnly = true, CurrentSessionOnly = true });

    internal static bool Acquire(Mutex mutex, TimeSpan timeout)
    {
        try
        {
            return mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    internal static bool IsOwnerRunning()
    {
        using var mutex = CreateMutex(OwnerMutex);
        if (!Acquire(mutex, TimeSpan.Zero))
        {
            return true;
        }

        mutex.ReleaseMutex();
        return false;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(
        SafeAccessTokenHandle token,
        int informationClass,
        IntPtr information,
        int informationLength,
        out int returnLength);
}
