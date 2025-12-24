namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Supported UI Automation patterns for interaction.
/// </summary>
public static class PatternTypes
{
    /// <summary>Click/activate pattern.</summary>
    public const string Invoke = "Invoke";

    /// <summary>Checkbox/toggle pattern.</summary>
    public const string Toggle = "Toggle";

    /// <summary>Get/set text value pattern.</summary>
    public const string Value = "Value";

    /// <summary>Multi-select pattern.</summary>
    public const string Selection = "Selection";

    /// <summary>Single select pattern.</summary>
    public const string SelectionItem = "SelectionItem";

    /// <summary>Tree/dropdown expand/collapse pattern.</summary>
    public const string ExpandCollapse = "ExpandCollapse";

    /// <summary>Scrollable container pattern.</summary>
    public const string Scroll = "Scroll";

    /// <summary>Scroll into view pattern.</summary>
    public const string ScrollItem = "ScrollItem";

    /// <summary>Rich text pattern.</summary>
    public const string Text = "Text";

    /// <summary>Slider/spinner range value pattern.</summary>
    public const string RangeValue = "RangeValue";

    /// <summary>Window controls pattern.</summary>
    public const string Window = "Window";

    /// <summary>Move/resize pattern.</summary>
    public const string Transform = "Transform";
}
