using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RdaWizard.Core.Settings;
using Xunit;

namespace RdaWizard.Core.Tests.Settings;

public class SettingsServiceTests : IDisposable
{
    private readonly string _root;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "rda-tests-" + Guid.NewGuid().ToString("N"));
        _service = new SettingsService(NullLogger<SettingsService>.Instance, _root);
    }

    [Fact]
    public async Task Defaults_WhenNoFileExists()
    {
        await _service.LoadAsync();
        Assert.Equal("system", _service.Current.Theme);
        Assert.Equal(100L * 1024 * 1024, _service.Current.PreviewMaxBytes);
        Assert.Equal(15, _service.Current.PreviewTimeoutSeconds);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTrips()
    {
        var settings = AppSettings.Default.With(theme: "dark", language: "zh-CN", collision: "skip");
        await _service.SaveAsync(settings);
        await _service.LoadAsync();
        Assert.Equal("dark", _service.Current.Theme);
        Assert.Equal("zh-CN", _service.Current.Language);
        Assert.Equal("skip", _service.Current.CollisionPolicy);
    }

    [Fact]
    public async Task CorruptFile_FallsBackToDefaults_NoCrash()
    {
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(_service.SettingsFilePath, "{ this is not json ]]");
        await _service.LoadAsync();
        Assert.Equal(AppSettings.Default.Theme, _service.Current.Theme);
    }

    [Fact]
    public async Task Save_RaisesChangedEvent()
    {
        AppSettings? observed = null;
        _service.Changed += (_, settings) => observed = settings;
        await _service.SaveAsync(AppSettings.Default.With(theme: "light"));
        Assert.NotNull(observed);
        Assert.Equal("light", observed!.Theme);
    }

    [Fact]
    public async Task SavedFile_IsValidJson()
    {
        await _service.SaveAsync(AppSettings.Default.With(theme: "dark"));
        var json = await File.ReadAllTextAsync(_service.SettingsFilePath);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("dark", doc.RootElement.GetProperty("Theme").GetString());
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Temp cleanup races are harmless.
        }
    }
}
