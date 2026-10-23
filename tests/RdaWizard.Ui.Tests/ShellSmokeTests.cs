using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;
using Xunit;

namespace RdaWizard.Ui.Tests;

/// <summary>
/// UI smoke on Windows runners: launch the built app, verify the shell
/// (window title, home header, about navigation), capture screenshots for
/// the T4 deliverable (shell + pages as PNG artifacts), then exit. The
/// engine (rrecov) is absent in this environment — the home page must
/// surface its truth (error/empty state), never crash.
/// </summary>
public sealed class ShellSmokeTests : IDisposable
{
    private readonly Application? _app;
    private readonly UIA3Automation _automation = new();

    public ShellSmokeTests()
    {
        var exe = FindApp();
        if (exe is not null)
        {
            _app = Application.Launch(exe);
        }
    }

    [Fact]
    public void Shell_ShowsTitle()
    {
        Assert.NotNull(_app);
        var window = _app!.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        Assert.NotNull(window);
        Assert.Contains("RDA", window.Title, StringComparison.Ordinal);
        CaptureTo(window, "shell-home.png");
    }

    [Fact]
    public void Home_ShowsHeader()
    {
        Assert.NotNull(_app);
        var window = _app!.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        Assert.NotNull(window);

        var headline = window.FindFirstDescendant(cf => cf.ByText("Select a location to search for lost data"));
        Assert.NotNull(headline);
    }

    [Fact]
    public void About_Page_Opens()
    {
        Assert.NotNull(_app);
        var window = _app!.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        Assert.NotNull(window);

        var aboutButton = window.FindFirstDescendant(cf => cf.ByText("About"));
        Assert.NotNull(aboutButton);
        aboutButton.AsButton().Invoke();

        var aboutHeadline = window.FindFirstDescendant(cf => cf.ByText("Open-source licenses"));
        Assert.NotNull(aboutHeadline);
        CaptureTo(window, "shell-about.png");
    }

    private static void CaptureTo(Window window, string fileName)
    {
        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(directory);
            using var image = Capture.Screen();
            image.ToFile(Path.Combine(directory, fileName));
        }
        catch
        {
            // Screenshots are best-effort evidence, never test failures.
        }
    }

    private static string? FindApp()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "RdaWizard.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "RdaWizard.App", "bin", "Release", "net10.0-windows", "RdaWizard.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "RdaWizard.App", "bin", "Debug", "net10.0-windows", "RdaWizard.exe"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public void Dispose()
    {
        try
        {
            _app?.Kill();
        }
        catch
        {
            // App may have exited already.
        }
        _automation.Dispose();
    }
}
