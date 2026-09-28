using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Native;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>Owns bounded, immutable references to observed UI Automation elements.</summary>
[SupportedOSPlatform("windows")]
public static class ElementIdGenerator
{
    internal const int MaxRetainedIds = 4096;

    private static readonly Lock s_lock = new();
    private static readonly Dictionary<string, Registration> s_registrations = new(StringComparer.Ordinal);
    private static readonly Dictionary<Identity, string> s_ids = [];
    private static readonly Queue<string> s_order = new();
    private static string s_generation = NewGeneration();
    private static long s_counter;

    internal static int RetainedIdCount
    {
        get { lock (s_lock) { return s_registrations.Count; } }
    }

    /// <summary>Registers a live element using its current runtime identity.</summary>
    public static string GenerateId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement) =>
        Generate(element, rootElement, cached: false);

    /// <summary>Registers a live element using cached identity properties when available.</summary>
    public static string GenerateFastId(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement) =>
        Generate(element, rootElement, cached: true);

    /// <summary>Registers a live element when no property cache was requested.</summary>
    public static string GenerateFastIdFromCurrent(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement) =>
        Generate(element, rootElement, cached: false);

    private static string Generate(UIA.IUIAutomationElement element, UIA.IUIAutomationElement rootElement, bool cached)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(rootElement);
        try
        {
            nint handle;
            try
            {
                handle = cached ? rootElement.GetCachedNativeWindowHandle() : rootElement.GetNativeWindowHandle();
            }
            catch (Exception ex) when (COMExceptionHelper.IsExpectedElementFailure(ex))
            {
                handle = rootElement.GetNativeWindowHandle();
            }
            if (handle == nint.Zero)
            {
                handle = GetTopLevelWindowHandle(element);
            }
            else
            {
                handle = NativeMethods.GetAncestor(handle, NativeConstants.GA_ROOT);
            }

            int[]? runtimeId;
            int providerProcessId;
            try
            {
                runtimeId = cached
                    ? (int[]?)element.GetCachedPropertyValue(UIA3PropertyIds.RuntimeId)
                    : element.GetRuntimeId();
                providerProcessId = cached
                    ? (int)element.GetCachedPropertyValue(UIA3PropertyIds.ProcessId)
                    : element.CurrentProcessId;
            }
            catch (Exception ex) when (COMExceptionHelper.IsExpectedElementFailure(ex))
            {
                runtimeId = element.GetRuntimeId();
                providerProcessId = element.CurrentProcessId;
            }

            var runtime = runtimeId is { Length: > 0 } ? string.Join(".", runtimeId) : "0";
            return Register($"window:{handle}|runtime:{runtime}|path:observed", element, providerProcessId);
        }
        catch (Exception ex) when (COMExceptionHelper.IsExpectedElementFailure(ex))
        {
            // A disappearing element can still be displayed, but its reference cannot resolve.
            return Register("window:0|runtime:0|path:stale", null);
        }
    }

    /// <summary>Resolves only a reference issued by this owner, never a name or a tree position.</summary>
    public static UIA.IUIAutomationElement? ResolveToAutomationElement(string elementId)
    {
        ArgumentNullException.ThrowIfNull(elementId);
        Registration? registration;
        lock (s_lock)
        {
            s_registrations.TryGetValue(elementId, out registration);
        }

        if (registration is null)
        {
            return null;
        }

        var identity = registration.Identity;
        if (identity.ProcessStartTicks <= 0 || identity.ProviderStartTicks <= 0 || identity.RuntimeId == "0" ||
            GetProcessLifetime(identity.WindowHandle) != (identity.ProcessId, identity.ProcessStartTicks) ||
            GetProcessLifetime(identity.ProviderProcessId) != (identity.ProviderProcessId, identity.ProviderStartTicks))
        {
            Retire(elementId);
            return null;
        }

        try
        {
            // Keep the original provider object: searching for a reused runtime ID can retarget.
            var element = registration.Element;
            if (element is not null &&
                element.CurrentProcessId == identity.ProviderProcessId &&
                string.Equals(string.Join(".", element.GetRuntimeId() ?? []), identity.RuntimeId, StringComparison.Ordinal) &&
                GetTopLevelWindowHandle(element) == identity.WindowHandle)
            {
                return element;
            }
        }
        catch (COMException ex) when (COMExceptionHelper.IsExpectedElementFailure(ex))
        {
            // The original provider no longer exposes this observation.
        }

        Retire(elementId);
        return null;
    }

    private static nint GetTopLevelWindowHandle(UIA.IUIAutomationElement element)
    {
        var current = element;
        var desktop = UIA3Automation.Instance.RootElement;
        while (current is not null && !current.IsSameElement(desktop))
        {
            var currentHandle = current.GetNativeWindowHandle();
            if (currentHandle != nint.Zero)
            {
                return NativeMethods.GetAncestor(currentHandle, NativeConstants.GA_ROOT);
            }

            current = current.GetParent();
        }

        return nint.Zero;
    }

    internal static string RegisterFullId(string fullId) => Register(fullId, null);

    private static string Register(string fullId, UIA.IUIAutomationElement? element, int providerProcessId = 0)
    {
        ArgumentNullException.ThrowIfNull(fullId);
        var segments = fullId.Split('|');
        var window = segments.FirstOrDefault(part => part.StartsWith("window:", StringComparison.Ordinal));
        var runtime = segments.FirstOrDefault(part => part.StartsWith("runtime:", StringComparison.Ordinal));
        _ = nint.TryParse(window?["window:".Length..], out var handle);
        var (processId, startTicks) = GetProcessLifetime(handle);
        var provider = providerProcessId == processId
            ? (ProcessId: processId, StartTicks: startTicks)
            : GetProcessLifetime(providerProcessId);
        var identity = new Identity(handle, processId, startTicks, provider.ProcessId, provider.StartTicks, runtime?["runtime:".Length..] ?? "0");
        lock (s_lock)
        {
            if (identity.RuntimeId != "0" && s_ids.TryGetValue(identity, out var existing))
            {
                return existing;
            }

            var id = $"{s_generation}.{(++s_counter).ToString("x", CultureInfo.InvariantCulture)}";
            s_registrations.Add(id, new Registration(identity, fullId, element, Environment.CurrentManagedThreadId));
            s_ids[identity] = id;
            s_order.Enqueue(id);
            while (s_order.Count > MaxRetainedIds)
            {
                Retire(s_order.Dequeue());
            }

            return id;
        }
    }

    private static (int ProcessId, long StartTicks) GetProcessLifetime(nint handle)
    {
        if (handle == nint.Zero || NativeMethods.GetWindowThreadProcessId(handle, out var nativeId) == 0)
        {
            return default;
        }

        return GetProcessLifetime(checked((int)nativeId));
    }

    private static (int ProcessId, long StartTicks) GetProcessLifetime(int processId)
    {
        if (processId <= 0)
        {
            return default;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return (process.Id, process.StartTime.ToUniversalTime().Ticks);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return default;
        }
    }

    private static void Retire(string id)
    {
        lock (s_lock)
        {
            if (s_registrations.Remove(id, out var registration) &&
                s_ids.TryGetValue(registration.Identity, out var current) && current == id)
            {
                s_ids.Remove(registration.Identity);
            }
        }
    }

    internal static string? ResolveFullId(string elementId)
    {
        ArgumentNullException.ThrowIfNull(elementId);
        lock (s_lock)
        {
            return s_registrations.TryGetValue(elementId, out var registration) ? registration.FullId : null;
        }
    }

    /// <summary>Returns the recorded window for an issued reference, never for a raw internal ID.</summary>
    public static bool TryResolveWindowHandle(string elementId, out nint windowHandle)
    {
        windowHandle = nint.Zero;
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return false;
        }

        lock (s_lock)
        {
            if (s_registrations.TryGetValue(elementId, out var registration))
            {
                windowHandle = registration.Identity.WindowHandle;
            }
        }

        return windowHandle != nint.Zero;
    }

    internal static void Clear()
    {
        lock (s_lock)
        {
            s_registrations.Clear();
            s_ids.Clear();
            s_order.Clear();
            s_generation = NewGeneration();
            s_counter = 0;
        }
    }

    internal static void RetireCurrentThread()
    {
        lock (s_lock)
        {
            foreach (var id in s_registrations
                         .Where(pair => pair.Value.ThreadId == Environment.CurrentManagedThreadId)
                         .Select(pair => pair.Key).ToArray())
            {
                Retire(id);
            }
        }
    }

    private static string NewGeneration() =>
        "e" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record Registration(Identity Identity, string FullId, UIA.IUIAutomationElement? Element, int ThreadId);
    private readonly record struct Identity(
        nint WindowHandle, int ProcessId, long ProcessStartTicks,
        int ProviderProcessId, long ProviderStartTicks, string RuntimeId);
}
