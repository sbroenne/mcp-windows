using System.Text.Json;
using System.Text.Json.Serialization;
using Sbroenne.WindowsMcp.Models;
using SharpToken;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Token count benchmarks for MCP response optimization.
/// Measures token usage of UIElementInfo vs UIElementCompact serialization.
/// </summary>
public sealed class TokenBenchmarkTests
{
    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("cl100k_base");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly ITestOutputHelper _output;

    public TokenBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Creates realistic sample UIElementInfo data representing a typical Notepad-like application.
    /// </summary>
    private static List<UIElementInfo> CreateSampleElements(int count)
    {
        var elements = new List<UIElementInfo>();

        // Common control types and names from a typical application
        var controlTypes = new[] { "Button", "Edit", "Text", "MenuItem", "Menu", "Pane", "Document", "CheckBox", "ComboBox", "ListItem" };
        var names = new[] { "Save", "Cancel", "OK", "File", "Edit", "View", "Help", "Text editor", "Close", "Minimize", "Maximize", "Undo", "Redo", "Cut", "Copy", "Paste", "Find", "Replace", "Font", "Format" };
        var patterns = new[]
        {
            new[] { "Invoke", "LegacyIAccessible" },
            new[] { "Value", "Scroll", "Text", "LegacyIAccessible", "Text2", "TextEdit" },
            new[] { "Toggle", "LegacyIAccessible" },
            new[] { "SelectionItem", "LegacyIAccessible" },
            new[] { "ExpandCollapse", "Invoke", "LegacyIAccessible" }
        };

        for (int i = 0; i < count; i++)
        {
            var controlType = controlTypes[i % controlTypes.Length];
            var name = names[i % names.Length];
            var supportedPatterns = patterns[i % patterns.Length];

            // Generate realistic coordinates
            var x = 100 + (i * 50) % 1600;
            var y = 100 + (i * 30) % 900;
            var width = controlType == "Document" ? 800 : controlType == "Button" ? 80 : 120;
            var height = controlType == "Document" ? 600 : 30;

            elements.Add(new UIElementInfo
            {
                ElementId = $"window:857388|runtime:{42 + i}.857388|path:fast/{controlType.ToLowerInvariant()}[{i}]",
                AutomationId = i % 3 == 0 ? $"btn{name}" : null,
                Name = name,
                ControlType = controlType,
                BoundingRect = new BoundingRect
                {
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                },
                MonitorRelativeRect = new MonitorRelativeRect
                {
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                },
                MonitorIndex = 0,
                ClickablePoint = new ClickablePoint
                {
                    X = x + width / 2,
                    Y = y + height / 2,
                    MonitorIndex = 0
                },
                SupportedPatterns = supportedPatterns,
                Value = controlType == "Edit" ? "Sample text content" : null,
                ToggleState = controlType == "CheckBox" ? "On" : null,
                IsEnabled = true,
                IsOffscreen = false
            });
        }

        return elements;
    }

    /// <summary>
    /// Baseline benchmark: Measures token count for current UIElementInfo serialization.
    /// </summary>
    [Fact]
    public void Baseline_UIElementInfo_TokenCount()
    {
        // Arrange
        var elements = CreateSampleElements(20);
        var json = JsonSerializer.Serialize(elements, JsonOptions);

        // Act
        var tokens = Encoding.Encode(json);
        var tokenCount = tokens.Count;

        // Assert & Report
        var tokensPerElement = (double)tokenCount / elements.Count;

        _output.WriteLine($"UIElementInfo (20 elements): {tokenCount} tokens total, {tokensPerElement:F1} tokens/element");

        // Log details for debugging
        var singleElementJson = JsonSerializer.Serialize(elements[0], JsonOptions);
        var singleTokens = Encoding.Encode(singleElementJson).Count;
        _output.WriteLine($"Single element: {singleTokens} tokens");

        // The baseline should be around 200 tokens per element
        // This test documents the current state - we'll compare against it after optimization
        Assert.InRange(tokensPerElement, 50, 400); // Wide range to document current state
    }

    /// <summary>
    /// Target benchmark: Measures token count for compact UIElementCompact serialization.
    /// </summary>
    [Fact]
    public void Target_UIElementCompact_TokenCount()
    {
        // Arrange
        var elements = CreateSampleElements(20);
        var compactElements = elements.Select(UIElementCompact.FromFull).ToList();
        var json = JsonSerializer.Serialize(compactElements, JsonOptions);

        // Act
        var tokens = Encoding.Encode(json);
        var tokenCount = tokens.Count;

        // Assert & Report
        var tokensPerElement = (double)tokenCount / compactElements.Count;

        _output.WriteLine($"UIElementCompact (20 elements): {tokenCount} tokens total, {tokensPerElement:F1} tokens/element");

        // Target: ~50 tokens per element (75% reduction from ~200)
        Assert.InRange(tokensPerElement, 20, 80);
    }

    /// <summary>
    /// Comparison test: Verifies the token reduction percentage meets target.
    /// </summary>
    [Fact]
    public void Comparison_TokenReduction_MeetsTarget()
    {
        // Arrange
        var elements = CreateSampleElements(20);
        var compactElements = elements.Select(UIElementCompact.FromFull).ToList();

        var fullJson = JsonSerializer.Serialize(elements, JsonOptions);
        var compactJson = JsonSerializer.Serialize(compactElements, JsonOptions);

        // Act
        var fullTokens = Encoding.Encode(fullJson).Count;
        var compactTokens = Encoding.Encode(compactJson).Count;

        var reductionPercent = (1.0 - (double)compactTokens / fullTokens) * 100;
        var fullPerElement = (double)fullTokens / elements.Count;
        var compactPerElement = (double)compactTokens / compactElements.Count;

        // Report
        var message = $"""
            Token Benchmark Results:
            ========================
            Full UIElementInfo:       {fullTokens,5} tokens ({fullPerElement:F1}/element)
            Compact UIElementCompact: {compactTokens,5} tokens ({compactPerElement:F1}/element)
            Reduction:                {reductionPercent:F1}%
            """;

        _output.WriteLine(message);

        // Assert: Target is 60%+ reduction (allowing some margin from 75% target)
        Assert.True(reductionPercent >= 60, message);
    }

    /// <summary>
    /// Outputs detailed token analysis for a single element.
    /// </summary>
    [Fact]
    public void SingleElement_DetailedAnalysis()
    {
        // Arrange
        var elements = CreateSampleElements(1);
        var element = elements[0];
        var compact = UIElementCompact.FromFull(element);

        var fullJson = JsonSerializer.Serialize(element, JsonOptions);
        var compactJson = JsonSerializer.Serialize(compact, JsonOptions);

        // Act
        var fullTokens = Encoding.Encode(fullJson).Count;
        var compactTokens = Encoding.Encode(compactJson).Count;

        // Report
        var message = $"""
            Single Element Analysis:
            ========================
            Full JSON ({fullTokens} tokens):
            {fullJson}

            Compact JSON ({compactTokens} tokens):
            {compactJson}

            Reduction: {(1.0 - (double)compactTokens / fullTokens) * 100:F1}%
            """;

        _output.WriteLine(message);

        Assert.True(compactTokens < fullTokens, message);
    }

    #region AnnotatedElement Benchmarks

    /// <summary>
    /// Creates sample AnnotatedElement data in the OLD full format (for comparison).
    /// </summary>
    private static List<AnnotatedElementFull> CreateSampleAnnotatedElementsFull(int count)
    {
        var elements = new List<AnnotatedElementFull>();
        var controlTypes = new[] { "Button", "Edit", "CheckBox", "ComboBox", "ListItem", "MenuItem", "Tab", "RadioButton" };
        var names = new[] { "Save", "Cancel", "OK", "Submit", "Close", "Open", "Delete", "Refresh", "Settings", "Help" };

        for (int i = 0; i < count; i++)
        {
            var x = 100 + (i * 60) % 1600;
            var y = 100 + (i * 40) % 900;
            var width = 100;
            var height = 30;

            elements.Add(new AnnotatedElementFull
            {
                Index = i + 1,
                Name = names[i % names.Length],
                ControlType = controlTypes[i % controlTypes.Length],
                AutomationId = i % 3 == 0 ? $"btn{names[i % names.Length]}" : null,
                ElementId = $"window:857388|runtime:{42 + i}.857388|path:fast/button[{i}]",
                ClickablePoint = new ClickablePoint { X = x + width / 2, Y = y + height / 2, MonitorIndex = 0 },
                BoundingBox = new BoundingRect { X = x, Y = y, Width = width, Height = height }
            });
        }

        return elements;
    }

    /// <summary>
    /// Creates sample AnnotatedElement data in the NEW compact format.
    /// </summary>
    private static List<AnnotatedElement> CreateSampleAnnotatedElementsCompact(int count)
    {
        var controlTypes = new[] { "Button", "Edit", "CheckBox", "ComboBox", "ListItem", "MenuItem", "Tab", "RadioButton" };
        var names = new[] { "Save", "Cancel", "OK", "Submit", "Close", "Open", "Delete", "Refresh", "Settings", "Help" };

        var elements = new List<AnnotatedElement>();

        for (int i = 0; i < count; i++)
        {
            var x = 100 + (i * 60) % 1600;
            var y = 100 + (i * 40) % 900;
            var width = 100;
            var height = 30;

            elements.Add(new AnnotatedElement
            {
                Index = i + 1,
                Name = names[i % names.Length],
                Type = controlTypes[i % controlTypes.Length],
                Id = $"window:857388|runtime:{42 + i}.857388|path:fast/button[{i}]",
                Click = [x + width / 2, y + height / 2, 0]
            });
        }

        return elements;
    }

    /// <summary>
    /// Baseline benchmark: Measures token count for OLD AnnotatedElement format.
    /// </summary>
    [Fact]
    public void AnnotatedElement_Full_TokenCount()
    {
        // Arrange
        var elements = CreateSampleAnnotatedElementsFull(20);
        var json = JsonSerializer.Serialize(elements, JsonOptions);

        // Act
        var tokens = Encoding.Encode(json);
        var tokenCount = tokens.Count;
        var tokensPerElement = (double)tokenCount / elements.Count;

        _output.WriteLine($"AnnotatedElement FULL (20 elements): {tokenCount} tokens total, {tokensPerElement:F1} tokens/element");

        // Log single element
        var singleJson = JsonSerializer.Serialize(elements[0], JsonOptions);
        var singleTokens = Encoding.Encode(singleJson).Count;
        _output.WriteLine($"Single element: {singleTokens} tokens");
        _output.WriteLine($"JSON: {singleJson}");

        Assert.InRange(tokensPerElement, 50, 200);
    }

    /// <summary>
    /// Target benchmark: Measures token count for NEW compact AnnotatedElement format.
    /// </summary>
    [Fact]
    public void AnnotatedElement_Compact_TokenCount()
    {
        // Arrange
        var elements = CreateSampleAnnotatedElementsCompact(20);
        var json = JsonSerializer.Serialize(elements, JsonOptions);

        // Act
        var tokens = Encoding.Encode(json);
        var tokenCount = tokens.Count;
        var tokensPerElement = (double)tokenCount / elements.Count;

        _output.WriteLine($"AnnotatedElement COMPACT (20 elements): {tokenCount} tokens total, {tokensPerElement:F1} tokens/element");

        // Log single element
        var singleJson = JsonSerializer.Serialize(elements[0], JsonOptions);
        var singleTokens = Encoding.Encode(singleJson).Count;
        _output.WriteLine($"Single element: {singleTokens} tokens");
        _output.WriteLine($"JSON: {singleJson}");

        Assert.InRange(tokensPerElement, 20, 80);
    }

    /// <summary>
    /// Comparison test: Verifies AnnotatedElement token reduction percentage.
    /// </summary>
    [Fact]
    public void AnnotatedElement_Comparison_TokenReduction()
    {
        // Arrange
        var fullElements = CreateSampleAnnotatedElementsFull(20);
        var compactElements = CreateSampleAnnotatedElementsCompact(20);

        var fullJson = JsonSerializer.Serialize(fullElements, JsonOptions);
        var compactJson = JsonSerializer.Serialize(compactElements, JsonOptions);

        // Act
        var fullTokens = Encoding.Encode(fullJson).Count;
        var compactTokens = Encoding.Encode(compactJson).Count;

        var reductionPercent = (1.0 - (double)compactTokens / fullTokens) * 100;
        var fullPerElement = (double)fullTokens / fullElements.Count;
        var compactPerElement = (double)compactTokens / compactElements.Count;

        // Report
        var message = $"""
            AnnotatedElement Token Benchmark Results:
            ==========================================
            Full format:    {fullTokens,5} tokens ({fullPerElement:F1}/element)
            Compact format: {compactTokens,5} tokens ({compactPerElement:F1}/element)
            Reduction:      {reductionPercent:F1}%
            """;

        _output.WriteLine(message);

        // Assert: Target is 40%+ reduction
        Assert.True(reductionPercent >= 40, message);
    }

    /// <summary>
    /// Detailed single-element comparison for AnnotatedElement.
    /// </summary>
    [Fact]
    public void AnnotatedElement_SingleElement_DetailedAnalysis()
    {
        // Arrange
        var fullElements = CreateSampleAnnotatedElementsFull(1);
        var compactElements = CreateSampleAnnotatedElementsCompact(1);

        var fullJson = JsonSerializer.Serialize(fullElements[0], JsonOptions);
        var compactJson = JsonSerializer.Serialize(compactElements[0], JsonOptions);

        // Act
        var fullTokens = Encoding.Encode(fullJson).Count;
        var compactTokens = Encoding.Encode(compactJson).Count;

        // Report
        var message = $"""
            AnnotatedElement Single Element Analysis:
            ==========================================
            Full JSON ({fullTokens} tokens):
            {fullJson}

            Compact JSON ({compactTokens} tokens):
            {compactJson}

            Reduction: {(1.0 - (double)compactTokens / fullTokens) * 100:F1}%
            """;

        _output.WriteLine(message);

        Assert.True(compactTokens < fullTokens, message);
    }

    #endregion

    /// <summary>
    /// Represents the OLD full AnnotatedElement format for benchmarking comparison.
    /// </summary>
    private sealed record AnnotatedElementFull
    {
        public required int Index { get; init; }
        public string? Name { get; init; }
        public required string ControlType { get; init; }
        public string? AutomationId { get; init; }
        public required string ElementId { get; init; }
        public required ClickablePoint ClickablePoint { get; init; }
        public required BoundingRect BoundingBox { get; init; }
    }
}
