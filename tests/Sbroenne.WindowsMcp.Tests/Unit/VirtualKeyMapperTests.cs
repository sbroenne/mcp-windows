using Sbroenne.WindowsMcp.Input;

namespace Sbroenne.WindowsMcp.Tests.Unit;

// Virtual key code constants for testing (mirrors internal NativeConstants)
internal static class VK
{
    public const int VK_RETURN = 0x0D;
    public const int VK_TAB = 0x09;
    public const int VK_ESCAPE = 0x1B;
    public const int VK_BACK = 0x08;
    public const int VK_SPACE = 0x20;
    public const int VK_F1 = 0x70;
    public const int VK_F12 = 0x7B;
    public const int VK_F24 = 0x87;
    public const int VK_UP = 0x26;
    public const int VK_DOWN = 0x28;
    public const int VK_LEFT = 0x25;
    public const int VK_RIGHT = 0x27;
    public const int VK_CONTROL = 0x11;
    public const int VK_SHIFT = 0x10;
    public const int VK_MENU = 0x12;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;
    public const int VK_COPILOT = 0xE6;
    public const int VK_VOLUME_MUTE = 0xAD;
    public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
    public const int VK_INSERT = 0x2D;
    public const int VK_DELETE = 0x2E;
    public const int VK_HOME = 0x24;
    public const int VK_END = 0x23;
    public const int VK_PRIOR = 0x21;
    public const int VK_NEXT = 0x22;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_RMENU = 0xA5;
}

/// <summary>
/// Unit tests for the VirtualKeyMapper class.
/// </summary>
public class VirtualKeyMapperTests
{
    #region TryGetVirtualKeyCode Tests

    [Theory]
    [InlineData("enter", VK.VK_RETURN)]
    [InlineData("ENTER", VK.VK_RETURN)]
    [InlineData("Enter", VK.VK_RETURN)]
    [InlineData("return", VK.VK_RETURN)]
    [InlineData("tab", VK.VK_TAB)]
    [InlineData("escape", VK.VK_ESCAPE)]
    [InlineData("esc", VK.VK_ESCAPE)]
    [InlineData("backspace", VK.VK_BACK)]
    [InlineData("space", VK.VK_SPACE)]
    public void TryGetVirtualKeyCode_NavigationKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("f1", VK.VK_F1)]
    [InlineData("f12", VK.VK_F12)]
    [InlineData("f24", VK.VK_F24)]
    public void TryGetVirtualKeyCode_FunctionKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("up", VK.VK_UP)]
    [InlineData("down", VK.VK_DOWN)]
    [InlineData("left", VK.VK_LEFT)]
    [InlineData("right", VK.VK_RIGHT)]
    [InlineData("arrowup", VK.VK_UP)]
    [InlineData("arrowdown", VK.VK_DOWN)]
    public void TryGetVirtualKeyCode_ArrowKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("ctrl", VK.VK_CONTROL)]
    [InlineData("control", VK.VK_CONTROL)]
    [InlineData("shift", VK.VK_SHIFT)]
    [InlineData("alt", VK.VK_MENU)]
    [InlineData("win", VK.VK_LWIN)]
    [InlineData("windows", VK.VK_LWIN)]
    [InlineData("meta", VK.VK_LWIN)]
    public void TryGetVirtualKeyCode_ModifierKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("a", 0x41)]
    [InlineData("A", 0x41)]
    [InlineData("z", 0x5A)]
    [InlineData("Z", 0x5A)]
    public void TryGetVirtualKeyCode_LetterKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("0", 0x30)]
    [InlineData("9", 0x39)]
    public void TryGetVirtualKeyCode_NumberKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("copilot", VK.VK_COPILOT)]
    public void TryGetVirtualKeyCode_CopilotKey_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("volumemute", VK.VK_VOLUME_MUTE)]
    [InlineData("volume_mute", VK.VK_VOLUME_MUTE)]
    [InlineData("playpause", VK.VK_MEDIA_PLAY_PAUSE)]
    public void TryGetVirtualKeyCode_MediaKeys_ReturnsCorrectCode(string keyName, int expectedCode)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCode, virtualKeyCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("unknownkey")]
    [InlineData("f25")]
    [InlineData("@")]
    public void TryGetVirtualKeyCode_InvalidKeys_ReturnsFalse(string? keyName)
    {
        // Act
        var result = VirtualKeyMapper.TryGetVirtualKeyCode(keyName!, out var virtualKeyCode);

        // Assert
        Assert.False(result);
        Assert.Equal(0, virtualKeyCode);
    }

    #endregion

    #region IsExtendedKey Tests

    [Theory]
    [InlineData(VK.VK_INSERT)]
    [InlineData(VK.VK_DELETE)]
    [InlineData(VK.VK_HOME)]
    [InlineData(VK.VK_END)]
    [InlineData(VK.VK_PRIOR)]
    [InlineData(VK.VK_NEXT)]
    [InlineData(VK.VK_LEFT)]
    [InlineData(VK.VK_RIGHT)]
    [InlineData(VK.VK_UP)]
    [InlineData(VK.VK_DOWN)]
    [InlineData(VK.VK_RCONTROL)]
    [InlineData(VK.VK_RMENU)]
    [InlineData(VK.VK_LWIN)]
    [InlineData(VK.VK_RWIN)]
    public void IsExtendedKey_ExtendedKeys_ReturnsTrue(int virtualKeyCode)
    {
        // Act
        var result = VirtualKeyMapper.IsExtendedKey(virtualKeyCode);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0x41)] // 'A'
    [InlineData(VK.VK_RETURN)]
    [InlineData(VK.VK_TAB)]
    [InlineData(VK.VK_SHIFT)]
    [InlineData(VK.VK_CONTROL)]
    [InlineData(VK.VK_MENU)]
    [InlineData(VK.VK_F1)]
    public void IsExtendedKey_NonExtendedKeys_ReturnsFalse(int virtualKeyCode)
    {
        // Act
        var result = VirtualKeyMapper.IsExtendedKey(virtualKeyCode);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidKeyName Tests

    [Theory]
    [InlineData("enter")]
    [InlineData("Enter")]
    [InlineData("ENTER")]
    [InlineData("a")]
    [InlineData("f1")]
    [InlineData("copilot")]
    public void IsValidKeyName_ValidKeys_ReturnsTrue(string keyName)
    {
        // Act
        var result = VirtualKeyMapper.IsValidKeyName(keyName);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("f25")]
    public void IsValidKeyName_InvalidKeys_ReturnsFalse(string? keyName)
    {
        // Act
        var result = VirtualKeyMapper.IsValidKeyName(keyName!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetVirtualKeyCode Tests

    [Fact]
    public void GetVirtualKeyCode_ValidKey_ReturnsCode()
    {
        // Act
        var result = VirtualKeyMapper.GetVirtualKeyCode("enter");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VK.VK_RETURN, result.Value);
    }

    [Fact]
    public void GetVirtualKeyCode_InvalidKey_ReturnsNull()
    {
        // Act
        var result = VirtualKeyMapper.GetVirtualKeyCode("invalidkey");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllKeyNames Tests

    [Fact]
    public void GetAllKeyNames_ReturnsNonEmptyCollection()
    {
        // Act
        var keyNames = VirtualKeyMapper.GetAllKeyNames().ToList();

        // Assert
        Assert.NotEmpty(keyNames);
        Assert.Contains("enter", keyNames);
        Assert.Contains("tab", keyNames);
        Assert.Contains("copilot", keyNames);
    }

    #endregion
}
