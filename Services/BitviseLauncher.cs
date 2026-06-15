using System.Diagnostics;
using System.IO;
using SSHProfileLauncher.Models;

namespace SSHProfileLauncher.Services;

/// <summary>
/// Resolves the BvSsh.exe location and launches Bitvise SSH Client either to
/// connect (-loginOnStartup) or to edit advanced settings (GUI, no auto-login).
/// </summary>
public sealed class BitviseLauncher
{
    private static readonly string[] CommonPaths =
    {
        @"C:\Program Files\Bitvise SSH Client\BvSsh.exe",
        @"C:\Program Files (x86)\Bitvise SSH Client\BvSsh.exe",
    };

    /// <summary>Returns a usable BvSsh.exe path, or null if none can be found.</summary>
    public static string? ResolveBvSshPath(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        foreach (var p in CommonPaths)
            if (File.Exists(p))
                return p;

        return ResolveFromPath();
    }

    private static string? ResolveFromPath()
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
            return null;

        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "BvSsh.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException)
            {
                // Skip malformed PATH entries.
            }
        }
        return null;
    }

    /// <summary>
    /// Connect and immediately open an interactive SSH terminal console window
    /// (Bitvise stermc.exe). Reuses the registry host-key store, so hosts already
    /// trusted in the GUI client connect without re-prompting.
    /// </summary>
    public void Connect(Profile profile, string bvSshPath)
    {
        var stermc = ResolveSibling(bvSshPath, "stermc.exe");
        var psi = new ProcessStartInfo
        {
            FileName = stermc,
            WorkingDirectory = Path.GetDirectoryName(stermc) ?? "",
            // ShellExecute gives the console app its own fresh console window
            // (a WPF process has no console for the child to inherit otherwise).
            UseShellExecute = true,
        };
        AddConnectionArgs(psi, profile);
        if (!string.IsNullOrWhiteSpace(profile.Name))
            psi.ArgumentList.Add($"-title={profile.Name}");
        Process.Start(psi);
    }

    /// <summary>Resolve a sibling Bitvise tool (e.g. stermc.exe) next to BvSsh.exe.</summary>
    private static string ResolveSibling(string bvSshPath, string toolName)
    {
        if (string.IsNullOrWhiteSpace(bvSshPath) || !File.Exists(bvSshPath))
            throw new FileNotFoundException(
                "找不到 Bitvise 安装目录，请在「设置」中指定 BvSsh.exe 的路径。", bvSshPath);

        var dir = Path.GetDirectoryName(bvSshPath)!;
        var tool = Path.Combine(dir, toolName);
        if (!File.Exists(tool))
            throw new FileNotFoundException($"在 Bitvise 目录中找不到 {toolName}。", tool);
        return tool;
    }

    /// <summary>
    /// Launch the Bitvise GUI with fields pre-filled but without logging in, so the
    /// user can configure advanced settings and "Save profile" in Bitvise's own editor.
    /// </summary>
    public void OpenInEditor(Profile profile, string bvSshPath)
    {
        var psi = NewStartInfo(bvSshPath);
        AddConnectionArgs(psi, profile);
        Process.Start(psi);
    }

    private static ProcessStartInfo NewStartInfo(string bvSshPath)
    {
        if (string.IsNullOrWhiteSpace(bvSshPath) || !File.Exists(bvSshPath))
            throw new FileNotFoundException(
                "找不到 BvSsh.exe，请在「设置」中指定 Bitvise SSH Client 的路径。", bvSshPath);

        return new ProcessStartInfo
        {
            FileName = bvSshPath,
            WorkingDirectory = Path.GetDirectoryName(bvSshPath) ?? "",
            UseShellExecute = false,
        };
    }

    private static void AddConnectionArgs(ProcessStartInfo psi, Profile p)
    {
        // A linked .tlp carries everything; let Bitvise own the details.
        if (p.HasLinkedProfile)
        {
            psi.ArgumentList.Add($"-profile={p.ProfilePath}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(p.Host))
            psi.ArgumentList.Add($"-host={p.Host}");
        if (p.Port > 0)
            psi.ArgumentList.Add($"-port={p.Port}");
        if (!string.IsNullOrWhiteSpace(p.User))
            psi.ArgumentList.Add($"-user={p.User}");

        switch (p.Auth)
        {
            case AuthMethod.PublicKey:
                var loc = string.IsNullOrWhiteSpace(p.KeyLocation) ? "a" : p.KeyLocation;
                psi.ArgumentList.Add($"-pk={loc}");
                break;
            case AuthMethod.KeyboardInteractive:
                psi.ArgumentList.Add("-kbdi");
                break;
            case AuthMethod.Password:
            default:
                // No password stored: Bitvise prompts interactively.
                break;
        }
    }
}
