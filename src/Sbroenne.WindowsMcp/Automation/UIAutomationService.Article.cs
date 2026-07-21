using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Clean "article" text extraction for web pages, used by <c>ui_read</c> in
/// <see cref="TextExtractionMode.Article"/> mode.
/// </summary>
/// <remarks>
/// Strategy (single cached tree walk, no per-node cross-process calls in the hot loop):
/// <list type="number">
/// <item>Resolve the primary content region by locating the <c>main</c> landmark; fall back to the
/// whole element when a page exposes no landmark.</item>
/// <item>Cache the content subtree in a single <c>FindFirstBuildCache</c> call and walk it with
/// <c>GetCachedChildren</c> so the poll loop makes no additional COM calls (this is what previously
/// starved the shared UIA STA thread).</item>
/// <item>Skip navigation, search and complementary landmark subtrees (site header, left nav,
/// breadcrumbs, "in this article" rails, footer).</item>
/// <item>Collect visible leaf text and headings only, preferring the accessible name (never the
/// ValuePattern href), so link text is preserved without inline URL noise.</item>
/// </list>
/// </remarks>
public sealed partial class UIAutomationService
{
    private const int MaxArticleNodesToScan = 6000;
    private const int MaxArticleDepth = 60;

    private static string ExtractArticleText(UIA.IUIAutomationElement element)
    {
        var contentRoot = FindMainLandmark(element) ?? element;

        UIA.IUIAutomationElement cachedRoot;
        try
        {
            var cacheRequest = CreateArticleCacheRequest();
            var built = contentRoot.FindFirstBuildCache(UIA.TreeScope.TreeScope_Element, Uia.TrueCondition, cacheRequest);
            if (built == null)
            {
                return string.Empty;
            }

            cachedRoot = built;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return string.Empty;
        }

        var nodes = new List<ArticleNode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var scanned = 0;
        CollectArticleNodes(cachedRoot, nodes, seen, insideList: false, inheritedHeadingLevel: 0, depth: 0, ref scanned);

        return ArticleTextExtraction.FormatArticle(nodes);
    }

    private static UIA.IUIAutomationCacheRequest CreateArticleCacheRequest()
    {
        var request = Uia.CreateCacheRequest();
        request.AddProperty(UIA3PropertyIds.Name);
        request.AddProperty(UIA3PropertyIds.ControlType);
        request.AddProperty(UIA3PropertyIds.IsOffscreen);
        request.AddProperty(UIA3PropertyIds.LandmarkType);
        request.AddProperty(UIA3PropertyIds.LocalizedLandmarkType);
        request.AddProperty(UIA3PropertyIds.HeadingLevel);
        request.TreeScope = UIA.TreeScope.TreeScope_Subtree;
        return request;
    }

    private static UIA.IUIAutomationElement? FindMainLandmark(UIA.IUIAutomationElement element)
    {
        try
        {
            var mainCondition = Uia.CreatePropertyCondition(UIA3PropertyIds.LandmarkType, UIA3LandmarkTypeIds.Main);
            return element.FindFirst(UIA.TreeScope.TreeScope_Descendants, mainCondition);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private static void CollectArticleNodes(
        UIA.IUIAutomationElement node,
        List<ArticleNode> nodes,
        HashSet<string> seen,
        bool insideList,
        int inheritedHeadingLevel,
        int depth,
        ref int scanned)
    {
        if (scanned >= MaxArticleNodesToScan || depth > MaxArticleDepth)
        {
            return;
        }

        scanned++;

        if (IsSkippableLandmark(node))
        {
            return;
        }

        var controlTypeId = SafeCachedControlTypeId(node);
        var headingLevel = Math.Max(inheritedHeadingLevel, GetHeadingLevel(node));
        var isList = insideList || controlTypeId == UIA3ControlTypeIds.ListItem;

        var children = SafeCachedChildren(node);
        if (children == null || children.Length == 0)
        {
            // Leaf: this is where the real text lives. Use the accessible name (visible link text),
            // never the ValuePattern value (which would be a raw href).
            var name = node.GetCachedName();
            if (!string.IsNullOrWhiteSpace(name) && !ArticleTextExtraction.LooksLikeUrl(name) && seen.Add(name))
            {
                nodes.Add(new ArticleNode(name, headingLevel, isList));
            }

            return;
        }

        for (var i = 0; i < children.Length && scanned < MaxArticleNodesToScan; i++)
        {
            UIA.IUIAutomationElement? child;
            try
            {
                child = children.GetElement(i);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                continue;
            }

            if (child == null)
            {
                continue;
            }

            CollectArticleNodes(child, nodes, seen, isList, headingLevel, depth + 1, ref scanned);
        }
    }

    private static bool IsSkippableLandmark(UIA.IUIAutomationElement node)
    {
        try
        {
            var landmark = node.GetCachedPropertyValue(UIA3PropertyIds.LandmarkType);
            if (landmark is int landmarkId &&
                (landmarkId == UIA3LandmarkTypeIds.Navigation || landmarkId == UIA3LandmarkTypeIds.Search))
            {
                return true;
            }

            var localized = node.GetCachedPropertyValue(UIA3PropertyIds.LocalizedLandmarkType) as string;
            if (!string.IsNullOrEmpty(localized) &&
                (localized.Equals("complementary", StringComparison.OrdinalIgnoreCase) ||
                 localized.Equals("banner", StringComparison.OrdinalIgnoreCase) ||
                 localized.Equals("contentinfo", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Property not cached / element gone - treat as non-landmark.
        }

        return false;
    }

    private static int GetHeadingLevel(UIA.IUIAutomationElement node)
    {
        try
        {
            if (node.GetCachedPropertyValue(UIA3PropertyIds.HeadingLevel) is int raw)
            {
                return UIA3LandmarkTypeIds.ToHeadingLevel(raw);
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Not a heading.
        }

        return 0;
    }

    private static int SafeCachedControlTypeId(UIA.IUIAutomationElement node)
    {
        try
        {
            return node.GetCachedControlTypeId();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return 0;
        }
    }

    private static UIA.IUIAutomationElementArray? SafeCachedChildren(UIA.IUIAutomationElement node)
    {
        try
        {
            return node.GetCachedChildren();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }
}
