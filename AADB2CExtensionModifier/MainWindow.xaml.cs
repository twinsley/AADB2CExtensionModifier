using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private string _tenantDomain;
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
            
            // Redirect Console output to Debug output
            Console.SetOut(new DebugTextWriter());
            
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

                // Get the tenant domain from Graph API
                ShowLoading(true, "Retrieving tenant information...");
                _tenantDomain = await Task.Run(() => _graphService.GetTenantDomain(_graphClient));

                if (string.IsNullOrEmpty(_tenantDomain))
                {
                    // Fallback: try to construct from tenant ID if it's not a GUID
                    if (!Guid.TryParse(tenantId, out _))
                    {
                        if (tenantId.Contains("."))
                        {
                            _tenantDomain = tenantId;
                        }
                        else
                        {
                            _tenantDomain = $"{tenantId}.onmicrosoft.com";
                        }
                        Console.WriteLine($"Using constructed tenant domain: {_tenantDomain}");
                    }
                    else
                    {
                        MessageBox.Show("Warning: Could not retrieve tenant domain from Graph API. You can manually edit it for B2C identity searches.",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                TenantDomainTextBox.Text = _tenantDomain ?? "Not detected";

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
                EditDomainButton.IsEnabled = true;
                TenantIdTextBox.IsEnabled = false;
                SearchGroupBox.IsEnabled = true;

                ShowLoading(false);

                MessageBox.Show($"Successfully authenticated!\n\nTenant: {tenantId}\nTenant Domain: {_tenantDomain ?? "Not detected"}\nUser: {me.DisplayName}\nB2C Extension App ID: {_b2cExtensionAppId ?? "Not found"}",
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
                _tenantDomain = null;
                _extensionAttributes.Clear();

                StatusTextBlock.Text = "Not connected";
                StatusTextBlock.Foreground = Brushes.Gray;
                SelectedUserTextBlock.Text = "None";
                UserIdTextBlock.Text = "";
                TenantDomainTextBox.Text = "";
                TenantDomainTextBox.IsReadOnly = true;
                TenantDomainTextBox.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                EditDomainButton.Content = "Edit";

                LoginButton.IsEnabled = true;
                LogoutButton.IsEnabled = false;
                EditDomainButton.IsEnabled = false;
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
                MessageBox.Show("Please enter a user email or identity value.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowLoading(true, "Searching for user...");

                string email = UserEmailTextBox.Text.Trim();
                var user = await Task.Run(() => _graphService.GetGraphUser(email, _graphClient, _tenantDomain));

                if (user == null)
                {
                    ShowLoading(false);
                    MessageBox.Show($"No user found with identity: {email}\n\nSearched in:\n- Email (mail)\n- User Principal Name\n- All identity values (B2C)", 
                        "User Not Found",
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

                Debug.WriteLine($"Attempting to load user with ID: {_currentUser.Id}");

                // First, discover what extension properties exist
                List<string> extensionPropertyNames = null;
                if (!string.IsNullOrEmpty(_b2cExtensionAppId))
                {
                    Debug.WriteLine("Discovering extension properties from B2C app...");
                    extensionPropertyNames = await Task.Run(() => _graphService.GetB2cExtensionPropertyNames(_graphClient, _b2cExtensionAppId));
                    Debug.WriteLine($"Discovered {extensionPropertyNames?.Count ?? 0} extension properties");
                }

                // Build the select query with base properties + extension properties
                var selectProperties = new List<string> { "id", "displayName", "mail", "userPrincipalName" };
                if (extensionPropertyNames != null && extensionPropertyNames.Count > 0)
                {
                    selectProperties.AddRange(extensionPropertyNames);
                    Debug.WriteLine($"Requesting user with {selectProperties.Count} properties including {extensionPropertyNames.Count} extensions");
                }

                // Get user with explicit property selection
                var user = await _graphClient.Users[_currentUser.Id].GetAsync(config =>
                {
                    config.QueryParameters.Select = selectProperties.ToArray();
                });

                Debug.WriteLine($"User retrieved successfully");
                Debug.WriteLine($"User.Mail: {user.Mail}");
                Debug.WriteLine($"User.DisplayName: {user.DisplayName}");
                Debug.WriteLine($"User AdditionalData count: {user.AdditionalData?.Count ?? 0}");
                
                // Debug: Log all additional data keys
                if (user.AdditionalData != null && user.AdditionalData.Count > 0)
                {
                    Debug.WriteLine("All user properties (from AdditionalData):");
                    foreach (var kvp in user.AdditionalData)
                    {
                        if (!kvp.Key.StartsWith("@odata"))
                        {
                            Debug.WriteLine($"  Key: {kvp.Key}, Value: {kvp.Value}");
                        }
                    }
                }

                // Get available extension attributes from the tenant (this may fail due to permissions, which is OK)
                List<IdentityUserFlowAttribute> availableAttributes = null;
                try
                {
                    availableAttributes = await Task.Run(() => _graphService.GetExtensionAttributes(_graphClient));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not retrieve IdentityUserFlowAttributes (may be missing permission): {ex.Message}");
                    // This is OK, we'll continue without the display names
                }

                Debug.WriteLine($"B2C Extension App ID: {_b2cExtensionAppId ?? "NULL"}");
                Debug.WriteLine($"Available attributes count: {availableAttributes?.Count ?? 0}");

                // Process extension attributes from AdditionalData
                if (user.AdditionalData != null && !string.IsNullOrEmpty(_b2cExtensionAppId))
                {
                    var extensionPrefix = $"extension_{_b2cExtensionAppId}_";
                    Debug.WriteLine($"Looking for extension attributes with prefix: {extensionPrefix}");

                    foreach (var kvp in user.AdditionalData)
                    {
                        if (kvp.Key.StartsWith(extensionPrefix) || (kvp.Key.StartsWith("extension_") && !kvp.Key.StartsWith("@")))
                        {
                            var attributeName = kvp.Key;
                            var shortName = kvp.Key.StartsWith(extensionPrefix) 
                                ? kvp.Key.Replace(extensionPrefix, "") 
                                : kvp.Key.Replace("extension_", "");
                            
                            // Try to find matching display name from available attributes
                            var matchingAttr = availableAttributes?.FirstOrDefault(a => a.Id == shortName);
                            var displayName = matchingAttr?.DisplayName ?? shortName;
                            var dataType = matchingAttr?.DataType?.ToString() ?? "String";

                            var value = kvp.Value?.ToString() ?? "";

                            Debug.WriteLine($"Found extension attribute: {attributeName} = {value}");

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

                Debug.WriteLine($"Total extension attributes added to grid: {_extensionAttributes.Count}");

                if (_extensionAttributes.Count == 0)
                {
                    var message = "No extension attributes found for this user.";
                    if (extensionPropertyNames == null || extensionPropertyNames.Count == 0)
                    {
                        message += "\n\nNo extension properties were discovered on the B2C extensions app. This could mean:\n" +
                                  "- No extension attributes have been created yet\n" +
                                  "- The application doesn't have permission to read the application registration";
                    }
                    else
                    {
                        message += $"\n\n{extensionPropertyNames.Count} extension properties were discovered, but the user doesn't have values for them.";
                    }
                    
                    MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                UpdateSaveButtonState();
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                Debug.WriteLine($"Error in LoadExtensionAttributesAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

        private void EditDomainButton_Click(object sender, RoutedEventArgs e)
        {
            if (TenantDomainTextBox.IsReadOnly)
            {
                TenantDomainTextBox.IsReadOnly = false;
                TenantDomainTextBox.Background = Brushes.White;
                EditDomainButton.Content = "Save";
                TenantDomainTextBox.Focus();
                TenantDomainTextBox.SelectAll();
            }
            else
            {
                var newDomain = TenantDomainTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(newDomain))
                {
                    MessageBox.Show("Tenant domain cannot be empty. For B2C identity searches, a valid domain is required.",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _tenantDomain = newDomain;
                TenantDomainTextBox.IsReadOnly = true;
                TenantDomainTextBox.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                EditDomainButton.Content = "Edit";
                
                MessageBox.Show($"Tenant domain updated to: {_tenantDomain}\n\nThis will be used for B2C identity searches.",
                    "Domain Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    
    // Helper class to redirect Console output to Debug output
    public class DebugTextWriter : System.IO.TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            Debug.Write(value);
        }

        public override void Write(string value)
        {
            Debug.Write(value);
        }

        public override void WriteLine(string value)
        {
            Debug.WriteLine(value);
        }
    }
}