namespace Sbroenne.WindowsMcp.Automation;

internal readonly record struct SaveFileObservation(DateTime CreationTimeUtc, DateTime LastWriteTimeUtc, long Length)
{
    internal static SaveFileObservation? Read(string filePath)
    {
        var file = new FileInfo(filePath);
        file.Refresh();
        return file.Exists
            ? new SaveFileObservation(file.CreationTimeUtc, file.LastWriteTimeUtc, file.Length)
            : null;
    }

    internal static bool HasChanged(string filePath, SaveFileObservation? previous)
    {
        var current = Read(filePath);
        return current.HasValue && current != previous;
    }
}
