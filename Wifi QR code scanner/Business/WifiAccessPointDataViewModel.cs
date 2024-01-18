namespace Wifi_QR_code_scanner.Business
{
    public class WifiAccessPointDataViewModel
    {

        public WifiAccessPointData AccessPointData { get; set; }
        public WifiAccessPointDataViewModel(WifiAccessPointData accessPointData)
        {
            this.AccessPointData = accessPointData;
            this.DisplayPassword = "***";
            this.DisplaySsid = accessPointData.ssid;
            this.DisplayNote = !string.IsNullOrEmpty(accessPointData.Note)? "Note: " + accessPointData.Note : null;
        }

        public string DisplayPassword { get; set; }
        public string DisplaySsid { get; set; }
        public string DisplayNote { get; set; }
    }
}
