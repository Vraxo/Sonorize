using IF.Lastfm.Core.Api;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections;

public partial class ScrobblingTab
{
    private bool _isSessionActive => !string.IsNullOrEmpty(AppSettings.Lastfm.SessionKey);
    private bool _isAuthenticating = false;
    private bool _showLogoutConfirmation = false;
    private string? _authError;
    private string _password = "";

    private void Save()
    {
        SettingsManager.Save(AppSettings);
    }

    private async Task Login()
    {
        if (!AuthService.IsAppIdConfigured)
        {
            _authError = "Missing API Keys.";
            return;
        }

        if (string.IsNullOrWhiteSpace(AppSettings.Lastfm.Username) || string.IsNullOrWhiteSpace(_password))
        {
            _authError = "Please enter username and password.";
            return;
        }

        _isAuthenticating = true;
        _authError = null;
        StateHasChanged();

        SettingsManager.Save(AppSettings);

        LastfmClient? client = await AuthService.AuthenticateWithCredentialsAsync(_password);

        _isAuthenticating = false;
        _password = ""; // Clear password from memory immediately

        if (client is not null)
        {
            // Success
        }
        else
        {
            _authError = "Authentication failed. Check credentials.";
        }
        StateHasChanged();
    }

    private void RequestLogout()
    {
        _showLogoutConfirmation = true;
    }

    private void CancelLogout()
    {
        _showLogoutConfirmation = false;
    }

    private void Logout()
    {
        AppSettings.Lastfm.SessionKey = null;
        _showLogoutConfirmation = false;
        SettingsManager.Save(AppSettings);
    }
}