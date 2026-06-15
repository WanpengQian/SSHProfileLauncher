using System.Windows;
using SSHProfileLauncher.Localization;

namespace SSHProfileLauncher.Views;

public partial class UsernamePromptWindow : Window
{
    public string Username => UserBox.Text.Trim();
    public bool Remember => RememberCheck.IsChecked == true;

    public UsernamePromptWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UserBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserBox.Text))
        {
            MessageBox.Show(this, Loc.T("Username_Empty"), Loc.T("Title_Info"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            UserBox.Focus();
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
