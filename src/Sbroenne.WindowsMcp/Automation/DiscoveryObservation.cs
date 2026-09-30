using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>Observes a requested PowerPoint window while its lazy document provider is queried.</summary>
internal sealed class DiscoveryObservation : UIA.IUIAutomationStructureChangedEventHandler, IDisposable
{
    private readonly UIA.IUIAutomation _automation;
    private readonly UIA.IUIAutomationElement _root;
    private bool _disposed;

    private DiscoveryObservation(UIA.IUIAutomation automation, UIA.IUIAutomationElement root)
    {
        _automation = automation;
        _root = root;
        lock (UIA3Automation.EventSubscriptionLock)
        {
            automation.AddStructureChangedEventHandler(root, UIA.TreeScope.TreeScope_Subtree, null, this);
        }
    }

    internal static DiscoveryObservation? Create(UIA.IUIAutomation automation, UIA.IUIAutomationElement root) =>
        string.Equals(root.CurrentClassName, "PPTFrameClass", StringComparison.Ordinal)
            ? new DiscoveryObservation(automation, root)
            : null;

    public void HandleStructureChangedEvent(UIA.IUIAutomationElement sender, UIA.StructureChangeType changeType, int[] runtimeId)
    {
        // PowerPoint exposes slide content when a client subscribes; callbacks need no further queries.
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            lock (UIA3Automation.EventSubscriptionLock)
            {
                _automation.RemoveStructureChangedEventHandler(_root, this);
            }
        }
    }
}
