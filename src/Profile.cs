using System.Text.Json.Serialization;

namespace GitIdentity;

/// <summary>
/// A single identity profile as stored in the JSON file.
/// </summary>
public sealed record Profile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("email")]
    public string Email { get; init; } = "";

    [JsonPropertyName("ssh_key")]
    public string? SshKey { get; init; }

    [JsonPropertyName("ssh_verbosity")]
    public int? SshVerbosity { get; init; }

    [JsonPropertyName("signing_key")]
    public string? SigningKey { get; init; }

    /// <summary>
    /// The internal name (the key in the JSON dictionary).
    /// </summary>
    [JsonIgnore]
    public string Id { get; init; } = "";
}
