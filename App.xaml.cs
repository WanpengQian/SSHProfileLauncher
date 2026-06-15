using System.IO;
using System.Windows;
using System.Windows.Threading;
using SSHProfileLauncher.Localization;
using SSHProfileLauncher.Services;

namespace SSHProfileLauncher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Surface (and log) any unhandled UI exception instead of silently killing the app.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogException(args.ExceptionObject as Exception);

        // Apply the saved language (or the system default) before any window loads.
        var saved = new ProfileStore().Load().Language;
        Loc.SetLanguage(string.IsNullOrEmpty(saved) ? Loc.DefaultLanguage() : saved);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        MessageBox.Show(
            $"发生错误：\n\n{e.Exception.Message}\n\n详细信息已写入：\n{LogPath}",
            "Bitvise Profile Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; // keep the app alive
    }

    private static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SSHProfileLauncher", "error.log");

    private static void LogException(Exception? ex)
    {
        if (ex is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:s}] {ex}\n\n");
        }
        catch
        {
            // Logging must never throw.
        }
    }
}
