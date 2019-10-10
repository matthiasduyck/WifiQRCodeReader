namespace Wifi_QR_code_scanner.Business
{
    public class WifiAccessPointData
    {
        public string ssid { get; set; }
        public string password { get; set; }

        public WifiAccessPointSecurity wifiAccessPointSecurity {get;set;}
        
        public override string ToString()
        {
            return "Network Name (SSID): " + ssid + System.Environment.NewLine +  "Password: " + password + System.Environment.NewLine + "Authentication type: " + this.wifiAccessPointSecurity;
        }

        public string ToObfuscatedString()
        {
            return "Network Name (SSID): " + ssid + System.Environment.NewLine + "Password: " + "******" + System.Environment.NewLine + "Authentication type: " + this.wifiAccessPointSecurity;
        }

        public bool isvalid()
        {
            if (string.IsNullOrEmpty(ssid) || ssid.Length < 1 || ssid.Length > 32)
            {
                return false;
            }
            if (wifiAccessPointSecurity == WifiAccessPointSecurity.WPA && (ssid.Length<8 || ssid.Length>63))
            {
                return false;
            }
            if (string.IsNullOrEmpty(password) && wifiAccessPointSecurity!=WifiAccessPointSecurity.nopass)
            {
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
