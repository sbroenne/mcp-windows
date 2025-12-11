# Quickstart: Implementing Mouse Position Awareness

**Feature**: 012-mouse-position  
**Date**: December 11, 2025  
**Audience**: Development team implementing breaking changes to MouseControlTool

## Overview

This quickstart guides you through the changes required to implement monitor-explicit mouse control. The feature makes `monitorIndex` **required** when coordinates (x/y) are provided, enabling LLMs to target specific monitors reliably.

## Key Changes at a Glance

| Area | Change | Breaking? | Impact |
|------|--------|-----------|--------|
| **MouseControlTool validation** | Require `monitorIndex` when x/y provided | YES | Coordinate-based calls must specify monitor |
| **MouseControlResult** | Add monitor dimension fields | NO | Response data enriched (backward compatible in JSON) |
| **Error handling** | New error codes and detailed context | NO | Better error messages with valid monitor indices |
| **Tests** | Multi-monitor integration tests | — | Validates behavior on multi-monitor systems |

## Implementation Path

### Phase 1: Update Response Model (MouseControlResult.cs)

Add three new fields to enrich responses with monitor context:

```csharp
public class MouseControlResult
{
    // Existing fields
    public bool Success { get; set; }
    public Position FinalPosition { get; set; }
    public string WindowTitle { get; set; }
    public string Error { get; set; }
    
    // NEW FIELDS
    [JsonPropertyName("monitorIndex")]
    public int? MonitorIndex { get; set; }
    
    [JsonPropertyName("monitorWidth")]
    public int? MonitorWidth { get; set; }
    
    [JsonPropertyName("monitorHeight")]
    public int? MonitorHeight { get; set; }
    
    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; }
    
    [JsonPropertyName("error_details")]
    public Dictionary<string, object> ErrorDetails { get; set; }
}
```

**Rationale**: Monitor dimensions tell the LLM the coordinate bounds for future calls. Error codes enable LLMs to distinguish between "missing parameter" vs. "out of bounds" vs. "invalid index".

### Phase 2: Update Validation in MouseControlTool.cs

In `ExecuteAsync()`, add conditional-required validation **before** operation execution:

```csharp
public async Task<MouseControlResult> ExecuteAsync(MouseControlRequest request)
{
    try
    {
        // NEW: Validate monitorIndex requirement
        if (HasCoordinates(request) && !request.MonitorIndex.HasValue)
        {
            var monitors = _screenService.GetMonitorIndices();
            return new MouseControlResult
            {
                Success = false,
                Error = "monitorIndex is required when using x/y coordinates",
                ErrorCode = "missing_required_parameter",
                ErrorDetails = new Dictionary<string, object>
                {
                    { "valid_indices", monitors }
                }
            };
        }
        
        // NEW: Validate monitorIndex is in valid range
        if (request.MonitorIndex.HasValue)
        {
            if (!_screenService.IsValidMonitorIndex(request.MonitorIndex.Value))
            {
                var monitors = _screenService.GetMonitorIndices();
                return new MouseControlResult
                {
                    Success = false,
                    Error = $"Invalid monitorIndex: {request.MonitorIndex}",
                    ErrorCode = "invalid_coordinates",
                    ErrorDetails = new Dictionary<string, object>
                    {
                        { "valid_indices", monitors },
                        { "provided_index", request.MonitorIndex }
                    }
                };
            }
        }
        
        // NEW: Validate coordinates are within monitor bounds (if both provided)
        if (request.MonitorIndex.HasValue && request.X.HasValue && request.Y.HasValue)
        {
            var monitor = _screenService.GetMonitor(request.MonitorIndex.Value);
            if (!monitor.Contains(request.X.Value, request.Y.Value))
            {
                return new MouseControlResult
                {
                    Success = false,
                    Error = $"Coordinates ({request.X}, {request.Y}) out of bounds for monitor {request.MonitorIndex}",
                    ErrorCode = "coordinates_out_of_bounds",
                    ErrorDetails = new Dictionary<string, object>
                    {
                        { "valid_bounds", new { 
                            left = monitor.Left, 
                            top = monitor.Top, 
                            right = monitor.Right, 
                            bottom = monitor.Bottom 
                        }},
                        { "provided_coordinates", new { x = request.X, y = request.Y } }
                    }
                };
            }
        }
        
        // Existing operation execution follows...
        var result = await PerformMouseOperation(request);
        
        // NEW: Enrich successful response with monitor context
        if (result.Success)
        {
            var finalMonitor = _screenService.FindMonitorAtPosition(result.FinalPosition.X, result.FinalPosition.Y);
            result.MonitorIndex = finalMonitor.Index;
            result.MonitorWidth = finalMonitor.Width;
            result.MonitorHeight = finalMonitor.Height;
        }
        
        return result;
    }
    catch (Exception ex)
    {
        return new MouseControlResult
        {
            Success = false,
            Error = ex.Message,
            ErrorCode = "unexpected_error"
        };
    }
}

private bool HasCoordinates(MouseControlRequest request)
{
    // Coordinates present if x/y both provided, or for drag endX/endY
    return (request.X.HasValue && request.Y.HasValue) || 
           (request.EndX.HasValue && request.EndY.HasValue) ||
           request.Action == "drag";
}
```

**Rationale**: Fail fast with actionable errors. Give LLMs the list of valid monitors so they can recover without re-querying.

### Phase 3: Test Multi-Monitor Behavior (Integration Tests)

Create `MultiMonitorFixture.cs` for reusable test setup:

```csharp
public class MultiMonitorFixture : IAsyncLifetime
{
    private readonly IScreen _screenService;
    private List<int> _availableMonitors;
    
    public async Task InitializeAsync()
    {
        // Detect available monitors
        _availableMonitors = _screenService.GetMonitorIndices();
        
        // Ensure at least 2 monitors detected, or log requirement
        if (_availableMonitors.Count < 2)
        {
            throw new InvalidOperationException(
                "Multi-monitor tests require at least 2 connected monitors. " +
                "Found: " + string.Join(", ", _availableMonitors));
        }
    }
    
    public async Task DisposeAsync()
    {
        // Restore cursor to primary monitor
        var primary = _screenService.GetMonitor(0);
        await _screenService.MoveMouseAsync(primary.Center.X, primary.Center.Y);
    }
    
    public int GetSecondaryMonitor() => _availableMonitors[1];
    public List<int> GetAllMonitors() => _availableMonitors;
}
```

**Example Test: Click on Secondary Monitor Requires monitorIndex**:

```csharp
public class MouseControlToolTests : IAsyncLifetime
{
    private readonly MouseControlTool _tool;
    private MultiMonitorFixture _fixture;
    
    [Fact]
    public async Task Click_WithCoordinates_RequiresMonitorIndex()
    {
        // Arrange
        var request = new MouseControlRequest
        {
            Action = "click",
            X = 500,
            Y = 300
            // NOTE: No MonitorIndex - this should fail
        };
        
        // Act
        var result = await _tool.ExecuteAsync(request);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("missing_required_parameter", result.ErrorCode);
        Assert.NotNull(result.ErrorDetails["valid_indices"]);
    }
    
    [Fact]
    public async Task Click_WithValidMonitorIndex_Succeeds()
    {
        // Arrange
        var secondaryMonitor = _fixture.GetSecondaryMonitor();
        var monitor = _screenService.GetMonitor(secondaryMonitor);
        
        var request = new MouseControlRequest
        {
            Action = "click",
            X = monitor.Width / 2,
            Y = monitor.Height / 2,
            MonitorIndex = secondaryMonitor
        };
        
        // Act
        var result = await _tool.ExecuteAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(secondaryMonitor, result.MonitorIndex);
        Assert.Equal(monitor.Width, result.MonitorWidth);
        Assert.Equal(monitor.Height, result.MonitorHeight);
    }
    
    [Fact]
    public async Task Click_WithInvalidMonitorIndex_ReturnsError()
    {
        // Arrange
        var request = new MouseControlRequest
        {
            Action = "click",
            X = 500,
            Y = 300,
            MonitorIndex = 99  // Invalid
        };
        
        // Act
        var result = await _tool.ExecuteAsync(request);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid_coordinates", result.ErrorCode);
        Assert.NotNull(result.ErrorDetails["valid_indices"]);
    }
    
    [Fact]
    public async Task Click_CoordinatesOutOfBounds_ReturnsDetailedError()
    {
        // Arrange
        var secondaryMonitor = _fixture.GetSecondaryMonitor();
        
        var request = new MouseControlRequest
        {
            Action = "click",
            X = 5000,  // Way out of bounds
            Y = 5000,
            MonitorIndex = secondaryMonitor
        };
        
        // Act
        var result = await _tool.ExecuteAsync(request);
        
        // Assert
        Assert.False(result.Success);
        Assert.Equal("coordinates_out_of_bounds", result.ErrorCode);
        Assert.NotNull(result.ErrorDetails["valid_bounds"]);
        Assert.NotNull(result.ErrorDetails["provided_coordinates"]);
    }
    
    [Fact]
    public async Task Click_WithoutCoordinates_DoesNotRequireMonitorIndex()
    {
        // Arrange - cursor at some position
        var request = new MouseControlRequest
        {
            Action = "click"
            // No X, Y, MonitorIndex
        };
        
        // Act
        var result = await _tool.ExecuteAsync(request);
        
        // Assert
        Assert.True(result.Success);
        // But response should indicate which monitor the click happened on
        Assert.NotNull(result.MonitorIndex);
    }
    
    [Fact]
    public async Task GetPosition_ReturnsCurrentMonitorContext()
    {
        // Arrange
        var request = new MouseControlRequest { Action = "get_position" };
        
        // Act
        var result = await _tool.ExecuteAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.MonitorIndex);
        Assert.True(result.MonitorWidth > 0);
        Assert.True(result.MonitorHeight > 0);
    }
}
```

## Breaking Change Communication

### For API Users

**Before (v1.x)**:
```csharp
// monitorIndex is optional; defaults to 0
await mouseControl.Click(x: 500, y: 300);  // Clicks on monitor 0, silently wrong if target is monitor 1
```

**After (v2.0)**:
```csharp
// monitorIndex is REQUIRED with coordinates
await mouseControl.Click(x: 500, y: 300, monitorIndex: 1);  // Clear intent; error if index invalid
```

### For LLM Callers

**Error Message** (when monitorIndex missing):
```
monitorIndex is required when using x/y coordinates

Valid monitor indices: [0, 1, 2]
```

**Example Recovery Flow**:
1. LLM calls `screenshot(monitorIndex: 1)` → Gets image
2. LLM analyzes image → Finds button at coordinates (500, 300)
3. LLM calls `click(x: 500, y: 300, monitorIndex: 1)` → Click succeeds

## Testing Checklist

- [ ] Single monitor: All existing tests pass without modification
- [ ] Multi-monitor required: Add 2+ monitors; run new integration tests
- [ ] Error paths: Verify error_code and error_details populated
- [ ] Response enrichment: Verify monitorIndex, monitorWidth, monitorHeight in success responses
- [ ] Coordinate-less actions: Verify click() without x/y doesn't require monitorIndex
- [ ] Edge cases: Negative coordinates (multi-monitor setups), fractional scaling

## Files Changed

| File | Change |
|------|--------|
| `src/Sbroenne.WindowsMcp/Models/MouseControlResult.cs` | Add MonitorIndex, MonitorWidth, MonitorHeight, ErrorCode, ErrorDetails |
| `src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs` | Add validation for monitorIndex requirement; enrich responses |
| `tests/Sbroenne.WindowsMcp.Tests/MultiMonitorFixture.cs` | NEW: Fixture for multi-monitor test setup |
| `tests/Sbroenne.WindowsMcp.Tests/MouseControlToolTests.cs` | Add multi-monitor integration tests |

## Next Steps

1. **Code Review**: Review validation logic against multi-monitor test results
2. **Manual Testing**: Test on actual multi-monitor Windows 11 system (required by Constitution XIV)
3. **Documentation**: Update tool documentation with monitorIndex requirement
4. **Release**: Publish as v2.0 (breaking change) with migration guide

## Links

- **Specification**: [spec.md](../spec.md)
- **Data Model**: [data-model.md](../data-model.md)
- **API Contracts**: [contracts/](../contracts/)
- **Research**: [research.md](../research.md)
