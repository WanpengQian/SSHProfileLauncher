using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using SSHProfileLauncher.Localization;
using SSHProfileLauncher.Models;
using SSHProfileLauncher.Services;
using SSHProfileLauncher.Views;

namespace SSHProfileLauncher;

public partial class MainWindow : Window
{
    private readonly ProfileStore _store = new();
    private readonly BitviseLauncher _launcher = new();
    private Library _library = new();
    private readonly ObservableCollection<Profile> _profiles = new();
    private ICollectionView _view = null!;
    private bool _suppressLang;

    public MainWindow()
    {
        InitializeComponent();
        Loc.LanguageChanged += RefreshTexts;
        Closed += (_, _) => Loc.LanguageChanged -= RefreshTexts;

        LoadLibrary();
        RefreshTexts();
        // Warn after the window is up, so the prompt appears over it.
        Loaded += (_, _) => CheckBitvisePresent();
    }

    // ---- Localization ----

    private void RefreshTexts()
    {
        ColName.Header = Loc.T("Col_Name");
        ColGroup.Header = Loc.T("Col_Group");
        ColHost.Header = Loc.T("Col_Host");
        ColPort.Header = Loc.T("Col_Port");
        ColUser.Header = Loc.T("Col_User");
        ColAuth.Header = Loc.T("Col_Auth");
        ColNote.Header = Loc.T("Col_Note");
        UpdateStatus();
        SyncLangBox();
    }

    private void SyncLangBox()
    {
        _suppressLang = true;
        foreach (ComboBoxItem it in LangBox.Items)
        {
            if ((string)it.Tag == Loc.Language)
            {
                LangBox.SelectedItem = it;
                break;
            }
        }
        _suppressLang = false;
    }

    private void LangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLang || LangBox.SelectedItem is not ComboBoxItem it) return;
        var code = (string)it.Tag;
        if (code == Loc.Language) return;

        Loc.SetLanguage(code); // raises LanguageChanged -> RefreshTexts
        _library.Language = code;
        _store.Save(_library);
    }

    // ---- Startup check ----

    private void CheckBitvisePresent()
    {
        if (BitviseLauncher.ResolveBvSshPath(_library.BvSshPath) is not null)
            return;

        var r = MessageBox.Show(this, Loc.T("Install_Prompt"), Loc.T("Install_Title"),
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (r == MessageBoxResult.Yes)
            OpenDownloadPage();
    }

    private void OpenDownloadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.bitvise.com/ssh-client-download",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.T("Title_CannotOpenBrowser"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- Library / list ----

    private void LoadLibrary()
    {
        _library = _store.Load();
        _profiles.Clear();
        foreach (var p in _library.Profiles)
            _profiles.Add(p);

        _view = CollectionViewSource.GetDefaultView(_profiles);
        _view.Filter = FilterProfile;
        _view.SortDescriptions.Add(new SortDescription(nameof(Profile.Group), ListSortDirection.Ascending));
        _view.SortDescriptions.Add(new SortDescription(nameof(Profile.Name), ListSortDirection.Ascending));
        Grid.ItemsSource = _view;

        UpdateStatus();
    }

    private bool FilterProfile(object obj)
    {
        if (obj is not Profile p) return false;
        var q = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(q)) return true;

        return Contains(p.Name, q) || Contains(p.Host, q) || Contains(p.User, q)
            || Contains(p.Group, q) || Contains(p.Note, q);
    }

    private static bool Contains(string? s, string q) =>
        s is not null && s.Contains(q, StringComparison.OrdinalIgnoreCase);

    private void UpdateStatus()
    {
        var bv = BitviseLauncher.ResolveBvSshPath(_library.BvSshPath);
        var bvText = bv ?? Loc.T("Status_BvNotFound");
        StatusText.Text = string.Format(Loc.T("Status_Format"), _profiles.Count, bvText, _store.FilePath);
    }

    private void Persist()
    {
        _library.Profiles = _profiles.ToList();
        _store.Save(_library);
        UpdateStatus();
    }

    private Profile? Selected => Grid.SelectedItem as Profile;

    private string? RequireBvSsh()
    {
        var bv = BitviseLauncher.ResolveBvSshPath(_library.BvSshPath);
        if (bv is null)
        {
            MessageBox.Show(this, Loc.T("Msg_BvMissing"), Loc.T("Title_BvMissing"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        return bv;
    }

    // ---- Toolbar handlers ----

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var bv = BitviseLauncher.ResolveBvSshPath(_library.BvSshPath) ?? "";
        var draft = new Profile { Name = "" };
        var dlg = new ProfileEditWindow(draft, bv, isNew: true) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _profiles.Add(draft);
            Persist();
            Grid.SelectedItem = draft;
            if (dlg.ConnectAfterSave)
                ConnectSelected();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void EditSelected()
    {
        if (Selected is null)
        {
            MessageBox.Show(this, Loc.T("Msg_SelectFirst"), Loc.T("Title_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var bv = BitviseLauncher.ResolveBvSshPath(_library.BvSshPath) ?? "";
        var working = Selected.Clone();
        var dlg = new ProfileEditWindow(working, bv, isNew: false) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            var idx = _profiles.IndexOf(Selected);
            _profiles[idx] = working;
            Persist();
            _view.Refresh();
            Grid.SelectedItem = working;
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) return;
        var r = MessageBox.Show(this,
            string.Format(Loc.T("Confirm_Delete"), Selected.Name), Loc.T("Title_ConfirmDelete"),
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            _profiles.Remove(Selected);
            Persist();
        }
    }

    private void Connect_Click(object sender, RoutedEventArgs e) => ConnectSelected();

    private void ConnectSelected()
    {
        if (Selected is null) return;
        var bv = RequireBvSsh();
        if (bv is null) return;

        var profile = ResolveUsername(Selected);
        if (profile is null) return; // user cancelled the username prompt

        // Activate English input right before launching so the terminal inherits
        // English (like mstsc inherits the launch-time input method).
        Interop.Ime.SwitchToEnglish(new WindowInteropHelper(this).Handle);

        try
        {
            _launcher.Connect(profile, bv);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.T("Title_CannotConnect"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// SSH usernames rarely match the Windows account, so if a profile has no user
    /// (and isn't delegating to a .tlp) ask for one before connecting instead of
    /// letting Bitvise silently default it. Returns null if the user cancels.
    /// </summary>
    private Profile? ResolveUsername(Profile selected)
    {
        if (selected.HasLinkedProfile || !string.IsNullOrWhiteSpace(selected.User))
            return selected;

        var dlg = new UsernamePromptWindow { Owner = this };
        if (dlg.ShowDialog() != true)
            return null;

        if (dlg.Remember)
        {
            selected.User = dlg.Username;
            Persist();
            _view.Refresh();
            return selected;
        }

        // Use a throwaway copy so an unsaved username doesn't mutate the stored profile.
        var transient = selected.Clone();
        transient.User = dlg.Username;
        return transient;
    }

    private void OpenInBitvise_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null) return;
        var bv = RequireBvSsh();
        if (bv is null) return;
        try
        {
            _launcher.OpenInEditor(Selected, bv);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.T("Title_CannotLaunchBitvise"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = Loc.T("Dlg_SelectBvSsh"),
            Filter = Loc.T("Filter_BvSsh"),
            FileName = "BvSsh.exe",
        };
        var current = BitviseLauncher.ResolveBvSshPath(_library.BvSshPath);
        if (current is not null)
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(current);

        if (dlg.ShowDialog(this) == true)
        {
            _library.BvSshPath = dlg.FileName;
            _store.Save(_library);
            UpdateStatus();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _view?.Refresh();

    private void Grid_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        // Entering the list via Tab: select the first row so the arrow keys work immediately.
        if (Grid.SelectedItem is null && Grid.Items.Count > 0)
            Grid.SelectedIndex = 0;
    }

    private void Grid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Enter connects the selected row instead of moving to the next row.
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            ConnectSelected();
            return;
        }

        // Treat the whole list as one Tab stop: Tab/Shift+Tab leave the grid instead of
        // walking cell-by-cell. Arrow keys fall through to normal row navigation.
        if (e.Key == System.Windows.Input.Key.Tab)
        {
            bool back = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
            (back ? LangBox : (System.Windows.Controls.Control)NewBtn).Focus();
            e.Handled = true;
        }
    }

    private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Only connect when the double-click landed on an actual data row,
        // not the column header or the empty area below the rows.
        if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) is null)
            return;
        if (Selected is not null)
            ConnectSelected();
    }

    private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d is not null)
        {
            if (d is T t) return t;
            d = VisualTreeHelper.GetParent(d);
        }
        return null;
    }
}
