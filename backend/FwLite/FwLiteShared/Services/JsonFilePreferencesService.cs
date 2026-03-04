using System.Text.Json;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

/// <summary>
/// JSON-file-backed preferences service.
/// Stores preferences in a JSON file alongside project data.
/// Works on all platforms without MAUI Essentials.
/// </summary>
public class JsonFilePreferencesService : IPreferencesService
{
    private readonly string _filePath;
    private readonly ILogger<JsonFilePreferencesService> _logger;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, string> _cache;
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    public JsonFilePreferencesService(IOptions<LcmCrdtConfig> config, ILogger<JsonFilePreferencesService> logger)
    {
        _filePath = Path.Combine(config.Value.ProjectPath, "preferences.json");
        _logger = logger;
        _cache = Load();
    }

    [JSInvokable]
    public string? Get(string key)
    {
        lock (_lock)
        {
            return _cache.GetValueOrDefault(key);
        }
    }

    [JSInvokable]
    public void Set(string key, string value)
    {
        lock (_lock)
        {
            _cache[key] = value;
            Save();
        }
    }

    [JSInvokable]
    public void Remove(string key)
    {
        lock (_lock)
        {
            if (_cache.Remove(key))
            {
                Save();
            }
        }
    }

    private Dictionary<string, string> Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to load preferences from {FilePath}", _filePath);
        }
        return [];
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null) Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(_cache, jsonOptions);
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
