using System.IO;
using System.Text.Json;
using ClaudeLauncher.Models;

namespace ClaudeLauncher.Services;

/// <summary>
/// Persists the project history and the settings in %APPDATA%\ClaudeLauncher.
/// </summary>
public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _root;
    private readonly string _historyFile;
    private readonly string _settingsFile;
    private readonly string _templatesFile;

    public StorageService()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeLauncher");
        _historyFile = Path.Combine(_root, "history.json");
        _settingsFile = Path.Combine(_root, "settings.json");
        _templatesFile = Path.Combine(_root, "templates.json");
        Directory.CreateDirectory(_root);
    }

    public string HistoryFilePath => _historyFile;

    public string TemplatesFilePath => _templatesFile;

    public List<ProjectEntry> LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFile))
            {
                return new List<ProjectEntry>();
            }

            var json = File.ReadAllText(_historyFile);
            var entries = JsonSerializer.Deserialize<List<ProjectEntry>>(json, JsonOptions);
            return entries?
                .Where(e => !string.IsNullOrWhiteSpace(e.Path))
                .OrderByDescending(e => e.LastOpened)
                .ToList()
                ?? new List<ProjectEntry>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new List<ProjectEntry>();
        }
    }

    public void SaveHistory(IEnumerable<ProjectEntry> entries)
    {
        try
        {
            File.WriteAllText(_historyFile, JsonSerializer.Serialize(entries, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // History is a convenience, never fail the launch because of it.
        }
    }

    /// <summary>Loads the CLAUDE.md templates, seeding a default one on first run.</summary>
    public List<PromptTemplate> LoadTemplates()
    {
        try
        {
            if (!File.Exists(_templatesFile))
            {
                var seeded = new List<PromptTemplate> { PromptTemplate.CreateDefault() };
                SaveTemplates(seeded);
                return seeded;
            }

            var json = File.ReadAllText(_templatesFile);
            var templates = JsonSerializer.Deserialize<List<PromptTemplate>>(json, JsonOptions);
            return templates?
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .ToList()
                ?? new List<PromptTemplate>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new List<PromptTemplate>();
        }
    }

    public void SaveTemplates(IEnumerable<PromptTemplate> templates)
    {
        try
        {
            File.WriteAllText(_templatesFile, JsonSerializer.Serialize(templates, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Templates are user data but a failed write must not break the launch.
        }
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFile))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            File.WriteAllText(_settingsFile, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
