using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using ClaudeLauncher.Models;

namespace ClaudeLauncher.Services;

public sealed record LaunchResult(bool Started, string? Error = null);

/// <summary>
/// Writes the CLAUDE.md file of a project and opens a terminal running the Claude Code CLI
/// in that folder.
/// </summary>
public sealed class LauncherService
{
    /// <summary>Win32 ERROR_CANCELLED, raised when the user declines the UAC prompt.</summary>
    private const int ErrorCancelled = 1223;

    /// <summary>Opens the terminal in the folder and runs the configured command.</summary>
    public LaunchResult Launch(string folder, AppSettings settings)
    {
        if (!Directory.Exists(folder))
        {
            return new LaunchResult(false, $"Dossier introuvable : {folder}");
        }

        if (FindOnPath("wt.exe") is null)
        {
            return new LaunchResult(false, "Windows Terminal (wt.exe) est introuvable.");
        }

        try
        {
            var process = Process.Start(BuildStartInfo(folder, settings));
            return process is null
                ? new LaunchResult(false, "Le terminal n'a pas démarré.")
                : new LaunchResult(true);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return new LaunchResult(false, "L'élévation en administrateur a été refusée.");
        }
        catch (Exception ex)
        {
            return new LaunchResult(false, $"Impossible de lancer le terminal : {ex.Message}");
        }
    }

    public static string? FindClaudeMd(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        foreach (var candidate in new[] { "CLAUDE.md", "claude.md", "Claude.md" })
        {
            var full = Path.Combine(folder, candidate);
            if (File.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    /// <summary>
    /// Writes the CLAUDE.md file of the project from a template text, replacing the
    /// supported placeholders. Returns the path of the created file.
    /// </summary>
    public static string CreateClaudeMd(string folder, string templateContent)
    {
        var name = new DirectoryInfo(folder).Name;
        var content = templateContent
            .Replace("{{PROJECT_NAME}}", name)
            .Replace("{{PROJECT_PATH}}", folder)
            .Replace("{{DATE}}", DateTime.Now.ToString("yyyy-MM-dd"));

        if (!content.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            content += Environment.NewLine;
        }

        var path = Path.Combine(folder, "CLAUDE.md");
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    /// <summary>
    /// Builds the start info of Windows Terminal, always elevated (UAC prompt). An elevated
    /// process ignores WorkingDirectory and starts in System32, so the folder is passed to
    /// wt.exe with -d.
    /// </summary>
    private static ProcessStartInfo BuildStartInfo(string folder, AppSettings settings) => new()
    {
        FileName = "wt.exe",
        Arguments = $"-d \"{folder}\" powershell.exe -NoExit -Command \"{settings.Command}\"",
        UseShellExecute = true,
        Verb = "runas",
        WorkingDirectory = folder
    };

    private static string? FindOnPath(string fileName)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var dir in paths)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            try
            {
                var full = Path.Combine(dir, fileName);
                if (File.Exists(full))
                {
                    return full;
                }
            }
            catch (ArgumentException)
            {
                // Malformed PATH entry, ignore it.
            }
        }

        return null;
    }
}
