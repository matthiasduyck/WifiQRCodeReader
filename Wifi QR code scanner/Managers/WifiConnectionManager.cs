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
                WiFiAvailableNetwork qualifyingWifi = null;
                try
                {
                    qualifyingWifi = firstWifiAdapter.NetworkReport.AvailableNetworks.FirstOrDefault(N => N.Ssid == wifiAccessPointData.ssid);
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
                        connectResult = await firstWifiAdapter.ConnectAsync(qualifyingWifi, WiFiReconnectionKind.Automatic);
                    }
                    else
                    {
                        connectResult = await firstWifiAdapter.ConnectAsync(qualifyingWifi, WiFiReconnectionKind.Automatic, new Windows.Security.Credentials.PasswordCredential() { Password = wifiAccessPointData.password });
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
                                MessageManager.ShowMessageToUserAsync("Connection failed because the network is not available.");
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
        }
    }
}
