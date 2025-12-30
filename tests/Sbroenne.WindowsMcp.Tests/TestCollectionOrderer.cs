// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests;

/// <summary>
/// Orders test collections to run Unit tests before Integration tests.
/// This ensures proper test isolation and faster feedback from unit tests.
/// </summary>
public class TestCollectionOrderer : ITestCollectionOrderer
{
    /// <inheritdoc/>
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        return testCollections
            .OrderBy(GetCollectionPriority)
            .ThenBy(c => c.DisplayName);
    }

    private static int GetCollectionPriority(ITestCollection collection)
    {
        var name = collection.DisplayName ?? string.Empty;

        // Unit tests run first (priority 0)
        if (name.Contains("Unit", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // Integration tests run second (priority 1)
        if (name.Contains("Integration", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Harness", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Mouse", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Keyboard", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Window", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Screenshot", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Electron", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        // Everything else runs last (priority 2)
        return 2;
    }
}
