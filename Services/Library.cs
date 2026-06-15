using SSHProfileLauncher.Models;

namespace SSHProfileLauncher.Services;

/// <summary>Root document persisted to library.json.</summary>
public sealed class Library
{
    /// <summary>Path to BvSsh.exe. Empty means auto-detect.</summary>
    public string BvSshPath { get; set; } = "";

    /// <summary>UI language code ("en" / "zh"). Empty means follow the system default.</summary>
    public string Language { get; set; } = "";

    public List<Profile> Profiles { get; set; } = new();
}
