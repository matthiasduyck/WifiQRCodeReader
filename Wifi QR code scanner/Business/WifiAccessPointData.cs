namespace Wifi_QR_code_scanner.Business
{
    public class WifiAccessPointData
    {
        public string ssid { get; set; }
        public string password { get; set; }

        public WifiAccessPointSecurity wifiAccessPointSecurity {get;set;}
        
        public override string ToString()
        {
            return "Network Name (SSID): " + ssid + System.Environment.NewLine +  "Password: " + password + System.Environment.NewLine + "Authentication: " + this.wifiAccessPointSecurity;
        }
    }
    public enum WifiAccessPointSecurity
    {
        WEP,
        WPA,
        nopass
    }
}
