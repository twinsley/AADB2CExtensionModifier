using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using AADB2CExtensionModifier.Services;
using AADB2CExtensionModifier.Models;

namespace AADB2CExtensionModifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GraphServiceClient _graphClient;
        private GraphHandlerService _graphService;
        private User _currentUser;
        private string _b2cExtensionAppId;
        private ObservableCollection<ExtensionAttributeModel> _extensionAttributes;

        private readonly string[] _scopes = new[]
        {
            "User.Read.All",
            "User.ReadWrite.All",
            "Application.Read.All",
            "Directory.ReadWrite.All"
        };

        // Using Microsoft Graph Command Line Tools Client ID for interactive authentication
        private const string DefaultClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e";

        public MainWindow()
        {
            InitializeComponent();
            _graphService = new GraphHandlerService();
            _extensionAttributes = new ObservableCollection<ExtensionAttributeModel>();
            AttributesDataGrid.ItemsSource = _extensionAttributes;

            // Monitor for changes to enable Save button
            foreach (var attr in _extensionAttributes)
            {
                attr.PropertyChanged += Attribute_PropertyChanged;
            }
        }

        private void Attribute_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExtensionAttributeModel.IsModified))
            {
                UpdateSaveButtonState();
            }
        }

        private void UpdateSaveButtonState()
        {
            var modifiedCount = _extensionAttributes.Count(a => a.IsModified);
            SaveButton.IsEnabled = modifiedCount > 0;
            ModifiedCountTextBlock.Text = modifiedCount > 0 ? $"{modifiedCount} attribute(s) modified" : "";
        }

        private void ShowLoading(bool show, string message = "Loading...")
        {
            LoadingTextBlock.Text = message;
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TenantIdTextBox.Text))
            {
                MessageBox.Show("Please enter a Tenant ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowLoading(true, "Authenticating...");

                string tenantId = TenantIdTextBox.Text.Trim();

                // Create Graph client with interactive authentication
                _graphClient = await Task.Run(() => _graphService.GetGraphClient(tenantId, DefaultClientId, _scopes));

                // Test the connection
                var me = await _graphClient.Me.GetAsync();

                // Get B2C Extension App ID
                ShowLoading(true, "Loading B2C configuration...");
                _b2cExtensionAppId = await Task.Run(() => _graphService.GetB2cExtensionAppId(_graphClient));

                if (string.IsNullOrEmpty(_b2cExtensionAppId))
                {
                    MessageBox.Show("Warning: B2C extension app not found. Extension attributes may not be available.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                StatusTextBlock.Text = $"Connected as: {me.DisplayName} ({me.UserPrincipalName})";
                StatusTextBlock.Foreground = Brushes.Green;

                LoginButton.IsEnabled = false;
                LogoutButton.IsEnabled = true;
                TenantIdTextBox.IsEnabled = false;
                SearchGroupBox.IsEnabled = true;

                ShowLoading(false);

                MessageBox.Show($"Successfully authenticated!\n\nTenant: {tenantId}\nUser: {me.DisplayName}\nB2C Extension App ID: {_b2cExtensionAppId ?? "Not found"}",
                    "Authentication Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Authentication failed:\n\n{ex.Message}",
                    "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Authentication failed";
                StatusTextBlock.Foreground = Brushes.Red;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _graphClient = null;
                _currentUser = null;
                _b2cExtensionAppId = null;
                _extensionAttributes.Clear();

                StatusTextBlock.Text = "Not connected";
                StatusTextBlock.Foreground = Brushes.Gray;
                SelectedUserTextBlock.Text = "None";
                UserIdTextBlock.Text = "";

                LoginButton.IsEnabled = true;
                LogoutButton.IsEnabled = false;
                TenantIdTextBox.IsEnabled = true;
                SearchGroupBox.IsEnabled = false;
                UserInfoGroupBox.IsEnabled = false;
                AttributesGroupBox.IsEnabled = false;
                SaveButton.IsEnabled = false;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserEmailTextBox.Text))
            {
                MessageBox.Show("Please enter a user email address.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowLoading(true, "Searching for user...");

                string email = UserEmailTextBox.Text.Trim();
                var user = await Task.Run(() => _graphService.GetGraphUser(email, _graphClient));

                if (user == null)
                {
                    ShowLoading(false);
                    MessageBox.Show($"No user found with email: {email}", "User Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _currentUser = user;
                SelectedUserTextBlock.Text = $"{user.DisplayName} ({user.Mail ?? user.UserPrincipalName})";
                UserIdTextBlock.Text = user.Id;

                UserInfoGroupBox.IsEnabled = true;
                AttributesGroupBox.IsEnabled = true;

                // Load extension attributes
                await LoadExtensionAttributesAsync();

                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Error searching for user:\n\n{ex.Message}",
                    "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadExtensionAttributesAsync()
        {
            try
            {
                ShowLoading(true, "Loading extension attributes...");

                _extensionAttributes.Clear();

                // Get the user with all extension attributes
                var user = await _graphClient.Users[_currentUser.Id].GetAsync(config =>
                {
                    config.QueryParameters.Select = new[] { "*" };
                });

                // Get available extension attributes from the tenant
                var availableAttributes = await Task.Run(() => _graphService.GetExtensionAttributes(_graphClient));

                // Process extension attributes
                if (user.AdditionalData != null && !string.IsNullOrEmpty(_b2cExtensionAppId))
                {
                    var extensionPrefix = $"extension_{_b2cExtensionAppId}_";

                    foreach (var kvp in user.AdditionalData)
                    {
                        if (kvp.Key.StartsWith(extensionPrefix))
                        {
                            var attributeName = kvp.Key;
                            var shortName = kvp.Key.Replace(extensionPrefix, "");
                            
                            // Try to find matching display name from available attributes
                            var matchingAttr = availableAttributes?.FirstOrDefault(a => a.Id == shortName);
                            var displayName = matchingAttr?.DisplayName ?? shortName;
                            var dataType = matchingAttr?.DataType?.ToString() ?? "String";

                            var value = kvp.Value?.ToString() ?? "";

                            var attrModel = new ExtensionAttributeModel
                            {
                                AttributeName = attributeName,
                                DisplayName = displayName,
                                DataType = dataType,
                                Value = value,
                                OriginalValue = value
                            };

                            attrModel.PropertyChanged += Attribute_PropertyChanged;
                            _extensionAttributes.Add(attrModel);
                        }
                    }
                }

                // Also check for standard extension attributes (extension_* without B2C app ID)
                if (user.AdditionalData != null)
                {
                    foreach (var kvp in user.AdditionalData)
                    {
                        if (kvp.Key.StartsWith("extension_") && !kvp.Key.StartsWith($"extension_{_b2cExtensionAppId}_"))
                        {
                            var value = kvp.Value?.ToString() ?? "";
                            var attrModel = new ExtensionAttributeModel
                            {
                                AttributeName = kvp.Key,
                                DisplayName = kvp.Key.Replace("extension_", ""),
                                DataType = "String",
                                Value = value,
                                OriginalValue = value
                            };

                            attrModel.PropertyChanged += Attribute_PropertyChanged;
                            _extensionAttributes.Add(attrModel);
                        }
                    }
                }

                if (_extensionAttributes.Count == 0)
                {
                    MessageBox.Show("No extension attributes found for this user.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                UpdateSaveButtonState();
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Error loading extension attributes:\n\n{ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var modifiedAttributes = _extensionAttributes.Where(a => a.IsModified).ToList();

            if (modifiedAttributes.Count == 0)
            {
                MessageBox.Show("No changes to save.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to save changes to {modifiedAttributes.Count} attribute(s)?",
                "Confirm Save", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                ShowLoading(true, "Saving changes...");

                // Build the update object
                var updateUser = new User
                {
                    AdditionalData = new Dictionary<string, object>()
                };

                foreach (var attr in modifiedAttributes)
                {
                    updateUser.AdditionalData[attr.AttributeName] = attr.Value ?? string.Empty;
                }

                // Update the user
                await _graphClient.Users[_currentUser.Id].PatchAsync(updateUser);

                // Reset modified flags
                foreach (var attr in modifiedAttributes)
                {
                    attr.ResetOriginalValue();
                }

                UpdateSaveButtonState();
                ShowLoading(false);

                MessageBox.Show($"Successfully updated {modifiedAttributes.Count} attribute(s)!",
                    "Save Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Error saving changes:\n\n{ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
                return;

            var hasChanges = _extensionAttributes.Any(a => a.IsModified);

            if (hasChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Refreshing will discard them. Continue?",
                    "Confirm Refresh", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            await LoadExtensionAttributesAsync();
        }

        private void UserEmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private void AttributesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Add logic for when a row is selected
        }
    }
}