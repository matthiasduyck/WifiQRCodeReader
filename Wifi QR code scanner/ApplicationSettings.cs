using System;

namespace Wifi_QR_code_scanner
{
    public class ApplicationSettings
    {
        public static string WiFiQRCodeScannerPROFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + WiFiQRCodeScannerPROFolderName;
        public const string WiFiQRCodeScannerPROFolderName = "Wifi QR Code Scanner PRO";

        public const string LocalSettingsApplicationDataFolderSettingName = "ApplicationDataFolder";
        public const string LocalSettingsImportFolderSettingName = "ImportFolder";
        public const string LocalSettingsFullTrustApplicationModeSettingName = "FullTrustApplicationMode";
    }

    public enum FullTrustApplicationMode
    {
        ExportProfilesToFilesystem,
        ImportProfiles
    }
}
