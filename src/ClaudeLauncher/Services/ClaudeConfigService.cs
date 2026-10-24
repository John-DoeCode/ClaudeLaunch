using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClaudeLauncher.Services;

/// <summary>
/// Reads and updates the Claude Code CLI configuration (~/.claude.json).
/// The CLI refuses to run "claude --rc" in a folder whose trust dialog has never been
/// accepted, so the launcher needs to know - and optionally set - that flag.
/// </summary>
public sealed class ClaudeConfigService
{
    private const string ProjectsKey = "projects";
    private const string TrustKey = "hasTrustDialogAccepted";

    public ClaudeConfigService(string? configPath = null)
    {
        ConfigPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude.json");
    }

    public string ConfigPath { get; }

    public bool ConfigExists => File.Exists(ConfigPath);

    /// <summary>The CLI stores project paths with forward slashes.</summary>
    public static string NormalizePath(string folder) =>
        folder.TrimEnd('\\', '/').Replace('\\', '/');

    /// <summary>
    /// True when the folder is already trusted. A missing or unreadable configuration is
    /// reported as trusted so the launcher never blocks on its own guess.
    /// </summary>
    public bool IsWorkspaceTrusted(string folder)
    {
        var root = TryLoad();
        if (root is null)
        {
            return true;
        }

        return FindProject(root, folder)?[TrustKey]?.GetValue<bool>() == true;
    }

    /// <summary>
    /// Marks the folder as trusted, creating its entry when needed. Returns false when the
    /// configuration could not be read or written; the file is replaced atomically and only
    /// after it has been parsed successfully.
    /// </summary>
    public bool TrustWorkspace(string folder)
    {
        var root = TryLoad();
        if (root is null)
        {
            return false;
        }

        if (root[ProjectsKey] is not JsonObject projects)
        {
            projects = new JsonObject();
            root[ProjectsKey] = projects;
        }

        var entry = FindProject(root, folder);
        if (entry is null)
        {
            entry = new JsonObject();
            projects[NormalizePath(folder)] = entry;
        }

        entry[TrustKey] = true;

        try
        {
            var temp = ConfigPath + ".launcher.tmp";
            File.WriteAllText(temp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, ConfigPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private JsonObject? TryLoad()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return null;
            }

            return JsonNode.Parse(File.ReadAllText(ConfigPath)) as JsonObject;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Finds the project entry, matching the path case-insensitively.</summary>
    private static JsonObject? FindProject(JsonObject root, string folder)
    {
        if (root[ProjectsKey] is not JsonObject projects)
        {
            return null;
        }

        var wanted = NormalizePath(folder);
        foreach (var (key, value) in projects)
        {
            if (string.Equals(key.TrimEnd('/'), wanted, StringComparison.OrdinalIgnoreCase)
                && value is JsonObject entry)
            {
                return entry;
            }
        }

        return null;
    }
}
