namespace Sbroenne.WindowsMcp.Models;

internal static class CompactAutomationId
{
    public static string? From(string? automationId)
    {
        var value = automationId?.Trim();
        return string.IsNullOrEmpty(value) || value.All(char.IsDigit)
            ? null
            : automationId;
    }
}
