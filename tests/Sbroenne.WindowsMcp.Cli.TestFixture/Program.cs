using System.Globalization;
using System.Text.Json;

namespace Sbroenne.WindowsMcp.Cli.TestFixture;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
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
