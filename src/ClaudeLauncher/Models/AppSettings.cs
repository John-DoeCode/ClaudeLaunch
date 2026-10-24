using System.Text.Json.Serialization;

namespace ClaudeLauncher.Models;

public sealed class AppSettings
{
    /// <summary>true runs "claude --rc", false runs "claude".</summary>
    public bool UseRemoteMode { get; set; } = true;

    /// <summary>Identifier of the template selected last, preselected on start.</summary>
    public string? LastTemplateId { get; set; }

    /// <summary>Command sent to the terminal for the current mode.</summary>
    [JsonIgnore]
    public string Command => UseRemoteMode ? "claude --rc" : "claude";
}
