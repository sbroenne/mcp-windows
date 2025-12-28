using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.ElectronHarness;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Performance benchmark tests for UI Automation operations.
/// These tests measure and record execution times to track performance across changes.
/// </summary>
public sealed class UIAutomationPerformanceTests : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private readonly UIAutomationThread _staThread;
    private readonly UIAutomationService _automationService;
    private readonly MonitorService _monitorService;
    private readonly WindowService _windowService;
    private readonly List<PerformanceResult> _results = [];

    public UIAutomationPerformanceTests()
    {
        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        _monitorService = new MonitorService();

        var windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
        var windowActivator = new WindowActivator(windowConfiguration);
        _windowService = new WindowService(
            windowEnumerator,
            windowActivator,
            _monitorService,
            secureDesktopDetector,
            windowConfiguration);

        var mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            _monitorService,
            mouseService,
            keyboardService,
            _windowService,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        // Output results to console for capturing
        if (_results.Count > 0)
        {
            var json = JsonSerializer.Serialize(_results, s_jsonOptions);
            Console.WriteLine("=== PERFORMANCE RESULTS ===");
            Console.WriteLine(json);
            Console.WriteLine("=== END PERFORMANCE RESULTS ===");
        }

        _staThread.Dispose();
        _automationService.Dispose();
    }

    #region WinForms Performance Tests

    [Collection("UITestHarness")]
    public sealed class WinFormsPerformanceTests : IClassFixture<UITestHarnessFixture>, IDisposable
    {
        private readonly UITestHarnessFixture _fixture;
        private readonly UIAutomationService _automationService;
        private readonly UIAutomationThread _staThread;
        private readonly string _windowHandle;
        private readonly List<PerformanceResult> _results = [];

        public WinFormsPerformanceTests(UITestHarnessFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _fixture.BringToFront();
            Thread.Sleep(200);

            _windowHandle = _fixture.TestWindowHandleString;

            _staThread = new UIAutomationThread();

            var windowConfiguration = WindowConfiguration.FromEnvironment();
            var elevationDetector = new ElevationDetector();
            var secureDesktopDetector = new SecureDesktopDetector();
            var monitorService = new MonitorService();

            var windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
            var windowActivator = new WindowActivator(windowConfiguration);
            var windowService = new WindowService(
                windowEnumerator,
                windowActivator,
                monitorService,
                secureDesktopDetector,
                windowConfiguration);

            var mouseService = new MouseInputService();
            var keyboardService = new KeyboardInputService();

            _automationService = new UIAutomationService(
                _staThread,
                monitorService,
                mouseService,
                keyboardService,
                windowService,
                elevationDetector,
                NullLogger<UIAutomationService>.Instance);
        }

        public void Dispose()
        {
            OutputResults("WinForms");
            _staThread.Dispose();
            _automationService.Dispose();
        }

        private void OutputResults(string appType)
        {
            if (_results.Count > 0)
            {
                foreach (var result in _results)
                {
                    result.AppType = appType;
                }

                var json = JsonSerializer.Serialize(_results, s_jsonOptions);
                Console.WriteLine($"=== PERFORMANCE RESULTS ({appType}) ===");
                Console.WriteLine(json);
                Console.WriteLine($"=== END PERFORMANCE RESULTS ({appType}) ===");
            }
        }

        [Fact]
        public async Task WinForms_GetTree_Depth5_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.GetTreeAsync(
                windowHandle: _windowHandle,
                parentElementId: null,
                maxDepth: 5,
                controlTypeFilter: null);
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "GetTree",
                Parameters = "maxDepth=5",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] WinForms GetTree(5): {sw.ElapsedMilliseconds}ms, {result.Diagnostics?.ElementsScanned} elements");
        }

        [Fact]
        public async Task WinForms_GetTree_Depth10_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.GetTreeAsync(
                windowHandle: _windowHandle,
                parentElementId: null,
                maxDepth: 10,
                controlTypeFilter: null);
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "GetTree",
                Parameters = "maxDepth=10",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] WinForms GetTree(10): {sw.ElapsedMilliseconds}ms, {result.Diagnostics?.ElementsScanned} elements");
        }

        [Fact]
        public async Task WinForms_GetTree_Depth15_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.GetTreeAsync(
                windowHandle: _windowHandle,
                parentElementId: null,
                maxDepth: 15,
                controlTypeFilter: null);
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "GetTree",
                Parameters = "maxDepth=15",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] WinForms GetTree(15): {sw.ElapsedMilliseconds}ms, {result.Diagnostics?.ElementsScanned} elements");
        }

        [Fact]
        public async Task WinForms_Find_AllButtons_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "Button",
            });
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "Find",
                Parameters = "ControlType=Button",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                ElementsFound = result.Elements?.Length ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] WinForms Find(Button): {sw.ElapsedMilliseconds}ms, found {result.Elements?.Length}, scanned {result.Diagnostics?.ElementsScanned}");
        }

        [Fact]
        public async Task WinForms_Find_ByName_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Submit",
                ControlType = "Button",
            });
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "Find",
                Parameters = "Name=Submit,ControlType=Button",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                ElementsFound = result.Elements?.Length ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] WinForms Find(Submit): {sw.ElapsedMilliseconds}ms, found {result.Elements?.Length}, scanned {result.Diagnostics?.ElementsScanned}");
        }

        [Fact]
        public async Task WinForms_FindAndClick_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Submit",
                ControlType = "Button",
            });
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "FindAndClick",
                Parameters = "Name=Submit,ControlType=Button",
                DurationMs = sw.ElapsedMilliseconds,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] WinForms FindAndClick(Submit): {sw.ElapsedMilliseconds}ms");
        }
    }

    #endregion

    #region Electron Performance Tests

    [Collection("ElectronHarness")]
    public sealed class ElectronPerformanceTests : IClassFixture<ElectronHarnessFixture>, IDisposable
    {
        private readonly ElectronHarnessFixture _fixture;
        private readonly UIAutomationService _automationService;
        private readonly UIAutomationThread _staThread;
        private readonly string _windowHandle;
        private readonly List<PerformanceResult> _results = [];

        public ElectronPerformanceTests(ElectronHarnessFixture fixture)
        {
            _fixture = fixture;
            _fixture.BringToFront();
            Thread.Sleep(300);

            _windowHandle = _fixture.WindowHandleString;

            _staThread = new UIAutomationThread();

            var windowConfiguration = WindowConfiguration.FromEnvironment();
            var elevationDetector = new ElevationDetector();
            var secureDesktopDetector = new SecureDesktopDetector();
            var monitorService = new MonitorService();

            var windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
            var windowActivator = new WindowActivator(windowConfiguration);
            var windowService = new WindowService(
                windowEnumerator,
                windowActivator,
                monitorService,
                secureDesktopDetector,
                windowConfiguration);

            var mouseService = new MouseInputService();
            var keyboardService = new KeyboardInputService();

            _automationService = new UIAutomationService(
                _staThread,
                monitorService,
                mouseService,
                keyboardService,
                windowService,
                elevationDetector,
                NullLogger<UIAutomationService>.Instance);
        }

        public void Dispose()
        {
            OutputResults("Electron");
            _staThread.Dispose();
            _automationService.Dispose();
        }

        private void OutputResults(string appType)
        {
            if (_results.Count > 0)
            {
                foreach (var result in _results)
                {
                    result.AppType = appType;
                }

                var json = JsonSerializer.Serialize(_results, s_jsonOptions);
                Console.WriteLine($"=== PERFORMANCE RESULTS ({appType}) ===");
                Console.WriteLine(json);
                Console.WriteLine($"=== END PERFORMANCE RESULTS ({appType}) ===");
            }
        }

        [Fact]
        public async Task Electron_GetTree_Depth5_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.GetTreeAsync(
                windowHandle: _windowHandle,
                parentElementId: null,
                maxDepth: 5,
                controlTypeFilter: null);
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "GetTree",
                Parameters = "maxDepth=5",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] Electron GetTree(5): {sw.ElapsedMilliseconds}ms, {result.Diagnostics?.ElementsScanned} elements");
        }

        [Fact]
        public async Task Electron_GetTree_Depth10_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.GetTreeAsync(
                windowHandle: _windowHandle,
                parentElementId: null,
                maxDepth: 10,
                controlTypeFilter: null);
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "GetTree",
                Parameters = "maxDepth=10",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] Electron GetTree(10): {sw.ElapsedMilliseconds}ms, {result.Diagnostics?.ElementsScanned} elements");
        }

        [Fact]
        public async Task Electron_GetTree_Depth15_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.GetTreeAsync(
                windowHandle: _windowHandle,
                parentElementId: null,
                maxDepth: 15,
                controlTypeFilter: null);
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "GetTree",
                Parameters = "maxDepth=15",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] Electron GetTree(15): {sw.ElapsedMilliseconds}ms, {result.Diagnostics?.ElementsScanned} elements");
        }

        [Fact]
        public async Task Electron_Find_AllButtons_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "Button",
            });
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "Find",
                Parameters = "ControlType=Button",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                ElementsFound = result.Elements?.Length ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] Electron Find(Button): {sw.ElapsedMilliseconds}ms, found {result.Elements?.Length}, scanned {result.Diagnostics?.ElementsScanned}");
        }

        [Fact]
        public async Task Electron_Find_ByName_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Navigate Home",
                ControlType = "Button",
            });
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "Find",
                Parameters = "Name=Navigate Home,ControlType=Button",
                DurationMs = sw.ElapsedMilliseconds,
                ElementsScanned = result.Diagnostics?.ElementsScanned ?? 0,
                ElementsFound = result.Elements?.Length ?? 0,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] Electron Find(Navigate Home): {sw.ElapsedMilliseconds}ms, found {result.Elements?.Length}, scanned {result.Diagnostics?.ElementsScanned}");
        }

        [Fact]
        public async Task Electron_FindAndClick_Performance()
        {
            var sw = Stopwatch.StartNew();
            var result = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Navigate Home",
                ControlType = "Button",
            });
            sw.Stop();

            _results.Add(new PerformanceResult
            {
                Operation = "FindAndClick",
                Parameters = "Name=Navigate Home,ControlType=Button",
                DurationMs = sw.ElapsedMilliseconds,
                Success = result.Success,
            });

            Assert.True(result.Success);
            Console.WriteLine($"[PERF] Electron FindAndClick(Navigate Home): {sw.ElapsedMilliseconds}ms");
        }
    }

    #endregion

    public class PerformanceResult
    {
        public string AppType { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Parameters { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public int ElementsScanned { get; set; }
        public int ElementsFound { get; set; }
        public bool Success { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    }
}
