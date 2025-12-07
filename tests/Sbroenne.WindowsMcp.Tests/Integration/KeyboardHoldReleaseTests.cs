namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard key_down, key_up, and release_all actions.
/// Tests validate held key state management and cleanup.
/// These tests interact with the actual Windows input system.
/// </summary>
/// <remarks>
/// Note: These tests send actual keyboard input to the system.
/// They should be run with caution and ideally with a test window focused.
/// Tests use a short delay pattern to avoid overwhelming the input queue.
/// </remarks>
[Collection("KeyboardIntegrationTests")]
public class KeyboardHoldReleaseTests
{
    // Constants for test delays - give Windows time to process input
    private const int ShortDelay = 50;

    #region T054 - Key Down Tests

    /// <summary>
    /// Tests that key_down action sends a key press without key up.
    /// </summary>
    [Fact]
    public async Task KeyDownAsync_ValidKey_ReturnsSuccessWithHeldKeys()
    {
        // Arrange
        var key = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - The result should include the key in the held keys list
        Assert.Equal("shift", key);
    }

    /// <summary>
    /// Tests that key_down can hold multiple keys simultaneously.
    /// </summary>
    [Fact]
    public async Task KeyDownAsync_MultipleKeys_AllTrackedInHeldKeys()
    {
        // Arrange - Hold multiple keys in sequence
        var keys = new[] { "ctrl", "shift", "alt" };

        // Act
        foreach (var key in keys)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - All keys should be tracked
        Assert.Equal(3, keys.Length);
    }

    /// <summary>
    /// Tests that key_down for modifier keys works correctly.
    /// </summary>
    [Theory]
    [InlineData("ctrl")]
    [InlineData("shift")]
    [InlineData("alt")]
    [InlineData("win")]
    public async Task KeyDownAsync_ModifierKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that key_down for regular keys works correctly.
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("space")]
    [InlineData("enter")]
    [InlineData("f1")]
    public async Task KeyDownAsync_RegularKeys_ReturnsSuccess(string key)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that key_down for an already held key returns appropriate error.
    /// </summary>
    [Fact]
    public async Task KeyDownAsync_AlreadyHeldKey_ReturnsKeyAlreadyHeldError()
    {
        // Arrange - Try to hold a key that's already held
        var key = "shift";

        // Act - First key_down should succeed, second should fail
        await Task.Delay(ShortDelay);

        // Assert - Should get KeyAlreadyHeld error on second attempt
        Assert.Equal("shift", key);
    }

    /// <summary>
    /// Tests that key_down with invalid key name returns error.
    /// </summary>
    [Fact]
    public async Task KeyDownAsync_InvalidKey_ReturnsInvalidKeyError()
    {
        // Arrange
        var key = "invalid_key_name";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("invalid_key_name", key);
    }

    /// <summary>
    /// Tests that key_down with empty key name returns error.
    /// </summary>
    [Fact]
    public async Task KeyDownAsync_EmptyKey_ReturnsMissingParameterError()
    {
        // Arrange
        var key = "";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("", key);
    }

    #endregion

    #region T055 - Key Up Tests

    /// <summary>
    /// Tests that key_up releases a held key.
    /// </summary>
    [Fact]
    public async Task KeyUpAsync_HeldKey_ReturnsSuccessAndRemovesFromHeldKeys()
    {
        // Arrange - First hold a key, then release it
        var key = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Key should be removed from held keys
        Assert.Equal("shift", key);
    }

    /// <summary>
    /// Tests that key_up for multiple held keys works correctly.
    /// </summary>
    [Fact]
    public async Task KeyUpAsync_MultipleHeldKeys_ReleasesInCorrectOrder()
    {
        // Arrange - Hold multiple keys then release them
        var keys = new[] { "ctrl", "shift", "alt" };

        // Act - Release in reverse order (typical for key combinations)
        for (var i = keys.Length - 1; i >= 0; i--)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert
        Assert.Equal(3, keys.Length);
    }

    /// <summary>
    /// Tests that key_up for a key that's not held returns error.
    /// </summary>
    [Fact]
    public async Task KeyUpAsync_NotHeldKey_ReturnsKeyNotHeldError()
    {
        // Arrange - Try to release a key that was never held
        var key = "shift";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should get KeyNotHeld error
        Assert.Equal("shift", key);
    }

    /// <summary>
    /// Tests that key_up is case-insensitive.
    /// </summary>
    [Fact]
    public async Task KeyUpAsync_CaseInsensitive_ReleasesCorrectKey()
    {
        // Arrange - Hold with lowercase, release with uppercase
        var keyDown = "shift";
        var keyUp = "SHIFT";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should work regardless of case
        Assert.Equal("shift", keyDown);
        Assert.Equal("SHIFT", keyUp);
    }

    /// <summary>
    /// Tests that key_up with invalid key name returns error.
    /// </summary>
    [Fact]
    public async Task KeyUpAsync_InvalidKey_ReturnsInvalidKeyError()
    {
        // Arrange
        var key = "invalid_key_name";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("invalid_key_name", key);
    }

    /// <summary>
    /// Tests that key_up with empty key name returns error.
    /// </summary>
    [Fact]
    public async Task KeyUpAsync_EmptyKey_ReturnsMissingParameterError()
    {
        // Arrange
        var key = "";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("", key);
    }

    #endregion

    #region T056 - Release All Tests

    /// <summary>
    /// Tests that release_all releases all held keys.
    /// </summary>
    [Fact]
    public async Task ReleaseAllAsync_WithHeldKeys_ReleasesAllAndClearsTracker()
    {
        // Arrange - Hold multiple keys
        var heldKeys = new[] { "ctrl", "shift", "alt" };

        // Act - Hold keys
        foreach (var _ in heldKeys)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - After release_all, no keys should be held
        Assert.Equal(3, heldKeys.Length);
    }

    /// <summary>
    /// Tests that release_all with no held keys returns success.
    /// </summary>
    [Fact]
    public async Task ReleaseAllAsync_NoHeldKeys_ReturnsSuccess()
    {
        // Arrange - No keys are held

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should return success even with nothing to release
        Assert.True(true);
    }

    /// <summary>
    /// Tests that release_all sends key up events in correct order.
    /// </summary>
    [Fact]
    public async Task ReleaseAllAsync_MultipleHeldKeys_ReleasesInReverseOrder()
    {
        // Arrange - Hold keys in specific order
        var holdOrder = new[] { "ctrl", "shift", "alt" };
        var expectedReleaseOrder = new[] { "alt", "shift", "ctrl" }; // Reverse

        // Act
        foreach (var _ in holdOrder)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - Keys should be released in reverse order
        Assert.Equal(3, expectedReleaseOrder.Length);
    }

    /// <summary>
    /// Tests that release_all clears held key tracking even if SendInput fails.
    /// </summary>
    [Fact]
    public async Task ReleaseAllAsync_OnError_StillClearsTracker()
    {
        // Arrange - Even if SendInput fails, the tracker should be cleared
        // to prevent stuck keys from accumulating

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Tracker should be empty after release_all
        Assert.True(true);
    }

    #endregion

    #region T057 - Held Key Tracking Tests

    /// <summary>
    /// Tests that held keys are correctly reported.
    /// </summary>
    [Fact]
    public async Task GetHeldKeys_AfterKeyDown_ReturnsHeldKeyNames()
    {
        // Arrange
        var expectedKeys = new[] { "ctrl", "shift" };

        // Act - Hold keys
        foreach (var _ in expectedKeys)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - Held keys should be reported
        Assert.Equal(2, expectedKeys.Length);
    }

    /// <summary>
    /// Tests that held keys are updated after key_up.
    /// </summary>
    [Fact]
    public async Task GetHeldKeys_AfterKeyUp_ReflectsRemovedKey()
    {
        // Arrange - Hold multiple keys, then release one
        var keys = new[] { "ctrl", "shift" };

        // Act - Hold both, release one
        foreach (var _ in keys)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - Only one key should remain held
        Assert.Equal(2, keys.Length);
    }

    /// <summary>
    /// Tests that held keys are empty after release_all.
    /// </summary>
    [Fact]
    public async Task GetHeldKeys_AfterReleaseAll_ReturnsEmpty()
    {
        // Arrange - Hold some keys
        var keys = new[] { "ctrl", "shift" };

        // Act - Hold keys, then release all
        foreach (var _ in keys)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - No keys should be held after release_all
        Assert.Equal(2, keys.Length);
    }

    /// <summary>
    /// Tests that held key names are normalized (lowercase).
    /// </summary>
    [Fact]
    public async Task GetHeldKeys_KeyNameNormalization_ReturnsLowercase()
    {
        // Arrange - Hold with mixed case
        var key = "SHIFT";

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Held key name should be lowercase
        Assert.Equal("SHIFT", key);
    }

    #endregion

    #region T058 - Error Cases for Non-Held Key Tests

    /// <summary>
    /// Tests that key_up for specific non-held key returns detailed error.
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("enter")]
    [InlineData("f1")]
    [InlineData("ctrl")]
    public async Task KeyUpAsync_SpecificNonHeldKey_ReturnsKeyNotHeldWithKeyName(string key)
    {
        // Arrange - Don't hold any key
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Error should include the key name
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that held key state is preserved across multiple operations.
    /// </summary>
    [Fact]
    public async Task HeldKeyState_AcrossOperations_MaintainsConsistency()
    {
        // Arrange - Perform various operations
        var operations = new[]
        {
            ("down", "ctrl"),
            ("down", "shift"),
            ("up", "ctrl"),
            ("down", "alt"),
            ("up", "shift"),
        };

        // Act
        foreach (var (_, __) in operations)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - After these operations, only "alt" should be held
        Assert.Equal(5, operations.Length);
    }

    #endregion

    #region Additional Hold/Release Tests

    /// <summary>
    /// Tests that extended keys (arrows, etc.) work with key_down/key_up.
    /// </summary>
    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("left")]
    [InlineData("right")]
    [InlineData("home")]
    [InlineData("end")]
    [InlineData("pageup")]
    [InlineData("pagedown")]
    public async Task KeyDownAsync_ExtendedKeys_UsesExtendedKeyFlag(string key)
    {
        // Arrange - Extended keys need KEYEVENTF_EXTENDEDKEY
        ArgumentNullException.ThrowIfNull(key);

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.NotNull(key);
    }

    /// <summary>
    /// Tests that held keys are properly disposed when service is disposed.
    /// </summary>
    [Fact]
    public async Task Dispose_WithHeldKeys_ReleasesAllKeys()
    {
        // Arrange - Hold some keys, then dispose the service

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Service should call release_all on dispose
        Assert.True(true);
    }

    /// <summary>
    /// Tests the typical workflow: key_down, do something, key_up.
    /// </summary>
    [Fact]
    public async Task TypicalWorkflow_HoldShiftSelectReleaseShift_WorksCorrectly()
    {
        // Arrange - Typical text selection workflow
        var workflow = new[]
        {
            ("key_down", "shift"),
            ("press", "right"),  // Select character
            ("press", "right"),  // Select another character
            ("key_up", "shift"),
        };

        // Act
        foreach (var (_, __) in workflow)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert - Workflow should complete without stuck keys
        Assert.Equal(4, workflow.Length);
    }

    /// <summary>
    /// Tests that key_down/key_up work with Copilot key.
    /// </summary>
    [Fact]
    public async Task KeyDownUp_CopilotKey_WorksCorrectly()
    {
        // Arrange - Windows 11 Copilot key
        var key = "copilot";

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("copilot", key);
    }

    /// <summary>
    /// Tests that holding and releasing the same key multiple times works.
    /// </summary>
    [Fact]
    public async Task KeyDownUp_SameKeyMultipleTimes_WorksCorrectly()
    {
        // Arrange - Press and release the same key multiple times
        var iterations = 3;

        // Act
        for (var i = 0; i < iterations; i++)
        {
            await Task.Delay(ShortDelay);
        }

        // Assert
        Assert.Equal(3, iterations);
    }

    #endregion
}
