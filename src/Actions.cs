using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace GitIdentity;

internal static class Actions
{
    // ---------- helpers --------------------------------------------------

    private static ProfileStore Store() => new();

    private static int Error(string message)
    {
        Ansi.Err($"error: {message}\n");
        return 1;
    }

    private static int Note(string message)
    {
        Console.Error.WriteLine(message);
        return 0;
    }

    // ---------- help -----------------------------------------------------

    public static int Help()
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine($"{Ansi.Tag("git-identity", Ansi.Magenta)}  v1.1.0  {Ansi.Dim("(.NET 10 NativeAOT, Windows)")}");
        sb.AppendLine();
        sb.AppendLine("  Manage multiple Git identities (user.name, user.email, SSH key, GPG key)");
        sb.AppendLine("  stored in %APPDATA%\\git-identity\\profiles.json.");
        sb.AppendLine();

        sb.AppendLine(Ansi.Bold + "Manage profiles" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Cmd("add")} {Ansi.Dim("<id> <name> <email> [ssh] [gpg]")}    create a profile");
        sb.AppendLine($"  {Ansi.Cmd("add")} {Ansi.Dim("<id> --gh|--gl|--bb|--ado")}            create from provider CLI");
        sb.AppendLine($"  {Ansi.Cmd("mod")} {Ansi.Dim("<id> <name|email|ssh|gpg> <value>")}  edit one field");
        sb.AppendLine($"  {Ansi.Cmd("del")} {Ansi.Dim("<id>")}                                remove a profile");
        sb.AppendLine($"  {Ansi.Cmd("ls")} {Ansi.Dim("[--raw]")}                               list profiles");
        sb.AppendLine($"  {Ansi.Cmd("show")} {Ansi.Dim("[<id>]")}                             show one (current if no id)");

        sb.AppendLine();
        sb.AppendLine(Ansi.Bold + "Use in a repo" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Cmd("use")} {Ansi.Dim("<id>")}                                set the current repo to a profile");
        sb.AppendLine($"  {Ansi.Cmd("current")}                                       show which profile is active here");
        sb.AppendLine($"  {Ansi.Cmd("refresh")}                                       re-apply current profile (no-op if ok)");
        sb.AppendLine($"  {Ansi.Cmd("settings")}                                       show user.* / sshCommand in this repo");
        sb.AppendLine($"  {Ansi.Cmd("run")} {Ansi.Dim("<id> -- <cmd...>")}                  run a shell command with the profile's SSH");

        sb.AppendLine();
        sb.AppendLine(Ansi.Bold + "Provider login" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Cmd("gh")}  {Ansi.Dim("[<id>]")}                              log in to GitHub (uses `gh` CLI)");
        sb.AppendLine($"  {Ansi.Cmd("gl")}  {Ansi.Dim("[<id>]")}                              log in to GitLab (uses `glab` CLI)");
        sb.AppendLine($"  {Ansi.Cmd("bb")}  {Ansi.Dim("[<id>]")}                              open Bitbucket SSH keys page");
        sb.AppendLine($"  {Ansi.Cmd("ado")} {Ansi.Dim("[<id>]")}                              log in to Azure DevOps (uses `az` CLI)");

        sb.AppendLine();
        sb.AppendLine(Ansi.Bold + "Help" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Cmd("help")}                                         this overview");
        sb.AppendLine($"  {Ansi.Cmd("help add")}                                     details for the add verb");
        sb.AppendLine($"  {Ansi.Cmd("help mod")}                                     details for the mod verb");

        sb.AppendLine();
        sb.AppendLine(Ansi.Dim("Tips:"));
        sb.AppendLine(Ansi.Dim("  • use <id> outside a git repo just prints the profile; cd into a repo first to apply it."));
        sb.AppendLine(Ansi.Dim("  • SSH key may be a bare name (id_rsa_work -> ~/.ssh/id_rsa_work) or a full path."));
        sb.AppendLine(Ansi.Dim("  • Set NO_COLOR=1 to disable colors."));
        sb.AppendLine();
        Console.Out.Write(sb);
        return 0;
    }

    public static int HelpAdd()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Ansi.Tag("git identity add", Ansi.Magenta)}");
        sb.AppendLine();
        sb.AppendLine("Create a new profile and save it to your profile store.");
        sb.AppendLine();
        sb.AppendLine(Ansi.Bold + "Usage" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> <name> <email>")}            create from name + email");
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> <name> <email> <ssh-file>")} include an SSH key path");
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> <name> <email> <ssh> <gpg>")} include SSH and GPG signing key");
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> --gh")}                    auto-fill from `gh auth status`");
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> --gl")}                    auto-fill from `glab auth status`");
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> --bb")}                    open Bitbucket SSH-keys page (no auto-detect)");
        sb.AppendLine($"  {Ansi.Arg("add")} {Ansi.Dim("<id> --ado")}                   auto-fill from `az devops user show`");
        sb.AppendLine();
        sb.AppendLine(Ansi.Bold + "Examples" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Arg("add work")} \"Test User\" test@work.example id_ed25519");
        sb.AppendLine($"  {Ansi.Arg("add work")} --gh");
        sb.AppendLine();
        sb.AppendLine(Ansi.Dim("After 'add', run 'git identity use <id>' inside a repo to apply it."));
        sb.AppendLine();
        Console.Out.Write(sb);
        return 0;
    }

    public static int HelpMod()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Ansi.Tag("git identity mod", Ansi.Magenta)}");
        sb.AppendLine();
        sb.AppendLine("Edit one field of an existing profile.");
        sb.AppendLine();
        sb.AppendLine(Ansi.Bold + "Usage" + Ansi.Reset);
        sb.AppendLine($"  {Ansi.Arg("mod")} {Ansi.Dim("<id> name <new-name>")}");
        sb.AppendLine($"  {Ansi.Arg("mod")} {Ansi.Dim("<id> email <new-email>")}");
        sb.AppendLine($"  {Ansi.Arg("mod")} {Ansi.Dim("<id> ssh <ssh-file>")}");
        sb.AppendLine($"  {Ansi.Arg("mod")} {Ansi.Dim("<id> gpg <gpg-key>")}");
        sb.AppendLine();
        sb.AppendLine("If the current repo is using this profile, the local git config");
        sb.AppendLine("is refreshed automatically after the change.");
        sb.AppendLine();
        Console.Out.Write(sb);
        return 0;
    }

    // ---------- add / mod / del (new short verbs) ------------------------

    public static int Define(string id, string name, string email, string? sshKey, string? gpgKey)
    {
        if (string.IsNullOrEmpty(id)) return Error("add needs <id>");
        if (string.IsNullOrEmpty(name)) return Error("add needs <name>");
        if (string.IsNullOrEmpty(email)) return Error("add needs <email>");

        var store = Store();
        if (store.Get(id) is not null)
        {
            Ansi.Warn($"  ! identity '{id}' already exists. Use `git identity mod {id} <field> <value>` to update it.\n");
        }
        var profile = new Profile { Name = name, Email = email, SshKey = sshKey, SigningKey = gpgKey };
        store.Define(id, profile);
        var saved = store.Get(id);
        if (saved is null) return Error("failed to save profile");
        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Ok("✓")} added {PrintProfileShort(saved)}");
        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Dim($"Next:  git identity use {id}")}");
        return 0;
    }

    public static int DefineSsh(string id, string sshKey, int? verbosity)
    {
        if (string.IsNullOrEmpty(id)) return Error("--define-ssh needs <id>");
        if (string.IsNullOrEmpty(sshKey)) return Error("--define-ssh needs <ssh-file>");

        var store = Store();
        var existing = store.Get(id);
        if (existing is null) return Error($"cannot define SSH key for undefined identity '{id}'");

        var updated = existing with { SshKey = sshKey, SshVerbosity = verbosity };
        store.Define(id, updated);
        Console.WriteLine($"  {Ansi.Ok("✓")} added SSH key {Ansi.Arg(sshKey)} to {Ansi.Arg(id)}");
        if (ActiveProfileName() == id) UseIdentity(id);
        return 0;
    }

    public static int DefineGpg(string id, string gpgKey)
    {
        if (string.IsNullOrEmpty(id)) return Error("--define-gpg needs <id>");
        if (string.IsNullOrEmpty(gpgKey)) return Error("--define-gpg needs <gpgkey>");

        var store = Store();
        var existing = store.Get(id);
        if (existing is null) return Error($"cannot define GPG key for undefined identity '{id}'");

        var updated = existing with { SigningKey = gpgKey };
        store.Define(id, updated);
        Console.WriteLine($"  {Ansi.Ok("✓")} added GPG key {Ansi.Arg(gpgKey)} to {Ansi.Arg(id)}");
        if (ActiveProfileName() == id) UseIdentity(id);
        return 0;
    }

    public static int Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return Error("del needs <id>");
        var store = Store();
        var existing = store.Get(id);
        if (existing is null) return Error($"identity '{id}' does not exist");
        store.Remove(id);
        Console.WriteLine($"  {Ansi.Ok("✓")} removed {Ansi.Arg(id)} ({existing.Name} <{existing.Email}>)");
        return 0;
    }

    // ---------- list / show / current / settings -------------------------

    public static int List(bool rawOnly)
    {
        var names = Store().ListNames();
        if (names.Count == 0)
        {
            Console.WriteLine(Ansi.Dim("  (no profiles yet — run `git identity add` to create one)"));
            return 0;
        }
        if (rawOnly)
        {
            foreach (var n in names) Console.WriteLine(n);
            return 0;
        }
        var active = ActiveProfileName();
        var max = names.Max(n => n.Length);
        Console.WriteLine();
        foreach (var n in names)
        {
            var p = Store().Get(n);
            if (p is null) continue;
            var marker = (n == active) ? Ansi.Ok("●") : Ansi.Dim("○");
            var idPad = n.PadRight(max);
            Console.WriteLine($"  {marker} {Ansi.Bold}{idPad}{Ansi.Reset}  {p.Name} <{p.Email}>");
        }
        Console.WriteLine();
        Console.WriteLine(Ansi.Dim($"  ● active in this repo, ○ available"));
        return 0;
    }

    public static int Print(string? id)
    {
        if (string.IsNullOrEmpty(id)) id = ActiveProfileName();
        if (string.IsNullOrEmpty(id))
        {
            return Error("no current identity (use 'git identity <id>' from a repo, or pass an id).");
        }
        var profile = Store().Get(id);
        if (profile is null) return Error($"identity '{id}' does not exist");
        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Arg("id")}    {id}");
        Console.WriteLine($"  {Ansi.Arg("name")}  {profile.Name}");
        Console.WriteLine($"  {Ansi.Arg("email")} {profile.Email}");
        if (!string.IsNullOrEmpty(profile.SshKey))
            Console.WriteLine($"  {Ansi.Arg("ssh")}   {profile.SshKey}  {Ansi.Dim("-> " + SshPathResolver.Resolve(profile.SshKey!))}");
        if (!string.IsNullOrEmpty(profile.SigningKey))
            Console.WriteLine($"  {Ansi.Arg("gpg")}   {profile.SigningKey}");
        Console.WriteLine();
        return 0;
    }

    public static int Current()
    {
        var id = ActiveProfileName();
        if (string.IsNullOrEmpty(id))
        {
            // No active profile. Show all known so the user has something to pick.
            var names = Store().ListNames();
            if (names.Count == 0)
            {
                Console.WriteLine(Ansi.Dim("  (no profile set, none defined)"));
                return 0;
            }
            Console.WriteLine(Ansi.Dim("  (no profile set in this repo — try:") + " git identity use " + names[0] + Ansi.Dim(")"));
            return 0;
        }
        var profile = Store().Get(id);
        if (profile is null)
        {
            Console.WriteLine($"  {Ansi.Warn("?")} {Ansi.Arg(id)} {Ansi.Dim("(profile missing from local storage)")}");
            return 0;
        }
        Console.WriteLine($"  {Ansi.Ok("●")} {Ansi.Arg(id)}  {profile.Name} <{profile.Email}>");
        return 0;
    }

    public static int GetSettings()
    {
        if (!GitCli.InRepo())
        {
            Ansi.Warn("  ! not a git repository\n");
            return 0;
        }
        var name = GitCli.GetLocal("user.name");
        var email = GitCli.GetLocal("user.email");
        var signing = GitCli.GetLocal("user.signingkey");
        var ssh = GitCli.GetLocal("core.sshcommand");
        var gpgsign = GitCli.GetLocal("commit.gpgsign");
        var gpgfmt = GitCli.GetLocal("gpg.format");
        if (name is null && email is null && signing is null && ssh is null)
        {
            Console.WriteLine(Ansi.Dim("  (no git-identity-owned settings in this repo)"));
            return 0;
        }
        Console.WriteLine();
        if (name    is not null) Console.WriteLine($"  {Ansi.Arg("user.name")}        = {name}");
        if (email   is not null) Console.WriteLine($"  {Ansi.Arg("user.email")}       = {email}");
        if (signing is not null) Console.WriteLine($"  {Ansi.Arg("user.signingkey")}  = {signing}");
        if (gpgsign is not null) Console.WriteLine($"  {Ansi.Arg("commit.gpgsign")}   = {gpgsign}");
        if (gpgfmt  is not null) Console.WriteLine($"  {Ansi.Arg("gpg.format")}       = {gpgfmt}");
        if (ssh     is not null) Console.WriteLine($"  {Ansi.Arg("core.sshCommand")}  = {ssh}");
        Console.WriteLine();
        return 0;
    }

    public static int Update()
    {
        if (!GitCli.InRepo())
        {
            return Error("not a git repository. `refresh` only works inside a repo.");
        }
        var id = ActiveProfileName();
        if (string.IsNullOrEmpty(id))
        {
            return Error("no active identity in this repo. Use 'git identity use <id>' first.");
        }
        // Re-apply; this is a no-op if already correct.
        return UseIdentity(id!);
    }

    // ---------- use ------------------------------------------------------

    public static int UseIdentity(string id)
    {
        if (string.IsNullOrEmpty(id)) return Error("use <id>");

        var profile = Store().Get(id);
        if (profile is null)
        {
            Ansi.Err($"error: identity '{id}' does not exist. Use `git identity add {id} ...` first.\n");
            return 1;
        }

        if (!GitCli.InRepo())
        {
            // Outside a repo: we cannot write per-repo config. Tell the user
            // what we'd do, and where the profile is saved.
            Console.WriteLine();
            Console.WriteLine($"  {Ansi.Warn("!")} {Ansi.Dim("not a git repository — nothing to apply.")}");
            Console.WriteLine($"  {Ansi.Dim("cd into a repo and re-run this command to apply.")}");
            Console.WriteLine();
            Console.WriteLine($"  {Ansi.Arg("profile")}  {PrintProfileShort(profile)}");
            Console.WriteLine();
            return 0;
        }

        // Apply to local repo via git config (so git does the validation)
        GitCli.SetLocal("user.name",  profile.Name);
        GitCli.SetLocal("user.email", profile.Email);

        if (!string.IsNullOrEmpty(profile.SigningKey))
        {
            GitCli.SetLocal("user.signingkey", profile.SigningKey!);
            GitCli.SetLocal("commit.gpgsign", "true");
            GitCli.SetLocal("tag.gpgsign", "true");
            if (profile.SigningKey!.StartsWith("ssh-", StringComparison.Ordinal))
                GitCli.SetLocal("gpg.format", "ssh");
            else
                GitCli.UnsetLocal("gpg.format");
        }
        else
        {
            GitCli.UnsetLocal("user.signingkey");
            GitCli.SetLocal("commit.gpgsign", "false");
            GitCli.SetLocal("tag.gpgsign", "false");
            GitCli.UnsetLocal("gpg.format");
        }

        var sshCmd = BuildSshCommand(profile);
        if (sshCmd is not null)
            GitCli.SetLocal("core.sshCommand", sshCmd);
        else
            GitCli.UnsetLocal("core.sshCommand");

        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Ok("✓")} {Ansi.Arg("use")}  {Ansi.Arg(id)}  {profile.Name} <{profile.Email}>");
        if (!string.IsNullOrEmpty(profile.SshKey))
            Console.WriteLine($"  {Ansi.Dim("ssh:")}  {Ansi.Dim(SshPathResolver.Resolve(profile.SshKey!))}");
        Console.WriteLine();
        return 0;
    }

    // ---------- run ------------------------------------------------------

    public static int ShellCommand(string? id, IReadOnlyList<string> command)
    {
        if (string.IsNullOrEmpty(id)) id = ActiveProfileName();
        if (string.IsNullOrEmpty(id))
        {
            return Error("no identity specified and no active one in this repo.");
        }
        var profile = Store().Get(id);
        if (profile is null) return Error($"identity '{id}' does not exist");

        var sshCmd = BuildSshCommand(profile);

        if (command.Count == 0)
        {
            Console.WriteLine(string.IsNullOrEmpty(sshCmd)
                ? "GIT_SSH_COMMAND="
                : $"GIT_SSH_COMMAND=\"{sshCmd}\"");
            return 0;
        }

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = command[0],
            UseShellExecute = false,
        };
        for (int i = 1; i < command.Count; i++) psi.ArgumentList.Add(command[i]);
        if (!string.IsNullOrEmpty(sshCmd)) psi.EnvironmentVariables["GIT_SSH_COMMAND"] = sshCmd;

        try
        {
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return Error($"failed to start '{command[0]}'");
            p.WaitForExit();
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            return Error($"failed to start '{command[0]}': {ex.Message}");
        }
    }

    // ---------- provider login (gh / gl / bb / ado) ----------------------

    private static string ProviderSwitch(ProviderAuth.Provider p) => p switch
    {
        ProviderAuth.Provider.GitHub      => "gh",
        ProviderAuth.Provider.GitLab      => "gl",
        ProviderAuth.Provider.Bitbucket   => "bb",
        ProviderAuth.Provider.AzureDevOps => "ado",
        _ => "?"
    };

    /// <summary>
    /// `git identity gh` (or gl/bb/ado): show the user where to log in,
    /// optionally open the URL, attempt auto-detect from local CLI, and
    /// return. Does NOT modify a profile on its own — that's what
    /// `add <id> --gh` does, where the user has confirmed the id.
    /// </summary>
    public static int ProviderLogin(ProviderAuth.Provider provider, string[] rest)
    {
        var pname = provider.ToString().ToLowerInvariant();
        var url = ProviderAuth.SshKeySettingsUrl(provider);
        var newKeyUrl = ProviderAuth.NewSshKeyUrl(provider);

        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Tag(pname, Ansi.Cyan)}  provider login");
        Console.WriteLine();

        var auto = ProviderAuth.TryAutoDetect(provider, out var info);
        if (auto is not null)
        {
            Console.WriteLine($"  {Ansi.Ok("✓")} detected from local CLI: {info}");
            Console.WriteLine($"        {Ansi.Arg("name")}  {auto.Name}");
            Console.WriteLine($"        {Ansi.Arg("email")} {auto.Email}");
            Console.WriteLine();
            Console.WriteLine($"  {Ansi.Dim("To save as a profile, run:")}");
            var id = rest.Length > 0 ? rest[0] : pname;
            Console.WriteLine($"        {Ansi.Arg($"git identity add {id} --{pname}")}");
        }
        else if (info is not null)
        {
            Console.WriteLine($"  {Ansi.Warn("!")} {info}");
        }
        else
        {
            Console.WriteLine($"  {Ansi.Dim("No local CLI detected for this provider.")}");
            Console.WriteLine($"  {Ansi.Dim(ProviderAuth.LoginHint(provider))}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {Ansi.B("Add a new SSH key:")}   {Ansi.Arg(newKeyUrl)}");
        Console.WriteLine($"  {Ansi.B("Manage SSH keys:")}     {Ansi.Arg(url)}");
        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Dim("Tip:")} {Ansi.Dim("Once the key is registered, run `git identity add <id> --" + ProviderSwitch(provider) + "`")}");
        Console.WriteLine($"       {Ansi.Dim("(auto-fills name+email from your CLI session, then prompts for the key file).")}");
        Console.WriteLine();
        return 0;
    }

    /// <summary>
    /// `git identity add <id> --gh` (etc.) — auto-detect, prompt for the
    /// SSH key, save the profile.
    /// </summary>
    public static int ProviderAdd(string id, ProviderAuth.Provider provider, bool manual)
    {
        var pname = provider.ToString().ToLowerInvariant();

        var auto = ProviderAuth.TryAutoDetect(provider, out var info);
        if (auto is null)
        {
            Console.WriteLine();
            Console.WriteLine($"  {Ansi.Err($"x {pname}: cannot auto-detect identity")}");
            if (info is not null) Console.WriteLine($"  {Ansi.Dim(info)}");
            Console.WriteLine();
            Console.WriteLine($"  Add the SSH key at: {Ansi.Arg(ProviderAuth.NewSshKeyUrl(provider))}");
            Console.WriteLine($"  Then run:           {Ansi.Arg($"git identity add {id} <name> <email> <ssh-file>")}");
            Console.WriteLine();
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Ok("✓")} {pname} detected: {Ansi.Arg(auto.Name)} <{Ansi.Arg(auto.Email)}>");
        Console.WriteLine();

        // Ask for the SSH key path (optional). If the user has a default
        // naming pattern, suggest it.
        var defaultKey = pname switch
        {
            "github"      => "id_ed25519_github",
            "gitlab"      => "id_ed25519_gitlab",
            "bitbucket"   => "id_ed25519_bb",
            "azuredevops" => "id_ed25519_ado",
            _ => $"id_ed25519_{pname}",
        };
        Console.Write($"  {Ansi.Arg("ssh key")} {Ansi.Dim($"[default: {defaultKey} in ~/.ssh, Enter to accept, '-' to skip]")}: ");
        var keyInput = Console.ReadLine()?.Trim();
        string? sshKey = null;
        if (string.IsNullOrEmpty(keyInput)) keyInput = defaultKey;
        if (keyInput != "-") sshKey = keyInput;

        var profile = new Profile
        {
            Name  = auto.Name,
            Email = auto.Email,
            SshKey = sshKey,
        };
        Store().Define(id, profile);
        Console.WriteLine();
        Console.WriteLine($"  {Ansi.Ok("✓")} saved {Ansi.Arg(id)} -> {profile.Name} <{profile.Email}>" + (sshKey is null ? "" : $", ssh={sshKey}"));
        Console.WriteLine($"  {Ansi.Dim("Next:  cd into a repo and run:")} {Ansi.Arg($"git identity use {id}")}");
        Console.WriteLine();
        return 0;
    }

    // ---------- helpers --------------------------------------------------

    public static string? ActiveProfileName()
    {
        if (!GitCli.InRepo()) return null;
        var name = GitCli.GetLocal("user.name");
        var email = GitCli.GetLocal("user.email");
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email)) return null;
        foreach (var n in Store().ListNames())
        {
            var p = Store().Get(n);
            if (p is not null && p.Name == name && p.Email == email) return n;
        }
        return null;
    }

    private static string? BuildSshCommand(Profile p)
    {
        if (string.IsNullOrEmpty(p.SshKey)) return null;
        var resolved = SshPathResolver.Resolve(p.SshKey!);
        var vflag = p.SshVerbosity switch
        {
            1 => "-v ",
            2 => "-vvv ",
            _ => ""
        };
        return $"ssh {vflag}-i \"{resolved}\" -o IdentitiesOnly=yes -F /dev/null";
    }

    private static string PrintProfileShort(Profile p) =>
        $"{Ansi.Arg(p.Id)} {p.Name} <{p.Email}>" +
        (string.IsNullOrEmpty(p.SshKey) ? "" : $"  {Ansi.Dim("ssh:")} {p.SshKey}");

    // legacy compat: keep the same buffer-based printer, but it's only
    // called from List() now. Unused otherwise.
    private static void PrintProfile(Profile p)
    {
        Console.WriteLine($"  {PrintProfileShort(p)}");
    }
}
