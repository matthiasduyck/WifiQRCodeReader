using System;

namespace Wifi_QR_code_scanner
{
    public class ApplicationSettings
    {
        public static string WiFiQRCodeScannerPROFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + WiFiQRCodeScannerPROFolderName;
        public const string WiFiQRCodeScannerPROFolderName = "Wifi QR Code Scanner PRO";
    }
}
