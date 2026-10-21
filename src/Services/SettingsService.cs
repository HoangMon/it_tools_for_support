using System;
using System.IO;
using System.Text.Json;
using Ernastie.Models;

namespace Ernastie.Services;

public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private readonly string _settingsFilePath;
    public AppSettingsModel Settings { get; private set; }

    public event Action<AppSettingsModel>? SettingsChanged;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
        Settings = LoadSettings();
    }

    public AppSettingsModel LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettingsModel>(json);
                if (loaded != null)
                {
                    ValidateDefaults(loaded);
                    return loaded;
                }
            }
        }
        catch { }

        var defaults = new AppSettingsModel();
        ValidateDefaults(defaults);
        return defaults;
    }

    public void SaveSettings(AppSettingsModel newSettings)
    {
        ValidateDefaults(newSettings);
        Settings = newSettings;
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }

        SettingsChanged?.Invoke(Settings);
    }

    private static void ValidateDefaults(AppSettingsModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Theme)) model.Theme = "Dark";
        if (string.IsNullOrWhiteSpace(model.Language)) model.Language = "vi";
        if (string.IsNullOrWhiteSpace(model.AppFolder))
        {
            model.AppFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        }
        if (string.IsNullOrWhiteSpace(model.DriverFolder))
        {
            model.DriverFolder = Directory.Exists(@"D:\") ? @"D:\backupdriver" : @"C:\backupdriver";
        }
    }
}
