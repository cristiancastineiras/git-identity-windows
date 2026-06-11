using System;
using System.Diagnostics;
using System.Text.Json;

namespace GitIdentity;

/// <summary>
/// Detect a git provider's local CLI ("gh", "glab", "az", etc.) and
/// read the logged-in user's identity (name, email, ssh key) from it.
/// If the CLI is installed and authenticated, we auto-fill the profile.
/// If not, we print the right URL (settings/security/keys) for the user
/// to log in and add a key manually, plus a ready-to-paste `git identity add`
/// command.
/// </summary>
internal static class ProviderAuth
{
    public enum Provider
    {
        GitHub,
        GitLab,
        Bitbucket,
        AzureDevOps,
    }

    /// <summary>
    /// Try to read the current user identity from the local CLI.
    /// Returns null if the CLI is missing, not logged in, or the
    /// provider doesn't expose a programmatic identity.
    /// </summary>
    public static Profile? TryAutoDetect(Provider provider, out string? info)
    {
        info = null;
        switch (provider)
        {
            case Provider.GitHub:        return FromGh(out info);
            case Provider.GitLab:        return FromGlab(out info);
            case Provider.AzureDevOps:   return FromAz(out info);
            case Provider.Bitbucket:     return null; // No public API; manual only
            default: return null;
        }
    }

    public static string SshKeySettingsUrl(Provider provider)
    {
        return provider switch
        {
            Provider.GitHub      => "https://github.com/settings/keys",
            Provider.GitLab      => "https://gitlab.com/-/profile/keys",
            Provider.Bitbucket   => "https://bitbucket.org/account/settings/ssh-keys/",
            Provider.AzureDevOps => "https://dev.azure.com/_usersSettings/keys",
            _ => ""
        };
    }

    public static string NewSshKeyUrl(Provider provider)
    {
        var s = SshKeySettingsUrl(provider);
        // Some providers (Bitbucket) end their settings page in a trailing slash.
        // Strip it so we can append "/new" cleanly.
        if (s.EndsWith("/")) s = s[..^1];
        return s + "/new";
    }

    public static string LoginHint(Provider provider)
    {
        return provider switch
        {
            Provider.GitHub      => "Run: gh auth login   (https://cli.github.com)",
            Provider.GitLab      => "Run: glab auth login (https://gitlab.com/gitlab-org/cli)",
            Provider.AzureDevOps => "Run: az login        (https://aka.ms/azuredevops-cli)",
            Provider.Bitbucket   => "Bitbucket has no public API for identity; add an SSH key at the URL above and run `git identity add` manually.",
            _ => ""
        };
    }

    // ---------- per-provider impls ----------------------------------------

    private static Profile? FromGh(out string? info)
    {
        info = null;
        if (!HasBinary("gh.exe")) return null;
        var (json, err, code) = Run("gh.exe", "api", "user");
        if (code != 0) { info = "gh api user failed: " + err.Trim(); return null; }
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            var name  = TryStr(r, "name");
            var email = TryStr(r, "email");
            var login = TryStr(r, "login") ?? "github-user";
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(email))
            {
                info = $"gh is authenticated as '{login}' but the user has no public name/email. Run: gh auth login --scopes user:email";
                return null;
            }
            info = $"gh user: {login}";
            return new Profile
            {
                Name  = string.IsNullOrEmpty(name)  ? login : name!,
                Email = string.IsNullOrEmpty(email) ? $"{login}@users.noreply.github.com" : email!,
                SshKey = null,
            };
        }
        catch (Exception ex)
        {
            info = "gh json parse failed: " + ex.Message;
            return null;
        }
    }

    private static Profile? FromGlab(out string? info)
    {
        info = null;
        if (!HasBinary("glab.exe")) return null;
        var (json, err, code) = Run("glab.exe", "api", "user");
        if (code != 0) { info = "glab api user failed: " + err.Trim(); return null; }
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            var name  = TryStr(r, "name");
            var email = TryStr(r, "email");
            var login = TryStr(r, "username") ?? TryStr(r, "login") ?? "gitlab-user";
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(email))
            {
                info = $"glab is authenticated as '{login}' but no public name/email. Run: glab auth login --scopes read_user";
                return null;
            }
            info = $"glab user: {login}";
            return new Profile
            {
                Name  = string.IsNullOrEmpty(name)  ? login : name!,
                Email = string.IsNullOrEmpty(email) ? $"{login}@users.noreply.gitlab.com" : email!,
                SshKey = null,
            };
        }
        catch (Exception ex)
        {
            info = "glab json parse failed: " + ex.Message;
            return null;
        }
    }

    private static string? TryStr(JsonElement e, string prop)
    {
        if (e.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            if (v.ValueKind == JsonValueKind.Number) return v.ToString();
        }
        return null;
    }

    private static Profile? FromAz(out string? info)
    {
        info = null;
        if (!HasBinary("az.exe")) return null;
        var (out1, err, code) = Run("az.exe", "account", "show", "-o", "json");
        if (code != 0) { info = "az account show failed: " + err; return null; }
        try
        {
            using var doc = JsonDocument.Parse(out1);
            var user = doc.RootElement.GetProperty("user");
            var name = user.TryGetProperty("name", out var n) ? n.GetString() : null;
            info = "az user: " + (user.TryGetProperty("name", out var n2) ? n2.GetString() : "(unknown)");
            // Azure DevOps uses a separate identity. Run: az devops user show
            var (out2, err2, code2) = Run("az.exe", "devops", "user", "show", "-o", "json");
            string? adoEmail = null;
            if (code2 == 0)
            {
                using var doc2 = JsonDocument.Parse(out2);
                adoEmail = doc2.RootElement.TryGetProperty("emailAddress", out var e) ? e.GetString() : null;
                if (string.IsNullOrEmpty(name))
                {
                    name = doc2.RootElement.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                }
            }
            return new Profile
            {
                Name  = name ?? "Azure DevOps User",
                Email = adoEmail ?? $"{user.GetProperty("name").GetString()}@dev.azure.com",
                SshKey = null,
            };
        }
        catch (Exception ex)
        {
            info = "az parse failed: " + ex.Message;
            return null;
        }
    }

    // ---------- shell helpers --------------------------------------------

    private static bool HasBinary(string name)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = name,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p!.WaitForExit();
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    private static (string outStr, string errStr, int code) Run(string file, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi)!;
            var so = p.StandardOutput.ReadToEnd();
            var se = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (so, se, p.ExitCode);
        }
        catch (Exception ex)
        {
            return ("", ex.Message, -1);
        }
    }
}
