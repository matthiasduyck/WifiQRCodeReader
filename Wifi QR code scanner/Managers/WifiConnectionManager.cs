using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wifi_QR_code_scanner.Business;
using Wifi_QR_code_scanner.Display;
using Windows;
using Windows.Devices;
using Windows.Devices.WiFi;

namespace Wifi_QR_code_scanner.Managers
{
    public class WifiConnectionManager
    {
        WiFiAdapter wiFiAdapter;
        public WifiConnectionManager()
        {
            InitializeWiFiAdapter();
        }

        public async Task InitializeWiFiAdapter()
        {
            var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
            if (result.Count >= 1)
            {
                wiFiAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);
            }
        }

        public async Task ConnectToWifiNetwork(WifiAccessPointData wifiAccessPointData)
        {
            if (wiFiAdapter!=null)
            {
                WiFiAvailableNetwork qualifyingWifi = null;
                try
                {
                    qualifyingWifi = wiFiAdapter.NetworkReport.AvailableNetworks.FirstOrDefault(N => N.Ssid == wifiAccessPointData.ssid);
                }
                catch (Exception)
                {
                    MessageManager.ShowMessageToUserAsync("An error occurred trying to find networks matching your code.");
                }
                if (qualifyingWifi != null)
                {
                    WiFiConnectionResult connectResult  = null;
                    if (string.IsNullOrWhiteSpace(wifiAccessPointData.password) || wifiAccessPointData.wifiAccessPointSecurity.Equals(WifiAccessPointSecurity.nopass))
                    {
                        connectResult = await wiFiAdapter.ConnectAsync(qualifyingWifi, WiFiReconnectionKind.Automatic);
                    }
                    else
                    {
                        connectResult = await wiFiAdapter.ConnectAsync(qualifyingWifi, WiFiReconnectionKind.Automatic, new Windows.Security.Credentials.PasswordCredential() { Password = wifiAccessPointData.password });
                    }
                    if (connectResult != null && connectResult.ConnectionStatus!=WiFiConnectionStatus.Success)
                    {
                        switch (connectResult.ConnectionStatus)
                        {
                            case WiFiConnectionStatus.AccessRevoked:
                                MessageManager.ShowMessageToUserAsync("Connection failed because access to the network has been revoked.");
                                break;
                            case WiFiConnectionStatus.InvalidCredential:
                                MessageManager.ShowMessageToUserAsync("Connection failed because an invalid credential was presented.");
                                break;
                            case WiFiConnectionStatus.NetworkNotAvailable:
                                MessageManager.ShowMessageToUserAsync("Connection failed because the network is not available. Try to move closer.");
                                break;
                            case WiFiConnectionStatus.Timeout:
                                MessageManager.ShowMessageToUserAsync("Connection failed because the connection attempt timed out.");
                                break;
                            case WiFiConnectionStatus.UnspecifiedFailure:
                                MessageManager.ShowMessageToUserAsync("Connection failed for an unknown reason.");
                                break;
                            case WiFiConnectionStatus.UnsupportedAuthenticationProtocol:
                                MessageManager.ShowMessageToUserAsync("Connection failed because the authentication protocol is not supported.");
                                break;
                        }
                    }
                }
                else
                {
                    MessageManager.ShowMessageToUserAsync("No WiFi network found matching your code. Please move closer and try again or verify your code.");
                }
            }
            else
            {
                MessageManager.ShowMessageToUserAsync("Wifi adapter could not be found or initialised.");
            }
        }

        public List<NetworkDisplayItem> ScanForAvailableNetworks()
        {
            if (wiFiAdapter != null)
            {
                var result = wiFiAdapter.ScanAsync();
                var availableNetworks = wiFiAdapter.NetworkReport.AvailableNetworks;
                return availableNetworks.OrderByDescending(x=>x.NetworkRssiInDecibelMilliwatts).Select(x=>new NetworkDisplayItem { ssid=x.Ssid }).ToList();
            }
            return null;
        }
    }
}
