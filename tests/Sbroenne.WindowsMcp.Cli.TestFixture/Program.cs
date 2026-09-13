using System.Globalization;
using System.Diagnostics;
using System.Text.Json;

namespace Sbroenne.WindowsMcp.Cli.TestFixture;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--handoff-primary", var directory, var count])
        {
            return RunReceiver(directory, int.Parse(count, CultureInfo.InvariantCulture));
        }

        if (args is ["--handoff-secondary", var target, var delay, var exitCode])
        {
            Thread.Sleep(int.Parse(delay, CultureInfo.InvariantCulture));
            WriteArguments(Path.Combine(target, "request.json"), ["owned local request"]);
            var deadline = Stopwatch.StartNew();
            while (!File.Exists(Path.Combine(target, "receipt.json")))
            {
                if (deadline.Elapsed > TimeSpan.FromSeconds(10))
                {
                    return 9;
                }
                Thread.Sleep(20);
            }
            return int.Parse(exitCode, CultureInfo.InvariantCulture);
        }

        if (args is ["--exit", var exitDelay, var code])
        {
            Thread.Sleep(int.Parse(exitDelay, CultureInfo.InvariantCulture));
            return int.Parse(code, CultureInfo.InvariantCulture);
        }

        // The persistent CLI owner must not rely on a later client's environment.
        const string RecordPrefix = "--record-arguments=";
        if (args.Length > 0 && args[^1].StartsWith(RecordPrefix, StringComparison.Ordinal))
        {
            WriteArguments(args[^1][RecordPrefix.Length..], args[..^1]);
            return 0;
        }

        var output = Environment.GetEnvironmentVariable("WINCLI_TEST_ARGUMENT_OUTPUT");
        if (!string.IsNullOrEmpty(output))
        {
            WriteArguments(output, args);
            return 0;
        }

        // Explicit fixture mode only: an accidental argument-less launch must do nothing.
        if (args is not ["--inert-window"])
        {
            return 2;
        }

        using var window = new InertWindow();
        using var timeout = new System.Windows.Forms.Timer { Interval = 60000 };
        timeout.Tick += (_, _) => window.Close();
        window.Shown += (_, _) =>
        {
            Console.WriteLine(window.Handle.ToInt64().ToString(CultureInfo.InvariantCulture));
            Console.Out.Flush();
            timeout.Start();
        };
        Application.Run(window);
        return 0;
    }

    private static int RunReceiver(string directory, int count)
    {
        var windows = Enumerable.Range(0, count).Select(_ => new InertWindow
        {
            Text = "Owned receiver - unrelated document title",
            ShowInTaskbar = true,
            Location = new Point(100, 100),
        }).ToArray();
        using var timer = new System.Windows.Forms.Timer { Interval = 25 };
        var lifetime = Stopwatch.StartNew();
        var received = false;
        timer.Tick += (_, _) =>
        {
            var request = Path.Combine(directory, "request.json");
            if (!received && File.Exists(request))
            {
                var arguments = JsonSerializer.Deserialize<string[]>(File.ReadAllText(request))!;
                WriteArguments(Path.Combine(directory, "receipt.json"), arguments);
                received = true;
            }
            if (lifetime.Elapsed > TimeSpan.FromMinutes(2))
            {
                windows[0].Close();
            }
        };
        windows[0].Shown += (_, _) =>
        {
            foreach (var window in windows.Skip(1))
            {
                window.Show();
            }
            Console.WriteLine(JsonSerializer.Serialize(windows.Select(w => w.Handle.ToInt64().ToString(CultureInfo.InvariantCulture))));
            Console.Out.Flush();
            WriteArguments(Path.Combine(directory, "owner.json"),
                [Environment.ProcessId.ToString(CultureInfo.InvariantCulture)]);
            timer.Start();
        };
        try
        {
            Application.Run(windows[0]);
            return 0;
        }
        finally
        {
            foreach (var window in windows)
            {
                window.Dispose();
            }
        }
    }

    private static void WriteArguments(string path, string[] arguments)
    {
        var pending = path + ".writing";
        File.WriteAllText(pending, JsonSerializer.Serialize(arguments));
        File.Move(pending, path, overwrite: true);
    }

    private sealed class InertWindow : Form
    {
        public InertWindow()
        {
            Text = "wincli inert test fixture";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-10000, -10000);
            Controls.Add(new Button { Text = "Inert", Name = "InertButton" });
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: never take desktop focus.
                return parameters;
            }
        }
    }
}
