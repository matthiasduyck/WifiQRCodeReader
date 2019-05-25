namespace Wifi_QR_code_scanner.Business
{
    public class WifiAccessPointData
    {
        public string ssid { get; set; }
        public string password { get; set; }
        public override string ToString()
        {
            return "Network Name (SSID): " + ssid + System.Environment.NewLine +  "Password: " + password;
        }
    }
}
