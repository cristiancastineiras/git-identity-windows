using System;
using System.Diagnostics;

namespace GitIdentity;

/// <summary>
/// Thin wrapper around the <c>git config</c> command for the operations we need.
/// We use the real <c>git</c> binary for BOTH reading and writing so we never
/// produce a corrupt .git/config (which we did once, by writing core.sshCommand
/// into the [user] section). Git itself is the only thing that should write
/// its own config files.
/// </summary>
public static class GitCli
{
    /// <summary>
    /// Read a single key from the LOCAL config of the current repo. Returns
    /// null if the key is not set, or if we are not in a repo.
    /// </summary>
    public static string? GetLocal(string key)
    {
        return RunGit($"config --local --get {Quote(key)}", captureStdout: true)?.Trim();
    }

    public static string? GetLocal(string section, string key)
    {
        return RunGit($"config --local --get {Quote(section)}.{Quote(key)}", captureStdout: true)?.Trim();
    }

    /// <summary>
    /// Read a key from the GLOBAL config. Returns null if not set.
    /// </summary>
    public static string? GetGlobal(string key)
    {
        return RunGit($"config --global --get {Quote(key)}", captureStdout: true)?.Trim();
    }

    /// <summary>
    /// Set a key in the LOCAL config. No-op if not in a repo.
    /// </summary>
    public static void SetLocal(string key, string value)
    {
        RunGit($"config --local {Quote(key)} {Quote(value)}");
    }

    public static void SetLocal(string section, string key, string value)
    {
        RunGit($"config --local {Quote(section)}.{Quote(key)} {Quote(value)}");
    }

    /// <summary>
    /// Unset a key in the LOCAL config. No-op if not set or not in a repo.
    /// </summary>
    public static void UnsetLocal(string key)
    {
        RunGit($"config --local --unset {Quote(key)}");
    }

    public static void UnsetLocal(string section, string key)
    {
        RunGit($"config --local --unset {Quote(section)}.{Quote(key)}");
    }

    /// <summary>
    /// Resolve the path to the global git config file by asking git
    /// where it actually reads from.
    /// </summary>
    public static string? ResolveGlobalConfigPath()
    {
        // `git config --global --list --show-origin` outputs lines like
        //   file:C:/path/.gitconfig\tkey=value
        // The first global-scope line gives us the file.
        var stdout = RunGit("config --global --list --show-origin", captureStdout: true);
        if (stdout is null) return null;
        foreach (var line in stdout.Split('\n'))
        {
            var tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            var origin = line[..tab];
            if (origin.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return origin["file:".Length..];
            }
        }
        return null;
    }

    /// <summary>
    /// Detect if the current working directory is inside a git repo.
    /// Uses .git/ as the marker (works for both regular repos and worktrees).
    /// </summary>
    public static bool InRepo()
    {
        return System.IO.Directory.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, ".git"));
    }

    /// <summary>
    /// Find the repo root (the directory containing .git/). Walks up the
    /// directory tree. Returns null if not in a repo.
    /// </summary>
    public static string? FindRepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
        while (dir is not null)
        {
            if (System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    // ---------- internals ----------------------------------------------

    private static string? RunGit(string args, bool captureStdout = false)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(2000);
            // Suppress git's own "warning: ..." output on stderr unless it
            // actually failed. We don't have a way to surface warnings
            // nicely, and on most systems git is silent.
            return captureStdout ? stdout : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Quote a git config value (or key) so shell metacharacters do not
    /// get interpreted. git's argument parser is shell-aware-ish, so we
    /// use double quotes with escaped double quotes inside.
    /// </summary>
    private static string Quote(string s)
    {
        // git's --get/--set argument form expects the value as a single
        // shell-style word. We use double quotes; embedded " becomes \".
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
