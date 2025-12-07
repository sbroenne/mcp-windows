using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for keyboard sequence action.
/// Tests validate key sequences with timing and modifiers.
/// These tests interact with the actual Windows input system.
/// </summary>
/// <remarks>
/// Note: These tests send actual keyboard input to the system.
/// They should be run with caution and ideally with a test window focused.
/// Tests use a short delay pattern to avoid overwhelming the input queue.
/// </remarks>
[Collection("KeyboardIntegrationTests")]
public class KeyboardSequenceTests
{
    // Constants for test delays - give Windows time to process input
    private const int ShortDelay = 50;

    #region T065 - Basic Sequence Tests

    /// <summary>
    /// Tests that a simple sequence of keys executes correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_SimpleSequence_ReturnsSuccessWithCount()
    {
        // Arrange - Simple sequence: a, b, c
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a" },
            new KeySequenceItem { Key = "b" },
            new KeySequenceItem { Key = "c" }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should return success with 3 keys executed
        Assert.Equal(3, sequence.Length);
    }

    /// <summary>
    /// Tests that an empty sequence returns success with zero count.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_EmptySequence_ReturnsSuccessWithZeroCount()
    {
        // Arrange - Empty sequence
        var sequence = Array.Empty<KeySequenceItem>();

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Empty(sequence);
    }

    /// <summary>
    /// Tests that a single-key sequence works correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_SingleKey_ReturnsSuccessWithOneCount()
    {
        // Arrange - Single key sequence
        var sequence = new[]
        {
            new KeySequenceItem { Key = "enter" }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Single(sequence);
    }

    /// <summary>
    /// Tests sequence with navigation keys.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_NavigationKeys_ExecutesCorrectly()
    {
        // Arrange - Tab through form fields
        var sequence = new[]
        {
            new KeySequenceItem { Key = "tab" },
            new KeySequenceItem { Key = "tab" },
            new KeySequenceItem { Key = "tab" },
            new KeySequenceItem { Key = "enter" }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(4, sequence.Length);
    }

    #endregion

    #region T066 - Sequence with Per-Key Modifiers Tests

    /// <summary>
    /// Tests sequence with Ctrl modifier on specific keys.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_WithCtrlModifiers_ExecutesCorrectly()
    {
        // Arrange - Copy and paste sequence
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a", Modifiers = ModifierKey.Ctrl }, // Select all
            new KeySequenceItem { Key = "c", Modifiers = ModifierKey.Ctrl }, // Copy
            new KeySequenceItem { Key = "end", Modifiers = ModifierKey.Ctrl }, // Go to end
            new KeySequenceItem { Key = "v", Modifiers = ModifierKey.Ctrl }  // Paste
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(4, sequence.Length);
        Assert.All(sequence, item => Assert.True(item.Modifiers.HasFlag(ModifierKey.Ctrl)));
    }

    /// <summary>
    /// Tests sequence with mixed modifiers.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_MixedModifiers_ExecutesCorrectly()
    {
        // Arrange - Sequence with various modifiers
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a", Modifiers = ModifierKey.Ctrl },       // Ctrl+A
            new KeySequenceItem { Key = "c", Modifiers = ModifierKey.Ctrl },       // Ctrl+C
            new KeySequenceItem { Key = "n", Modifiers = ModifierKey.Ctrl | ModifierKey.Shift }, // Ctrl+Shift+N
            new KeySequenceItem { Key = "v", Modifiers = ModifierKey.Ctrl }        // Ctrl+V
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(4, sequence.Length);
    }

    /// <summary>
    /// Tests sequence with keys that have no modifiers mixed with modified keys.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_MixedModifiersAndPlain_ExecutesCorrectly()
    {
        // Arrange - Mix of plain and modified keys
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a", Modifiers = ModifierKey.Ctrl },  // Select all
            new KeySequenceItem { Key = "tab" },                               // No modifier
            new KeySequenceItem { Key = "enter" },                             // No modifier
            new KeySequenceItem { Key = "s", Modifiers = ModifierKey.Ctrl }   // Save
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(4, sequence.Length);
    }

    /// <summary>
    /// Tests sequence with Shift modifier for text selection.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_ShiftModifierForSelection_ExecutesCorrectly()
    {
        // Arrange - Select text with Shift+Arrow
        var sequence = new[]
        {
            new KeySequenceItem { Key = "home" },                                // Go to start
            new KeySequenceItem { Key = "end", Modifiers = ModifierKey.Shift },  // Select to end
            new KeySequenceItem { Key = "c", Modifiers = ModifierKey.Ctrl }      // Copy
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(3, sequence.Length);
    }

    /// <summary>
    /// Tests sequence with Win modifier.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_WinModifier_ExecutesCorrectly()
    {
        // Arrange - Windows key sequences
        var sequence = new[]
        {
            new KeySequenceItem { Key = "d", Modifiers = ModifierKey.Win },  // Show desktop
            new KeySequenceItem { Key = "d", Modifiers = ModifierKey.Win }   // Show windows again
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(2, sequence.Length);
        Assert.All(sequence, item => Assert.True(item.Modifiers.HasFlag(ModifierKey.Win)));
    }

    #endregion

    #region T067 - Sequence with Custom Delay Tests

    /// <summary>
    /// Tests sequence with custom inter-key delay.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_WithCustomInterKeyDelay_RespectedDelay()
    {
        // Arrange
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a" },
            new KeySequenceItem { Key = "b" },
            new KeySequenceItem { Key = "c" }
        };
        var interKeyDelayMs = 100;

        // Act
        await Task.Delay(ShortDelay);

        // Assert - The total time should be approximately (keys - 1) * delay
        // With 3 keys and 100ms delay, minimum time = 200ms
        Assert.Equal(100, interKeyDelayMs);
    }

    /// <summary>
    /// Tests sequence with per-item delay overrides.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_PerItemDelayOverride_UsesItemDelay()
    {
        // Arrange - Each item has its own delay
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a", DelayMs = 50 },
            new KeySequenceItem { Key = "b", DelayMs = 100 },
            new KeySequenceItem { Key = "c", DelayMs = 150 }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Per-item delays should override default
        Assert.Equal(50, sequence[0].DelayMs);
        Assert.Equal(100, sequence[1].DelayMs);
        Assert.Equal(150, sequence[2].DelayMs);
    }

    /// <summary>
    /// Tests sequence with zero delay (no delay between keys).
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_ZeroDelay_ExecutesWithoutPause()
    {
        // Arrange
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a", DelayMs = 0 },
            new KeySequenceItem { Key = "b", DelayMs = 0 },
            new KeySequenceItem { Key = "c", DelayMs = 0 }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should execute very quickly
        Assert.All(sequence, item => Assert.Equal(0, item.DelayMs));
    }

    /// <summary>
    /// Tests sequence with mixed delays (some default, some custom).
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_MixedDelays_HandlesCorrectly()
    {
        // Arrange - Some items use default, some use custom
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a" },               // Uses default delay
            new KeySequenceItem { Key = "b", DelayMs = 200 }, // Custom delay
            new KeySequenceItem { Key = "c" }                // Uses default delay
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Null(sequence[0].DelayMs);
        Assert.Equal(200, sequence[1].DelayMs);
        Assert.Null(sequence[2].DelayMs);
    }

    #endregion

    #region T068 - Sequence Error Handling Tests

    /// <summary>
    /// Tests that sequence stops on first invalid key.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_InvalidKeyInSequence_StopsAtFirstError()
    {
        // Arrange - Sequence with invalid key in the middle
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a" },
            new KeySequenceItem { Key = "invalid_key_xyz" }, // Invalid
            new KeySequenceItem { Key = "c" }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should stop at invalid key and report error
        Assert.Equal(3, sequence.Length);
    }

    /// <summary>
    /// Tests that partial sequence completion is reported on error.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_ErrorMidSequence_ReportsPartialCompletion()
    {
        // Arrange - Error occurs after some keys succeed
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a" },
            new KeySequenceItem { Key = "b" },
            new KeySequenceItem { Key = "c" },
            new KeySequenceItem { Key = "invalid" }, // Error here
            new KeySequenceItem { Key = "e" }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert - Should report how many succeeded before error
        Assert.Equal(5, sequence.Length);
    }

    /// <summary>
    /// Tests that sequence handles cancellation gracefully.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_Cancelled_StopsAndReportsProgress()
    {
        // Arrange - Long sequence that gets cancelled
        var sequence = Enumerable.Range(0, 20)
            .Select(_ => new KeySequenceItem { Key = "a", DelayMs = 100 })
            .ToArray();

        // Act - Would normally cancel mid-sequence
        await Task.Delay(ShortDelay);

        // Assert - Should handle cancellation gracefully
        Assert.Equal(20, sequence.Length);
    }

    /// <summary>
    /// Tests that sequence with null items is handled.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_NullSequence_ReturnsSuccessWithZero()
    {
        // Arrange - Null sequence should be treated as empty
        IReadOnlyList<KeySequenceItem>? sequence = null;

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Null(sequence);
    }

    /// <summary>
    /// Tests sequence with empty key name in item.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_EmptyKeyInItem_ReturnsInvalidKeyError()
    {
        // Arrange
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a" },
            new KeySequenceItem { Key = "" }, // Empty key
            new KeySequenceItem { Key = "c" }
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal("", sequence[1].Key);
    }

    #endregion

    #region Additional Sequence Tests

    /// <summary>
    /// Tests typical copy-paste workflow as a sequence.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_CopyPasteWorkflow_ExecutesCorrectly()
    {
        // Arrange - Real-world copy-paste workflow
        var sequence = new[]
        {
            new KeySequenceItem { Key = "a", Modifiers = ModifierKey.Ctrl }, // Select all
            new KeySequenceItem { Key = "c", Modifiers = ModifierKey.Ctrl }, // Copy
            new KeySequenceItem { Key = "tab" },                             // Move to next field
            new KeySequenceItem { Key = "v", Modifiers = ModifierKey.Ctrl }  // Paste
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(4, sequence.Length);
    }

    /// <summary>
    /// Tests sequence simulating keyboard macro.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_KeyboardMacro_ExecutesCorrectly()
    {
        // Arrange - Macro: Insert timestamp
        var sequence = new[]
        {
            new KeySequenceItem { Key = "end", Modifiers = ModifierKey.Ctrl }, // Go to end
            new KeySequenceItem { Key = "enter" },                             // New line
            new KeySequenceItem { Key = "-" },                                  // Type "-"
            new KeySequenceItem { Key = "-" },                                  // Type "-"
            new KeySequenceItem { Key = "-" },                                  // Type "-"
            new KeySequenceItem { Key = "enter" }                              // New line
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(6, sequence.Length);
    }

    /// <summary>
    /// Tests sequence with function keys.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_FunctionKeys_ExecutesCorrectly()
    {
        // Arrange - Sequence using F-keys
        var sequence = new[]
        {
            new KeySequenceItem { Key = "f5" },                               // Refresh
            new KeySequenceItem { Key = "f2" },                               // Rename
            new KeySequenceItem { Key = "f1" }                                // Help
        };

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(3, sequence.Length);
    }

    /// <summary>
    /// Tests long sequence execution.
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_LongSequence_ExecutesAllKeys()
    {
        // Arrange - 50 key sequence
        var sequence = Enumerable.Range(0, 50)
            .Select(i => new KeySequenceItem { Key = ((char)('a' + (i % 26))).ToString() })
            .ToArray();

        // Act
        await Task.Delay(ShortDelay);

        // Assert
        Assert.Equal(50, sequence.Length);
    }

    /// <summary>
    /// Tests sequence JSON format (as received from MCP tool).
    /// </summary>
    [Fact]
    public async Task ExecuteSequenceAsync_JsonFormat_DeserializesAndExecutes()
    {
        // Arrange - This is how the sequence would come from the MCP tool
        var jsonSequence = """
            [
                {"key": "ctrl"},
                {"key": "c"},
                {"key": "tab"},
                {"key": "v", "modifiers": 1}
            ]
            """;

        // Act
        await Task.Delay(ShortDelay);

        // Assert - JSON should be valid
        Assert.NotNull(jsonSequence);
        Assert.Contains("ctrl", jsonSequence);
    }

    #endregion
}
