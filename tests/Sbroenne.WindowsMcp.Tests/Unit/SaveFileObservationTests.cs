using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class SaveFileObservationTests
{
    [Fact]
    public void ExistingUnchangedFile_DoesNotConfirmSave()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "already saved");
            var before = SaveFileObservation.Read(path);

            Assert.NotNull(before);
            Assert.False(SaveFileObservation.HasChanged(path, before));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NewFile_ConfirmsCreationButNotDeletion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"save-observation-{Guid.NewGuid():N}.txt");
        try
        {
            var before = SaveFileObservation.Read(path);
            Assert.Null(before);
            Assert.False(SaveFileObservation.HasChanged(path, before));

            File.WriteAllText(path, "saved");
            Assert.True(SaveFileObservation.HasChanged(path, before));
            var created = SaveFileObservation.Read(path);
            File.Delete(path);
            Assert.False(SaveFileObservation.HasChanged(path, created));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedMetadata_ConfirmsAnObservableWrite(bool changeLength)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "saved");
            var before = SaveFileObservation.Read(path);
            var previousWrite = File.GetLastWriteTimeUtc(path);
            if (changeLength)
            {
                File.AppendAllText(path, " again");
                File.SetLastWriteTimeUtc(path, previousWrite);
            }
            else
            {
                File.SetLastWriteTimeUtc(path, previousWrite.AddSeconds(2));
            }

            Assert.True(SaveFileObservation.HasChanged(path, before));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
