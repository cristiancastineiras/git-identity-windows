using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitIdentity;

[JsonSerializable(typeof(Dictionary<string, Profile>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
internal partial class ProfileJsonContext : JsonSerializerContext { }

/// <summary>
/// Stores identity profiles in a JSON file at
/// <c>%APPDATA%\git-identity\profiles.json</c> on Windows, separate from
/// the git config. This keeps our storage format simple (no INI section
/// rules to get wrong) and avoids polluting the user's git config with
/// our own namespace.
///
/// Uses System.Text.Json source generation to be NativeAOT-friendly
/// (no reflection-based serialization).
/// </summary>
public sealed class ProfileStore
{
    public string Path { get; }

    public ProfileStore()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "git-identity");
        Directory.CreateDirectory(dir);
        Path = System.IO.Path.Combine(dir, "profiles.json");
    }

    public Dictionary<string, Profile> Load()
    {
        if (!File.Exists(Path)) return new(StringComparer.Ordinal);
        try
        {
            var json = File.ReadAllText(Path);
            if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.Ordinal);
            var loaded = JsonSerializer.Deserialize(json,
                ProfileJsonContext.Default.DictionaryStringProfile);
            if (loaded is null) return new(StringComparer.Ordinal);
            // The JSON dictionary key IS the profile id. Re-attach it to each
            // Profile (the Id property is [JsonIgnore] so it doesn't round-trip).
            var result = new Dictionary<string, Profile>(StringComparer.Ordinal);
            foreach (var kv in loaded)
            {
                if (kv.Value is not null)
                {
                    result[kv.Key] = kv.Value with { Id = kv.Key };
                }
            }
            return result;
        }
        catch
        {
            // If the file is corrupt, return empty rather than crashing.
            return new(StringComparer.Ordinal);
        }
    }

    public void Save(Dictionary<string, Profile> profiles)
    {
        var json = JsonSerializer.Serialize(profiles,
            ProfileJsonContext.Default.DictionaryStringProfile);
        File.WriteAllText(Path, json);
    }

    public Profile? Get(string id)
    {
        var profiles = Load();
        return profiles.TryGetValue(id, out var p) ? p : null;
    }

    /// <summary>
    /// Define or update a profile. The caller passes the id (the dictionary
    /// key) and a profile object whose <c>Name</c> is the human-readable
    /// name. The profile's <c>Id</c> field is set to the id on save.
    /// </summary>
    public void Define(string id, Profile profile)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("id is required", nameof(id));
        var profiles = Load();
        var normalized = profile with { Id = id };
        profiles[id] = normalized;
        Save(profiles);
    }

    public void Remove(string id)
    {
        var profiles = Load();
        profiles.Remove(id);
        Save(profiles);
    }

    public IReadOnlyList<string> ListNames()
    {
        var profiles = Load();
        var names = new List<string>(profiles.Keys);
        names.Sort(StringComparer.Ordinal);
        return names;
    }
}
