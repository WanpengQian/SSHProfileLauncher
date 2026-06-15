using System.IO;
using System.Text.Json.Serialization;

namespace SSHProfileLauncher.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthMethod
{
    Password,
    PublicKey,
    KeyboardInteractive,
}

/// <summary>
/// One SSH connection entry in the library. Holds the common fields we edit
/// ourselves; anything advanced is delegated to a linked Bitvise .tlp profile.
/// </summary>
public sealed class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string User { get; set; } = "";
    public AuthMethod Auth { get; set; } = AuthMethod.Password;

    /// <summary>
    /// Bitvise -pk key location for public-key auth: "a" (any), "g1" (global key 1),
    /// or a path. Ignored unless Auth == PublicKey.
    /// </summary>
    public string KeyLocation { get; set; } = "a";

    public string Group { get; set; } = "";
    public string Note { get; set; } = "";

    /// <summary>
    /// Optional path to a Bitvise .tlp/.bscp profile saved via Bitvise's own editor.
    /// When set, connecting uses -profile= instead of the common command-line fields.
    /// </summary>
    public string? ProfilePath { get; set; }

    [JsonIgnore]
    public bool HasLinkedProfile =>
        !string.IsNullOrWhiteSpace(ProfilePath) && File.Exists(ProfilePath);

    /// <summary>Compact mark for the .tlp column (avoids a row-stretching checkbox).</summary>
    [JsonIgnore]
    public string LinkedMark => HasLinkedProfile ? "✓" : "";

    [JsonIgnore]
    public string AuthDisplay => Auth switch
    {
        AuthMethod.Password => "Password",
        AuthMethod.PublicKey => $"PublicKey ({KeyLocation})",
        AuthMethod.KeyboardInteractive => "Keyboard-interactive",
        _ => Auth.ToString(),
    };

    public Profile Clone() => (Profile)MemberwiseClone();
}
