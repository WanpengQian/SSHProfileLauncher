using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SSHProfileLauncher.Localization;
using SSHProfileLauncher.Models;
using SSHProfileLauncher.Services;

namespace SSHProfileLauncher.Views;

public partial class ProfileEditWindow : Window
{
    private readonly Profile _profile;
    private readonly string _bvSshPath;
    private readonly BitviseLauncher _launcher = new();

    private bool _suppress;          // ignore TextChanged logic during programmatic edits
    private bool _nameFollowsHost;   // while true, Name mirrors Host (until the user edits Name)

    /// <summary>Working copy is mutated in place; caller passes a clone for "edit".</summary>
    public ProfileEditWindow(Profile profile, string bvSshPath, bool isNew)
    {
        InitializeComponent();
        _profile = profile;
        _bvSshPath = bvSshPath;
        Title = Loc.T(isNew ? "NewTitle" : "EditTitle");

        _suppress = true;
        LoadFromProfile();
        _suppress = false;

        _nameFollowsHost = isNew;
        if (isNew)
            Loaded += (_, _) => HostBox.Focus();
    }

    private void HostBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppress || !_nameFollowsHost) return;
        _suppress = true;
        NameBox.Text = HostBox.Text;   // default the name to the host/IP
        _suppress = false;
    }

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppress) return;
        _nameFollowsHost = false;       // user customized the name; stop mirroring
    }

    private void LoadFromProfile()
    {
        NameBox.Text = _profile.Name;
        HostBox.Text = _profile.Host;
        PortBox.Text = _profile.Port.ToString();
        UserBox.Text = _profile.User;
        KeyLocBox.Text = _profile.KeyLocation;
        GroupBox.Text = _profile.Group;
        NoteBox.Text = _profile.Note;

        foreach (ComboBoxItem item in AuthBox.Items)
        {
            if ((string)item.Tag == _profile.Auth.ToString())
            {
                AuthBox.SelectedItem = item;
                break;
            }
        }
        if (AuthBox.SelectedItem is null)
            AuthBox.SelectedIndex = 0;

        UpdateLinkedText();
    }

    private AuthMethod SelectedAuth =>
        Enum.TryParse<AuthMethod>((string)((ComboBoxItem)AuthBox.SelectedItem).Tag, out var a)
            ? a : AuthMethod.Password;

    private void AuthBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KeyLabel is null) return; // during InitializeComponent
        bool isKey = SelectedAuth == AuthMethod.PublicKey;
        KeyLabel.Visibility = isKey ? Visibility.Visible : Visibility.Collapsed;
        KeyPanel.Visibility = isKey ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLinkedText()
    {
        if (_profile.HasLinkedProfile)
        {
            LinkedText.Text = string.Format(Loc.T("Adv_Linked"), _profile.ProfilePath);
            UnlinkBtn.IsEnabled = true;
        }
        else
        {
            LinkedText.Text = Loc.T("Adv_NotLinked");
            UnlinkBtn.IsEnabled = false;
        }
    }

    private bool TryCommit()
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show(this, Loc.T("Msg_NeedName"), Loc.T("Title_Info"), MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return false;
        }
        if (!_profile.HasLinkedProfile && string.IsNullOrWhiteSpace(HostBox.Text))
        {
            MessageBox.Show(this, Loc.T("Msg_NeedHost"), Loc.T("Title_Info"), MessageBoxButton.OK, MessageBoxImage.Warning);
            HostBox.Focus();
            return false;
        }

        int port = 22;
        if (!string.IsNullOrWhiteSpace(PortBox.Text) &&
            (!int.TryParse(PortBox.Text.Trim(), out port) || port < 1 || port > 65535))
        {
            MessageBox.Show(this, Loc.T("Msg_PortRange"), Loc.T("Title_Info"), MessageBoxButton.OK, MessageBoxImage.Warning);
            PortBox.Focus();
            return false;
        }

        _profile.Name = NameBox.Text.Trim();
        _profile.Host = HostBox.Text.Trim();
        _profile.Port = port;
        _profile.User = UserBox.Text.Trim();
        _profile.Auth = SelectedAuth;
        _profile.KeyLocation = string.IsNullOrWhiteSpace(KeyLocBox.Text) ? "a" : KeyLocBox.Text.Trim();
        _profile.Group = GroupBox.Text.Trim();
        _profile.Note = NoteBox.Text.Trim();
        return true;
    }

    private void OpenInBitvise_Click(object sender, RoutedEventArgs e)
    {
        // Commit current fields so the GUI opens pre-filled with what's on screen.
        if (!TryCommit()) return;
        try
        {
            _launcher.OpenInEditor(_profile, _bvSshPath);
            MessageBox.Show(this, Loc.T("Msg_OpenedInBitvise"), Loc.T("Title_Info"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.T("Title_CannotLaunchBitvise"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LinkProfile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = Loc.T("Dlg_SelectProfile"),
            Filter = Loc.T("Filter_Profile"),
        };
        if (dlg.ShowDialog(this) == true)
        {
            _profile.ProfilePath = dlg.FileName;
            UpdateLinkedText();
        }
    }

    private void UnlinkProfile_Click(object sender, RoutedEventArgs e)
    {
        _profile.ProfilePath = null;
        UpdateLinkedText();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCommit()) return;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
