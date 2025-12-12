# Standard Attributes Feature - Implementation Summary

## Overview
Added functionality to view and edit standard user attributes in addition to extension attributes. The attributes are now organized in separate tabs for better organization.

## Changes Made

### 1. New Model Class
**File:** `AADB2CExtensionModifier\Models\StandardAttributeModel.cs`
- Created a new model class similar to `ExtensionAttributeModel`
- Supports read-only properties (e.g., User ID, User Type)
- Implements `INotifyPropertyChanged` for change tracking
- Tracks modifications to enable/disable the Save button

### 2. Updated UI (XAML)
**File:** `AADB2CExtensionModifier\MainWindow.xaml`
- Changed the title from "Extension Attribute Manager" to "User Attribute Manager"
- Replaced the single DataGrid with a `TabControl` containing two tabs:
  - **Extension Attributes Tab:** Shows custom B2C extension attributes
  - **Standard Attributes Tab:** Shows standard user properties
- Each tab has its own:
  - DataGrid with appropriate columns
  - Refresh button
  - Save button
  - Modified count indicator
- The Standard Attributes DataGrid uses templates to visually distinguish read-only fields (gray, italic)

### 3. Updated Code-Behind
**File:** `AADB2CExtensionModifier\MainWindow.xaml.cs`
- Added `_standardAttributes` collection to manage standard user properties
- Created `LoadStandardAttributesAsync()` method that loads 20 common user attributes:
  - Basic Info: ID, Display Name, Given Name, Surname
  - Contact: Email, UPN, Mobile Phone, Business Phones
  - Work Info: Job Title, Department, Office Location, Employee ID
  - Address: Street Address, City, State, Postal Code, Country
  - Other: Company Name, User Type, Account Enabled
- Added `StandardSaveButton_Click()` method to handle saving standard attributes
  - Maps model properties to User object properties
  - Handles special cases like `businessPhones` (collection) and `accountEnabled` (boolean)
- Added `StandardRefreshButton_Click()` method
- Created `StandardAttribute_PropertyChanged()` and `UpdateStandardSaveButtonState()` for change tracking
- Updated logout to clear both collections

## Standard Attributes Included

| Property Name | Display Name | Data Type | Editable |
|--------------|--------------|-----------|----------|
| id | User ID | String | No |
| displayName | Display Name | String | Yes |
| givenName | Given Name | String | Yes |
| surname | Surname | String | Yes |
| mail | Email | String | Yes |
| userPrincipalName | User Principal Name | String | Yes |
| mobilePhone | Mobile Phone | String | Yes |
| businessPhones | Business Phones | StringCollection | Yes |
| jobTitle | Job Title | String | Yes |
| department | Department | String | Yes |
| officeLocation | Office Location | String | Yes |
| streetAddress | Street Address | String | Yes |
| city | City | String | Yes |
| state | State | String | Yes |
| postalCode | Postal Code | String | Yes |
| country | Country | String | Yes |
| companyName | Company Name | String | Yes |
| employeeId | Employee ID | String | Yes |
| userType | User Type | String | No |
| accountEnabled | Account Enabled | Boolean | Yes |

## Usage

1. **Login** to Azure AD B2C tenant
2. **Search** for a user
3. Navigate between tabs:
   - **Extension Attributes Tab:** View/edit custom B2C attributes
   - **Standard Attributes Tab:** View/edit standard user properties
4. Make changes to editable fields
5. Click **Save Changes** on the respective tab
6. Use **Refresh** to reload data from the server

## Notes

- Read-only fields are displayed in gray italic text and cannot be edited
- Each tab maintains its own modified state independently
- Changes must be saved per tab (extension attributes and standard attributes are saved separately)
- The `businessPhones` field accepts comma-separated phone numbers
- The `accountEnabled` field accepts "true" or "false" values

## Future Enhancements

Possible additions:
- Add more standard attributes (e.g., onPremisesSyncEnabled, createdDateTime, etc.)
- Add data validation for specific fields (email format, phone format, etc.)
- Add filtering/searching within the attribute lists
- Export attributes to CSV/JSON
- Bulk attribute updates
