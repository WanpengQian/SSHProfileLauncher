using System.IO;
using System.Text.Json;

namespace SSHProfileLauncher.Services;

/// <summary>
/// Loads and saves the profile library as JSON under
/// %APPDATA%\SSHProfileLauncher\library.json.
/// </summary>
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public string FilePath { get; }

    public ProfileStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SSHProfileLauncher");
        Directory.CreateDirectory(dir);
        FilePath = Path.Combine(dir, "library.json");
    }

    public Library Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new Library();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Library>(json, Options) ?? new Library();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable file: don't crash, start fresh but keep a backup.
            TryBackupCorrupt();
            return new Library();
        }
    }

    public void Save(Library library)
    {
        var json = JsonSerializer.Serialize(library, Options);
        // Write atomically to avoid truncating the library on a mid-write crash.
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, FilePath, overwrite: true);
        File.Delete(tmp);
    }

    private void TryBackupCorrupt()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Copy(FilePath, FilePath + ".bak", overwrite: true);
        }
        catch
        {
            // Best effort only.
        }
    }
}
