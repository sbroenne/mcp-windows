using System.Reflection;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Tools;

/// <summary>
/// Verifies that MCP tool method signatures do not expose nullable numeric parameters.
/// Nullable numerics often produce JSON schema unions like ["integer","null"], which can
/// cause downstream LLM schema validation failures.
/// </summary>
public sealed class ToolOptionalNumericParametersTests
{
    public static TheoryData<Type, string> OptionalNumericParameterCases => new()
    {
        // App
        { typeof(AppTool), "timeoutMs" },

        // Screenshot control
        { typeof(ScreenshotControlTool), "monitorIndex" },
        { typeof(ScreenshotControlTool), "regionX" },
        { typeof(ScreenshotControlTool), "regionY" },
        { typeof(ScreenshotControlTool), "regionWidth" },
        { typeof(ScreenshotControlTool), "regionHeight" },
        { typeof(ScreenshotControlTool), "quality" },

        // Window management
        { typeof(WindowManagementTool), "x" },
        { typeof(WindowManagementTool), "y" },
        { typeof(WindowManagementTool), "width" },
        { typeof(WindowManagementTool), "height" },
        { typeof(WindowManagementTool), "timeoutMs" },
        { typeof(WindowManagementTool), "monitorIndex" },

        // Mouse control
        { typeof(MouseControlTool), "x" },
        { typeof(MouseControlTool), "y" },
        { typeof(MouseControlTool), "endX" },
        { typeof(MouseControlTool), "endY" },
        { typeof(MouseControlTool), "monitorIndex" },

        // Keyboard control
        { typeof(KeyboardControlTool), "interKeyDelayMs" },

        // UI automation tools
        { typeof(UIFindTool), "exactDepth" }
    };

    [Theory]
    [MemberData(nameof(OptionalNumericParameterCases))]
    public void ExecuteAsync_OptionalNumericParameter_IsNotNullable(Type toolType, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(toolType);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        var executeAsync = toolType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(m => m.Name == "ExecuteAsync");

        var parameter = executeAsync.GetParameters().SingleOrDefault(p => p.Name == parameterName);
        Assert.NotNull(parameter);

        // Nullable<T> for numeric types should not be exposed in public tool surface.
        if (parameter!.ParameterType.IsGenericType &&
            parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlying = Nullable.GetUnderlyingType(parameter.ParameterType);
            Assert.False(
                underlying is not null && IsNumericType(underlying),
                $"{toolType.Name}.ExecuteAsync parameter '{parameterName}' should not be nullable numeric (was {parameter.ParameterType}).");
        }
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) ||
        type == typeof(sbyte) ||
        type == typeof(short) ||
        type == typeof(ushort) ||
        type == typeof(int) ||
        type == typeof(uint) ||
        type == typeof(long) ||
        type == typeof(ulong) ||
        type == typeof(float) ||
        type == typeof(double) ||
        type == typeof(decimal);
}
