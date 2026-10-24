using System.IO;
using System.Text.Json.Serialization;

namespace ClaudeLauncher.Models;

/// <summary>
/// One folder in which Claude Code CLI has been launched at least once.
/// </summary>
public sealed class ProjectEntry
{
    public string Path { get; set; } = string.Empty;

    public DateTime LastOpened { get; set; } = DateTime.Now;

    public int LaunchCount { get; set; }

    /// <summary>Optional user friendly name, defaults to the folder name.</summary>
    public string? Alias { get; set; }

    [JsonIgnore]
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Alias)
            ? Alias!
            : (System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar)) is { Length: > 0 } n
                ? n
                : Path);

    [JsonIgnore]
    public bool Exists => Directory.Exists(Path);

    [JsonIgnore]
    public bool HasClaudeMd => ClaudeMdPath is not null;

    /// <summary>Full path of the CLAUDE.md file if the folder already contains one.</summary>
    [JsonIgnore]
    public string? ClaudeMdPath
    {
        get
        {
            if (!Directory.Exists(Path))
            {
                return null;
            }

            // The file system is case insensitive on Windows, but be explicit about
            // the variants we accept so the check stays predictable.
            foreach (var candidate in new[] { "CLAUDE.md", "claude.md", "Claude.md" })
            {
                var full = System.IO.Path.Combine(Path, candidate);
                if (File.Exists(full))
                {
                    return full;
                }
            }

            return null;
        }
    }

    public override string ToString() => Path;
}
