# Azure AD B2C Extension Attribute Manager

A WPF desktop application for managing users in Microsoft Entra (Azure AD B2C), with support for viewing and updating extension attributes.

## Features

- **Interactive Authentication**: Uses browser-based authentication with tenant selection
- **User Search**: Find users by email or User Principal Name
- **View Extension Attributes**: Display all extension attributes for a selected user
- **Edit Extension Attributes**: Modify extension attribute values with validation
- **Change Tracking**: Visual indication of modified attributes before saving
- **B2C Support**: Automatically detects B2C extension app and formats attributes correctly

## Prerequisites

- .NET 8.0 or later
- Azure AD tenant with appropriate permissions
- User account with permissions to read and write user properties

## Required Permissions

The application requests the following Microsoft Graph API permissions:
- `User.Read.All` - Read all users
- `User.ReadWrite.All` - Read and write all users
- `Application.Read.All` - Read application registrations (for B2C extension app detection)
- `Directory.ReadWrite.All` - Read and write directory data

## How to Use

### 1. Authentication

1. Launch the application
2. Enter your **Tenant ID** in the format:
   - GUID format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
   - Domain format: `contoso.onmicrosoft.com`
3. Click **Login**
4. A browser window will open - complete the authentication process
5. Select your tenant in the authentication dialog if prompted
6. Once authenticated, the status will show your connected user

### 2. Search for a User

1. After authentication, the User Search section becomes enabled
2. Enter a user's **email address** or **User Principal Name** in the search box
3. Click **Search User** or press Enter
4. If found, the user's information will be displayed

### 3. View and Edit Extension Attributes

1. Once a user is selected, their extension attributes will be loaded automatically
2. The DataGrid shows:
   - **Attribute Name**: Full attribute name (e.g., `extension_abc123_CustomAttribute`)
   - **Display Name**: Friendly name of the attribute
   - **Data Type**: The attribute's data type (String, Boolean, Integer, DateTime)
   - **Current Value**: The current value (double-click to edit)
3. Modified attributes are tracked and counted at the bottom

### 4. Save Changes

1. Edit one or more attribute values in the DataGrid
2. The **Save Changes** button will become enabled
3. A counter shows how many attributes have been modified
4. Click **Save Changes** to persist the updates to Azure AD
5. Confirm the save operation in the dialog

### 5. Refresh Data

- Click **Refresh** to reload the user's extension attributes from Azure AD
- If you have unsaved changes, you'll be prompted to confirm

### 6. Logout

- Click **Logout** to disconnect from Azure AD
- This will clear all loaded data and return to the login screen

## Technical Details

### Architecture

- **MainWindow.xaml**: WPF UI with GroupBoxes for different sections
- **MainWindow.xaml.cs**: Application logic and event handlers
- **GraphHandlerService.cs**: Microsoft Graph API interaction layer
- **ExtensionAttributeModel.cs**: Data model with change tracking using INotifyPropertyChanged

### Authentication

The application uses `InteractiveBrowserCredential` from Azure.Identity for authentication:
- Default Client ID: `14d82eec-204b-4c2f-b7e8-296a70dab67e` (Microsoft Graph Command Line Tools)
- Redirect URI: `http://localhost`
- Tenant selection is handled in the browser authentication flow

### Extension Attribute Format

Azure AD B2C extension attributes follow this format:
```
extension_{B2C_APP_ID_WITHOUT_HYPHENS}_{ATTRIBUTE_NAME}
```

The application automatically:
1. Detects the B2C extension app (named `b2c-extensions-app`)
2. Extracts and formats the App ID
3. Filters and displays only extension attributes
4. Handles both B2C and standard extension attributes

## Troubleshooting

### Authentication Fails
- Ensure your Tenant ID is correct
- Check that you have appropriate permissions in the tenant
- Try logging out and logging back in

### B2C Extension App Not Found
- The app will still work but may not format attribute names optimally
- Ensure your tenant has a B2C configuration
- Check that the extension app exists and is named `b2c-extensions-app`

### User Not Found
- Verify the email address is correct
- The user must have a `mail` or `userPrincipalName` property
- Ensure you have `User.Read.All` permission

### Cannot Save Changes
- Check that you have `User.ReadWrite.All` permission
- Verify the attribute names are correct
- Ensure values match the expected data type

## Building from Source

```bash
# Clone the repository
git clone https://github.com/twinsley/AADB2CExtensionModifier.git

# Navigate to the project directory
cd AADB2CExtensionModifier

# Build the solution
dotnet build

# Run the application
dotnet run --project AADB2CExtensionModifier/AADB2CExtensionModifier.csproj
```

## License

MIT License - See LICENSE.txt for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
