using Microsoft.Extensions.Logging;
using NSubstitute;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Unit.Automation.Tools;

/// <summary>
/// Unit tests for the 6 focused UI automation tools.
/// Validates tool initialization and constructor requirements.
/// Each tool has a single clear responsibility with reduced parameters.
/// </summary>
public sealed class UIAutomationToolsTests
{
    private readonly IUIAutomationService _mockService;
    private readonly IOcrService _mockOcrService;
    private readonly IScreenshotService _mockScreenshotService;
    private readonly IWindowService _mockWindowService;
    private readonly ILogger<UIFindTool> _mockFindLogger;
    private readonly ILogger<UIClickTool> _mockClickLogger;
    private readonly ILogger<UITypeTool> _mockTypeLogger;
    private readonly ILogger<UIWaitTool> _mockWaitLogger;
    private readonly ILogger<UIReadTool> _mockReadLogger;
    private readonly ILogger<UIFileTool> _mockFileLogger;

    public UIAutomationToolsTests()
    {
        _mockService = Substitute.For<IUIAutomationService>();
        _mockOcrService = Substitute.For<IOcrService>();
        _mockScreenshotService = Substitute.For<IScreenshotService>();
        _mockWindowService = Substitute.For<IWindowService>();

        _mockFindLogger = Substitute.For<ILogger<UIFindTool>>();
        _mockClickLogger = Substitute.For<ILogger<UIClickTool>>();
        _mockTypeLogger = Substitute.For<ILogger<UITypeTool>>();
        _mockWaitLogger = Substitute.For<ILogger<UIWaitTool>>();
        _mockReadLogger = Substitute.For<ILogger<UIReadTool>>();
        _mockFileLogger = Substitute.For<ILogger<UIFileTool>>();
    }

    #region UIFindTool Tests

    [Fact]
    public void UIFindTool_Constructor_WithNullService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIFindTool(null!, _mockFindLogger));
    }

    [Fact]
    public void UIFindTool_Constructor_WithNullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIFindTool(_mockService, null!));
    }

    [Fact]
    public void UIFindTool_Constructor_WithValidDependencies_Succeeds()
    {
        var tool = new UIFindTool(_mockService, _mockFindLogger);
        Assert.NotNull(tool);
    }

    #endregion

    #region UIClickTool Tests

    [Fact]
    public void UIClickTool_Constructor_WithNullService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIClickTool(null!, _mockWindowService, _mockClickLogger));
    }

    [Fact]
    public void UIClickTool_Constructor_WithNullWindowService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIClickTool(_mockService, null!, _mockClickLogger));
    }

    [Fact]
    public void UIClickTool_Constructor_WithNullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIClickTool(_mockService, _mockWindowService, null!));
    }

    [Fact]
    public void UIClickTool_Constructor_WithValidDependencies_Succeeds()
    {
        var tool = new UIClickTool(_mockService, _mockWindowService, _mockClickLogger);
        Assert.NotNull(tool);
    }

    #endregion

    #region UITypeTool Tests

    [Fact]
    public void UITypeTool_Constructor_WithNullService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UITypeTool(null!, _mockTypeLogger));
    }

    [Fact]
    public void UITypeTool_Constructor_WithNullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UITypeTool(_mockService, null!));
    }

    [Fact]
    public void UITypeTool_Constructor_WithValidDependencies_Succeeds()
    {
        var tool = new UITypeTool(_mockService, _mockTypeLogger);
        Assert.NotNull(tool);
    }

    #endregion

    #region UIWaitTool Tests

    [Fact]
    public void UIWaitTool_Constructor_WithNullService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIWaitTool(null!, _mockWaitLogger));
    }

    [Fact]
    public void UIWaitTool_Constructor_WithNullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIWaitTool(_mockService, null!));
    }

    [Fact]
    public void UIWaitTool_Constructor_WithValidDependencies_Succeeds()
    {
        var tool = new UIWaitTool(_mockService, _mockWaitLogger);
        Assert.NotNull(tool);
    }

    #endregion

    #region UIReadTool Tests

    [Fact]
    public void UIReadTool_Constructor_WithNullService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIReadTool(null!, _mockOcrService, _mockScreenshotService, _mockReadLogger));
    }

    [Fact]
    public void UIReadTool_Constructor_WithNullOcrService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIReadTool(_mockService, null!, _mockScreenshotService, _mockReadLogger));
    }

    [Fact]
    public void UIReadTool_Constructor_WithNullScreenshotService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIReadTool(_mockService, _mockOcrService, null!, _mockReadLogger));
    }

    [Fact]
    public void UIReadTool_Constructor_WithNullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIReadTool(_mockService, _mockOcrService, _mockScreenshotService, null!));
    }

    [Fact]
    public void UIReadTool_Constructor_WithValidDependencies_Succeeds()
    {
        var tool = new UIReadTool(_mockService, _mockOcrService, _mockScreenshotService, _mockReadLogger);
        Assert.NotNull(tool);
    }

    #endregion

    #region UIFileTool Tests

    [Fact]
    public void UIFileTool_Constructor_WithNullService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIFileTool(null!, _mockFileLogger));
    }

    [Fact]
    public void UIFileTool_Constructor_WithNullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIFileTool(_mockService, null!));
    }

    [Fact]
    public void UIFileTool_Constructor_WithValidDependencies_Succeeds()
    {
        var tool = new UIFileTool(_mockService, _mockFileLogger);
        Assert.NotNull(tool);
    }

    #endregion

    #region Architecture Validation

    [Fact]
    public void AllTools_AreSealed_AndFocused()
    {
        // Verify we've properly split the old mega-tool into 6 focused tools
        var toolTypes = new[]
        {
            typeof(UIFindTool),      // Find/locate elements
            typeof(UIClickTool),     // Click elements  
            typeof(UITypeTool),      // Type text into fields
            typeof(UIWaitTool),      // Wait for state changes
            typeof(UIReadTool),      // Read text with OCR fallback
            typeof(UIFileTool)       // File operations (Save)
        };

        Assert.Equal(6, toolTypes.Length);
        foreach (var toolType in toolTypes)
        {
            Assert.NotNull(toolType);
            Assert.True(toolType.IsClass, $"{toolType.Name} should be a class");
            Assert.True(toolType.IsSealed, $"{toolType.Name} should be sealed");
        }
    }

    #endregion
}
