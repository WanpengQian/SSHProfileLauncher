using System.Globalization;
using System.Windows;

namespace SSHProfileLauncher.Localization;

/// <summary>
/// Minimal runtime localization: swaps a string ResourceDictionary in the app
/// merged dictionaries so DynamicResource references update live, and exposes
/// T(key) for strings built in code-behind.
/// </summary>
public static class Loc
{
    public static readonly string[] Supported = { "en", "zh" };

    private static ResourceDictionary? _current;

    /// <summary>Raised after the language changes; subscribers refresh code-built text.</summary>
    public static event Action? LanguageChanged;

    public static string Language { get; private set; } = "en";

    /// <summary>Pick a sensible default when the user has not chosen one yet.</summary>
    public static string DefaultLanguage() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? "zh" : "en";

    public static void SetLanguage(string code)
    {
        if (Array.IndexOf(Supported, code) < 0)
            code = "en";

        var dict = new ResourceDictionary
        {
            Source = new Uri(
                $"/SSHProfileLauncher;component/Localization/Strings.{code}.xaml",
                UriKind.Relative),
        };

        var app = Application.Current;
        if (_current is not null)
            app.Resources.MergedDictionaries.Remove(_current);
        app.Resources.MergedDictionaries.Add(dict);
        _current = dict;

        Language = code;
        LanguageChanged?.Invoke();
    }

    /// <summary>Look up a string in the current language (falls back to the key).</summary>
    public static string T(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;
}
