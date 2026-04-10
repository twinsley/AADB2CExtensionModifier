# Azure AD B2C User Attribute Manager

A Windows desktop tool for managing Azure AD B2C user accounts through the Microsoft Graph API. Built for administrators who need to inspect, edit, and troubleshoot B2C user attributes — including the notoriously opaque extension attributes — without writing Graph API calls by hand or navigating the Azure Portal's limited B2C user interface.

It can be found here in the [Windows App Store](https://apps.microsoft.com/detail/9mxx2kf7xtt8?hl=en-US&gl=US) for ease of install, or you can build it from source.

## What it does

- **Search for users** by email, UPN, or B2C identity value (e.g., a federated identity or local account sign-in name)
- **View and edit standard attributes** like display name, email, job title, phone numbers, address fields, and account status
- **View and edit extension attributes** — the custom attributes defined through B2C user flows or custom policies, which show up as `extension_{appId}_{attributeName}` in Graph
- **Browse sign-in logs** for a specific user, with detailed failure information including error codes, conditional access status, and risk levels
- **Browse audit logs** showing what changes have been made to a user, including which properties were modified and their old/new values
- **Delete users** (with double confirmation)
- **Toggle between UTC and local time** in the log tabs

The tool authenticates interactively through the browser using the Microsoft Graph Command Line Tools app registration, so there's no need to create your own app registration or manage client secrets.

## Requirements

- Windows 10 or later
- .NET 8 Desktop Runtime
- An Azure AD B2C tenant
- A user account with sufficient permissions (see [Permissions](#permissions) below)
- Azure AD Premium P1 or P2 for sign-in and audit log access

## Getting started

1. Clone the repository and build the solution in Visual Studio 2022 or later, or from the command line:

   ```
   dotnet build
   ```

2. Run the application.

3. In the **Tenant** field, enter one of the following:
   - Your tenant ID (a GUID like `12345678-1234-1234-1234-123456789012`)
   - Your tenant domain (like `contoso.onmicrosoft.com`)
   - Just the tenant short name (like `contoso` — it will append `.onmicrosoft.com` automatically)

4. Click **Login**. A browser window will open for interactive authentication. Sign in with an account that has admin permissions on the B2C tenant.

5. Once connected, the tenant domain is auto-detected from Graph. If it picks the wrong one (some tenants have multiple verified domains), click **Edit** next to the Tenant Domain field to override it. This matters for B2C identity searches.

6. Search for a user by typing their email address or identity value into the search box and clicking **Search User** (or pressing Enter).

## Tabs

### Standard Attributes

Shows common user properties (display name, email, phone, address, etc.). Editable fields can be modified directly in the grid. Read-only fields like User ID and User Type are shown in gray italic. A counter at the bottom tracks how many attributes you've modified. Click **Save Changes** to write them back, or **Refresh** to discard changes and reload from Graph.

### Extension Attributes

Shows all B2C extension attributes defined on the tenant's `b2c-extensions-app`. Attributes that have never been set for the user are shown with empty values so you can populate them. Values can be edited inline. The display name and data type columns are populated from the tenant's user flow attribute definitions when available.

Extension attributes follow the format:
```
extension_{b2cAppIdWithoutDashes}_{attributeName}
```

The tool discovers the B2C extensions app automatically and resolves the full attribute names.

### Sign-In Logs

Loads sign-in history for the selected user. Each entry includes:

- Application and resource names
- IP address and location
- Client app type
- Status (color-coded green/red)
- Error code and failure reason
- Additional details from Azure AD
- Conditional access status
- Risk level and risk detail

Results are paginated — click **Load More** to fetch additional pages. Use the **Show UTC time** checkbox to toggle the Date/Time column between your local timezone and UTC.

Sign-in logs require Azure AD Premium P1/P2 and the `AuditLog.Read.All` permission. Logs are retained for 7–30 days depending on license.

### Audit Logs

Shows directory audit events related to the selected user — attribute changes, password resets, policy updates, etc. Includes the activity name, who initiated the action, target resources, result status and reason, modified properties with old/new values, and a correlation ID for cross-referencing. Also paginated with a UTC toggle.

## Permissions

The application uses `InteractiveBrowserCredential` from Azure.Identity, authenticating against the Microsoft Graph Command Line Tools first-party app registration (client ID `14d82eec-204b-4c2f-b7e8-296a70dab67e`). This is pre-consented in most Azure AD tenants.

The following Graph API permissions are requested:

| Permission | Purpose |
|---|---|
| User.Read.All | Read user profiles and search for users |
| User.ReadWrite.All | Update user attributes |
| Application.Read.All | Discover the B2C extensions app and its extension properties |
| Directory.ReadWrite.All | Read/write directory data including extension attributes |
| IdentityUserFlow.Read.All | Read user flow attribute definitions (display names, data types) |
| AuditLog.Read.All | Read sign-in and audit logs |

If your tenant requires admin consent for these permissions, a Global Administrator will need to grant consent before the tool can be used.

## Settings

The tool saves your last-used tenant ID and tenant domain to `%APPDATA%\AADB2CExtensionModifier\settings.json` so you don't have to re-enter them each session. No credentials or tokens are stored.

## Project structure

```
AADB2CExtensionModifier/
  MainWindow.xaml / .cs          - Main window UI and event handlers
  App.xaml / .cs                 - Application entry point
  Services/
    GraphHandlerService.cs       - All Microsoft Graph API calls
    AppSettingsService.cs        - Settings file persistence
  Models/
    StandardAttributeModel.cs    - Model for standard user properties
    ExtensionAttributeModel.cs   - Model for B2C extension attributes
    SignInLogModel.cs            - Model for sign-in log entries
    AuditLogModel.cs             - Model for audit log entries
```

## Dependencies

- [Microsoft.Graph](https://www.nuget.org/packages/Microsoft.Graph) v5.98.0 — Microsoft Graph SDK
- [Azure.Identity](https://www.nuget.org/packages/Azure.Identity) v1.17.1 — Azure authentication
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) v10.0.1 — Settings serialization

## Troubleshooting

**Authentication fails**
- Double-check the tenant value. A GUID, full domain, or short name are all accepted.
- Make sure your account has the required permissions on the target tenant.
- If your tenant restricts the Microsoft Graph Command Line Tools app, a Global Admin may need to grant consent.

**B2C extension app not found**
- The tool looks for an application named `b2c-extensions-app` in the tenant. This is created automatically when you configure B2C, but won't exist in a standard Azure AD tenant.
- The tool will still work for standard and sign-in/audit log features; only extension attribute discovery is affected.

**User not found**
- The search checks the `mail` and `userPrincipalName` fields first, then falls back to B2C identity matching using the tenant domain as the issuer. Make sure the tenant domain is set correctly if you're searching for B2C local accounts.

**Sign-in or audit logs unavailable**
- These require Azure AD Premium P1 or P2 licensing on the tenant.
- The `AuditLog.Read.All` permission must be consented.

**UTC and local time show the same values**
- If your machine's timezone is set to UTC, the two formats will produce identical timestamps. This is expected.

## Known limitations

- Only supports Azure public cloud (`login.microsoftonline.com`). Sovereign clouds (US Gov, China, Germany) are not currently supported.
- Sign-in and audit logs require Azure AD Premium licensing.
- Extension attributes that have never been set for any user may not appear if the `b2c-extensions-app` doesn't have corresponding extension property definitions registered.

## License

See the repository for license information.

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
