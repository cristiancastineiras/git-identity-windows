using System;
using System.IO;

namespace GitIdentity;

/// <summary>
/// Resolves an SSH key path according to the rules in the bash original:
///   - bare name (e.g. <c>id_rsa_work</c>)         -&gt; <c>%HOME%/.ssh/id_rsa_work</c>
///   - <c>~/.ssh/id_rsa</c>                          -&gt; <c>%HOME%/.ssh/id_rsa</c>
///   - <c>C:\keys\k</c>, <c>/c/keys/k</c>, UNC      -&gt; kept as-is
/// </summary>
public static class SshPathResolver
{
    public static string Resolve(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        // Tilde expansion
        if (raw[0] == '~')
        {
            var home = HomeDir();
            return home + raw.Substring(1);
        }

        // Absolute path (Windows drive, forward slash, UNC, or rooted)
        if (raw[0] == '\\' || raw[0] == '/') return raw;
        if (raw.Length >= 2 && raw[1] == ':') return raw; // C:\...

        // Bare name: prefix with ~/.ssh/
        return Path.Combine(HomeDir(), ".ssh", raw);
    }

    private static string HomeDir()
    {
        // Prefer HOME (git for Windows sets this to %USERPROFILE% typically),
        // fall back to USERPROFILE, then to a sane default.
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(home)) return home;
        var up = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrEmpty(up)) return up;
        return Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\";
    }
}
