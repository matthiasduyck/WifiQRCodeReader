using System;

namespace Wifi_QR_code_scanner.Business
{
    public class WifiAccessPointData
    {
        public string ssid { get; set; }
        public string password { get; set; }
        public bool hidden { get; set; }

        public WifiAccessPointSecurity wifiAccessPointSecurity {get;set;}
        
        public override string ToString()
        {
            return "Network Name (SSID): " + ssid + System.Environment.NewLine +  "Password: " + password + System.Environment.NewLine + "Authentication type: " + this.wifiAccessPointSecurity;
        }

        public string ToObfuscatedString()
        {
            return "Network Name (SSID): " + ssid + System.Environment.NewLine + "Password: " + "******" + System.Environment.NewLine + "Authentication type: " + this.wifiAccessPointSecurity;
        }

        public bool isvalid(out string validationreason)
        {
            validationreason = "none";
            if (string.IsNullOrEmpty(ssid))
            {
                validationreason = "Network name is empty.";
                return false;
            }
            else if (ssid.Length < 1)
            {
                validationreason = "Network name is too short.";
                return false;
            }
            else if (wifiAccessPointSecurity == WifiAccessPointSecurity.WPA && ssid.Length < 8)
            {
                validationreason = "Network name is too short. Must be 8 characters or more for WPA type networks.";
                return false;
            }
            else if (wifiAccessPointSecurity == WifiAccessPointSecurity.WPA && ssid.Length > 63)
            {
                validationreason = "Network name is too long. Must be 63 characters or less for WPA type networks.";
                return false;
            }
            else if (ssid.Length > 32)
            {
                validationreason = "Network name is too long. Must be 32 characters or less.";
                return false;
            }
            else if (string.IsNullOrEmpty(password) && wifiAccessPointSecurity!=WifiAccessPointSecurity.nopass)
            {
                validationreason = "Password is empty.";
                return false;
            }
            return true;
        }
    }
    public enum WifiAccessPointSecurity
    {
        WEP,
        WPA,
        nopass
    }
}
