using System;
using System.Collections.Generic;
using System.Linq;

namespace GitIdentity;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Dispatch(args);
        }
        catch (Exception ex)
        {
            Ansi.Err($"fatal: {ex.Message}\n");
            return 2;
        }
    }

    private static int Dispatch(string[] args)
    {
        if (args.Length == 0) return Actions.Current();

        var verb = args[0];
        var rest = args.Skip(1).ToArray();

        switch (verb)
        {
            // ---- top-level help ----
            case "-h":
            case "--help":
            case "help":
                if (rest.Length > 0)
                {
                    return rest[0] switch
                    {
                        "add" => Actions.HelpAdd(),
                        "mod" => Actions.HelpMod(),
                        _ => Actions.Help(),
                    };
                }
                return Actions.Help();

            // ---- new short verbs (pnpm-style) ----
            case "add":   return AddVerb(rest);
            case "mod":   return ModVerb(rest);
            case "del":   return DelVerb(rest);
            case "ls":
            case "list":  return Actions.List(rawOnly: rest.Contains("--raw"));
            case "use":   return Actions.UseIdentity(rest.Length > 0 ? rest[0] : "");
            case "current": case "cur": case "c": return Actions.Current();
            case "show":  return Actions.Print(rest.Length > 0 ? rest[0] : null);
            case "settings": case "st": return Actions.GetSettings();
            case "refresh": case "ref": return Actions.Update();
            case "run":   return Actions.ShellCommand(
                            id: rest.Length > 0 ? rest[0] : null,
                            command: rest.Length > 1 ? rest.Skip(1).ToArray() : Array.Empty<string>());
            case "gh":   return Actions.ProviderLogin(ProviderAuth.Provider.GitHub, rest);
            case "gl":   return Actions.ProviderLogin(ProviderAuth.Provider.GitLab, rest);
            case "bb":   return Actions.ProviderLogin(ProviderAuth.Provider.Bitbucket, rest);
            case "ado":  return Actions.ProviderLogin(ProviderAuth.Provider.AzureDevOps, rest);

            // ---- legacy flag-form verbs (kept for back-compat) ----
            case "-l":
            case "--list":         return Actions.List(rawOnly: false);
            case "-R":
            case "--list-raw":     return Actions.List(rawOnly: true);
            case "-s":
            case "--get-settings": return Actions.GetSettings();
            case "-u":
            case "--update":       return Actions.Update();
            case "-r":
            case "--remove":
                if (rest.Length < 1) return Error("--remove needs <id>");
                return Actions.Remove(rest[0]);
            case "-d":
            case "--define":
                if (rest.Length < 3) return Error("--define needs <id> <name> <email> [ssh-file [gpgkey]]");
                return Actions.Define(
                    id: rest[0], name: rest[1], email: rest[2],
                    sshKey: rest.Length > 3 ? rest[3] : null,
                    gpgKey: rest.Length > 4 ? rest[4] : null);
            case "--define-ssh":
                if (rest.Length < 2) return Error("--define-ssh needs <id> <ssh-file> [verbosity:0|1|2]");
                int? verbosity = null;
                if (rest.Length > 2 && int.TryParse(rest[2], out var v)) verbosity = v;
                return Actions.DefineSsh(rest[0], rest[1], verbosity);
            case "--define-gpg":
                if (rest.Length < 2) return Error("--define-gpg needs <id> <gpgkey>");
                return Actions.DefineGpg(rest[0], rest[1]);
            case "-p":
            case "--print":        return Actions.Print(rest.Length > 0 ? rest[0] : null);
            case "-c":
            case "--get-shell-command":
                return Actions.ShellCommand(
                    id: rest.Length > 0 ? rest[0] : null,
                    command: rest.Length > 1 ? rest.Skip(1).ToArray() : Array.Empty<string>());

            default:
                // bare positional: treat as `use <id>`
                return Actions.UseIdentity(verb);
        }
    }

    // ---------- "add" / "mod" verb dispatch -------------------------------

    private static int AddVerb(string[] rest)
    {
        // Forms supported:
        //   add <id> <name> <email> [ssh-file [gpgkey]]
        //   add <id> --gh        (use gh auth status)
        //   add <id> --gl        (use glab auth status)
        //   add <id> --bb        (manual URL hints)
        //   add <id> --ado       (use az account/devops)
        if (rest.Length == 0) return HelpAdd();
        var id = rest[0];

        ProviderAuth.Provider? provider = null;
        for (int i = 1; i < rest.Length; i++)
        {
            if (rest[i] == "--gh")  provider = ProviderAuth.Provider.GitHub;
            if (rest[i] == "--gl")  provider = ProviderAuth.Provider.GitLab;
            if (rest[i] == "--bb")  provider = ProviderAuth.Provider.Bitbucket;
            if (rest[i] == "--ado") provider = ProviderAuth.Provider.AzureDevOps;
        }

        if (provider is not null)
        {
            return Actions.ProviderAdd(id, provider.Value, manual: false);
        }

        if (rest.Length < 3) return Error("add <id> <name> <email> [ssh-file [gpgkey]]");
        return Actions.Define(
            id: id, name: rest[1], email: rest[2],
            sshKey: rest.Length > 3 ? rest[3] : null,
            gpgKey: rest.Length > 4 ? rest[4] : null);
    }

    private static int ModVerb(string[] rest)
    {
        if (rest.Length == 0) return HelpMod();
        var id = rest[0];
        var store = new ProfileStore();
        var existing = store.Get(id);
        if (existing is null) return Error($"identity '{id}' does not exist. Use 'git identity add {id} ...' first.");
        // mod <id> <name|email|ssh|gpg> <value>
        if (rest.Length < 3) return Error("mod <id> <field> <value>   (fields: name, email, ssh, gpg)");
        var field = rest[1].ToLowerInvariant();
        var value = rest[2];
        Profile updated = field switch
        {
            "name"  => existing with { Name = value },
            "email" => existing with { Email = value },
            "ssh"   => existing with { SshKey = value },
            "gpg"   => existing with { SigningKey = value },
            _ => existing,
        };
        if (ReferenceEquals(updated, existing)) return Error($"unknown field '{field}'. Use: name | email | ssh | gpg");
        store.Define(id, updated);
        Console.WriteLine($"  {Ansi.Ok("✓")} updated {Ansi.Arg(id)}.{field} = {Ansi.Arg(value)}");
        if (Actions.ActiveProfileName() == id) Actions.UseIdentity(id);
        return 0;
    }

    private static int DelVerb(string[] rest)
    {
        if (rest.Length < 1) return Error("del <id>");
        return Actions.Remove(rest[0]);
    }

    private static int HelpAdd()  { Actions.HelpAdd();  return 1; }
    private static int HelpMod()  { Actions.HelpMod();  return 1; }

    private static int Error(string msg)
    {
        Ansi.Err($"error: {msg}\n");
        Ansi.Dim("Run 'git identity help' for usage.\n");
        return 1;
    }
}
