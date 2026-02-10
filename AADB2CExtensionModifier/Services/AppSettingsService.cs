using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AADB2CExtensionModifier.Services
{
    /// <summary>
    /// Service for managing application settings persistence
    /// </summary>
    internal class AppSettingsService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AADB2CExtensionModifier");

        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

        public class AppSettings
        {
            [JsonPropertyName("tenantId")]
            public string TenantId { get; set; } = string.Empty;

            [JsonPropertyName("tenantDomain")]
            public string TenantDomain { get; set; } = string.Empty;
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    Debug.WriteLine("Settings file not found. Using default settings.");
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                
                Debug.WriteLine($"Settings loaded: TenantId={settings?.TenantId}, TenantDomain={settings?.TenantDomain}");
                
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                    Debug.WriteLine($"Created settings directory: {AppDataFolder}");
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
                
                Debug.WriteLine($"Settings saved: TenantId={settings.TenantId}, TenantDomain={settings.TenantDomain}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all saved settings
        /// </summary>
        public void ClearSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    File.Delete(SettingsFilePath);
                    Debug.WriteLine("Settings file deleted");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing settings: {ex.Message}");
            }
        }
    }
}
