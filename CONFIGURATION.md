# Configuration Guide

## Quick Start for Different Tenant Types

### Azure AD B2C Tenant
```
Tenant ID Format: yourtenant.onmicrosoft.com
Example: contosob2c.onmicrosoft.com
```

### Regular Azure AD Tenant
```
Tenant ID Format: yourtenant.onmicrosoft.com or GUID
Example: contoso.onmicrosoft.com
Example: a1b2c3d4-e5f6-a7b8-c9d0-e1f2a3b4c5d6
```

## Finding Your Tenant ID

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory**
3. Click **Overview** (or **Properties** in some views)
4. Copy the **Tenant ID** or **Directory ID**

## Setting Up Permissions

### For Administrator Consent

If you're a Global Administrator:
1. Log in for the first time
2. The consent screen will appear
3. Check "Consent on behalf of your organization"
4. Click Accept

### For Application Registration (Optional)

If you want to use a custom app registration:

1. Go to Azure Portal > Azure Active Directory > App registrations
2. Click **New registration**
3. Name: "B2C Extension Attribute Manager"
4. Supported account types: "Accounts in this organizational directory only"
5. Redirect URI: 
   - Platform: Public client/native
   - URI: `http://localhost`
6. Click **Register**

7. Go to **API permissions**
8. Add these Microsoft Graph permissions (Delegated):
   - User.Read.All
   - User.ReadWrite.All
   - Application.Read.All
   - Directory.ReadWrite.All
9. Click **Grant admin consent**

10. Copy the **Application (client) ID** and update the `DefaultClientId` constant in `MainWindow.xaml.cs`

## Extension Attribute Naming

### Azure AD B2C
Extension attributes are stored as:
```
extension_{appId}_{attributeName}
```

Example:
```
extension_1234567890abcdef1234567890abcdef_CustomField
```

### Creating Extension Attributes

To create new extension attributes in B2C:

1. Azure Portal > Azure AD B2C > User attributes
2. Click **Add**
3. Provide:
   - Name: CustomField
   - Data type: String/Boolean/Integer/DateTime
   - Description: (optional)

4. The attribute becomes available as:
   ```
   extension_{b2cAppId}_CustomField
   ```

## Common Tenant IDs for Testing

### Microsoft Demo Tenants
Microsoft provides demo tenants for testing. Contact your Microsoft representative for access.

### Personal B2C Tenant Setup
1. Create a free Azure account
2. Create an Azure AD B2C tenant
3. Note your tenant name: `{yourname}b2c.onmicrosoft.com`

## Troubleshooting Authentication

### "AADSTS50020: User account from identity provider does not exist"
- The user account doesn't exist in the specified tenant
- Verify you're using the correct tenant ID
- Ensure you have an account in that tenant

### "AADSTS65001: The user or administrator has not consented"
- Grant admin consent for the permissions
- Or have each user consent on first login

### "AADSTS7000215: Invalid client secret is provided"
- This shouldn't occur with public client flow
- If it does, verify the client ID is correct

### "AADSTS700016: Application not found in the directory"
- The client ID is incorrect
- Use the default Microsoft Graph CLI ID: `14d82eec-204b-4c2f-b7e8-296a70dab67e`

## Example Tenant Configurations

### Example 1: B2C Tenant
```
Tenant ID: contosob2c.onmicrosoft.com
Expected Users: customer@example.com
Extension Attributes: Yes (b2c-extensions-app detected)
```

### Example 2: Enterprise Tenant
```
Tenant ID: contoso.onmicrosoft.com (or GUID)
Expected Users: user@contoso.com
Extension Attributes: May or may not have B2C extension app
```

### Example 3: Multi-tenant App
```
Tenant ID: organizations (not recommended for this app)
Use: Specific tenant ID instead
```

## Security Best Practices

1. **Use Least Privilege**: Only grant necessary permissions
2. **Audit Changes**: Track who modifies extension attributes
3. **Regular Reviews**: Review user attributes periodically
4. **Secure Workstation**: Run the app on a secure, managed device
5. **Conditional Access**: Configure CA policies for admin accounts

## Data Type Validation

### String
- Any text value
- Max length: 256 characters for most attributes

### Boolean
- Valid values: `true`, `false`
- Case insensitive

### Integer
- Whole numbers only
- Range: -2,147,483,648 to 2,147,483,647

### DateTime
- Format: ISO 8601
- Example: `2024-01-15T10:30:00Z`
