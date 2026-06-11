using System;

namespace GitIdentity;

/// <summary>
/// Minimal ANSI color helper. Honors NO_COLOR (https://no-color.org/),
/// respects Console.IsOutputRedirected (no TTY -> no colors), and uses
/// truecolor ("pnpm-style" bold colors) where supported.
/// </summary>
internal static class Ansi
{
    public static bool Enabled { get; }

    static Ansi()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
        {
            Enabled = false;
            return;
        }
        try { Enabled = !Console.IsOutputRedirected; }
        catch { Enabled = false; }
    }

    public const string Reset   = "\x1b[0m";
    public const string Bold    = "\x1b[1m";
    public const string Faint   = "\x1b[2m";
    public const string Red     = "\x1b[31m";
    public const string Green   = "\x1b[32m";
    public const string Yellow  = "\x1b[33m";
    public const string Blue    = "\x1b[34m";
    public const string Magenta = "\x1b[35m";
    public const string Cyan    = "\x1b[36m";
    public const string BgGrey  = "\x1b[100m";
    public const string BgBlue  = "\x1b[44m";

    public static void W(string s) => Console.Write(s);

    public static string Tag(string label, string fg = Cyan) => Enabled ? $"{Bold}{fg}{label}{Reset}" : label;
    public static string Arg(string s)  => Enabled ? $"{Bold}{Yellow}{s}{Reset}" : s;
    public static string Cmd(string s)  => Enabled ? $"{Bold}{Cyan}{s}{Reset}"   : s;
    public static string B(string s)    => Enabled ? $"{Bold}{s}{Reset}"         : s;
    public static string Ok(string s)   => Enabled ? $"{Green}{s}{Reset}"   : s;
    public static string Warn(string s) => Enabled ? $"{Yellow}{s}{Reset}"  : s;
    public static string Err(string s)  => Enabled ? $"{Red}{s}{Reset}"     : s;
    public static string Dim(string s)  => Enabled ? $"{Faint}{s}{Reset}"   : s;
}
