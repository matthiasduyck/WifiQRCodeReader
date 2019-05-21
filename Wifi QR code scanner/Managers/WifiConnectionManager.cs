using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wifi_QR_code_scanner.Business;
using Windows;
using Windows.Devices;
using Windows.Devices.WiFi;

namespace Wifi_QR_code_scanner.Managers
{
    public class WifiConnectionManager
    {
        public async void ConnectToWifiNetwork(WifiAccessPointData wifiAccessPointData)
        {
            //Windows.Devices.WiFi.WiFiAdapter.FindAllAdaptersAsync().;
            var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
            WiFiAdapter firstWifiAdapter;
            if (result.Count >= 1)
            {
                firstWifiAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);
                var qualifyingWifi = firstWifiAdapter.NetworkReport.AvailableNetworks.FirstOrDefault(N => N.Ssid == wifiAccessPointData.ssid);

                List<WiFiAvailableNetwork> availableNetworks = new List<WiFiAvailableNetwork>();

                await firstWifiAdapter.ConnectAsync(qualifyingWifi, WiFiReconnectionKind.Automatic,new Windows.Security.Credentials.PasswordCredential() {Password= wifiAccessPointData.password });

            }


        }
    }
}
