using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using Wifi_QR_code_scanner.Business;
//using NativeWifi;
using Windows.Networking.Connectivity;
using System.Collections.Immutable;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.Storage.Search;
using System.Collections.ObjectModel;
using WiFi_QR_Code_Scanner_PRO.Business;
using System.Xml.Serialization;
using Wifi_QR_code_scanner;

namespace WiFi_QR_Code_Scanner_PRO.Managers
{
    public delegate void StoredCredentialsUpdateDelegate(List<WifiAccessPointData> accessPointData);

    public class StoredCredentialsManager
    {
        
        private StorageFolder ApplicationDataFolder;

        private const string ApplicationFolderName = ApplicationSettings.WiFiQRCodeScannerPROFolderName;

        private StorageFileQueryResult profileFileQuery;

        StoredCredentialsUpdateDelegate StoredCredentialsUpdateDelegate { get; set; }

        public StoredCredentialsManager(StoredCredentialsUpdateDelegate storedCredentialsUpdateDelegate)
        {
            StoredCredentialsUpdateDelegate = storedCredentialsUpdateDelegate;
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// Executes a shell command synchronously.
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="command">string command</param></span>
        /// <span class="code-SummaryComment"><returns>string, as output of the command.</returns></span>
        public async void UpdateStoredCredentials()
        {
            if (ApplicationDataFolder == null)
            {
                SetupApplicationDataFolderAndSubscribeChanges();
            }
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private async void SetupApplicationDataFolderAndSubscribeChanges()
        {
            ApplicationDataFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync(ApplicationFolderName, CreationCollisionOption.OpenIfExists);
            ApplicationDataFolder = await KnownFolders.DocumentsLibrary.GetFolderAsync(ApplicationFolderName);
            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".xml");
            var fileQueryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            profileFileQuery = ApplicationDataFolder.CreateFileQueryWithOptions(fileQueryOptions);
            //subscribe on query's ContentsChanged event
            profileFileQuery.ContentsChanged += Query_ContentsChanged;
            //trigger once needed for init
            var files = await profileFileQuery.GetFilesAsync();
        }

        private void Query_ContentsChanged(Windows.Storage.Search.IStorageQueryResultBase sender, object args)
        {
            LoadProfileDataFromFiles();
        }

        private async void LoadProfileDataFromFiles()
        {
            var files = await profileFileQuery.GetFilesAsync();
            XmlSerializer serializer = new XmlSerializer(typeof(WLANProfile));
            var result = new List<WifiAccessPointData>();
            foreach (var file in files)
            {
                var wifiProfileData = await Windows.Storage.FileIO.ReadTextAsync(file);

                using (StringReader reader = new StringReader(wifiProfileData))
                {
                    try
                    {
                        var wlanProfile = (WLANProfile)serializer.Deserialize(reader);
                        result.Add(MapWLANProfileToWifiAccessPointData(wlanProfile));
                    }
                    catch(Exception e)
                    {

                    }
                    
                    
                }
            }

            this.StoredCredentialsUpdateDelegate(result);
        }

        private WifiAccessPointData MapWLANProfileToWifiAccessPointData(WLANProfile wlanProfile)
        {
            var result = new WifiAccessPointData()
            {
                password = wlanProfile.MSM.Security.SharedKey !=null && !wlanProfile.MSM.Security.AuthEncryption.Authentication.Contains("open") ? wlanProfile.MSM.Security.SharedKey.KeyMaterial : "",
                ssid = wlanProfile.SSIDConfig.SSID.Name
            };

            if (wlanProfile.MSM.Security.AuthEncryption.Authentication.Contains("open"))
            {
                result.wifiAccessPointSecurity = WifiAccessPointSecurity.nopass;
            }
            else if (wlanProfile.MSM.Security.AuthEncryption.Authentication.Contains("WPA"))
            {
                result.wifiAccessPointSecurity = WifiAccessPointSecurity.WPA;
            }
            else
            {
                result.wifiAccessPointSecurity = WifiAccessPointSecurity.WEP;
            }

            return result;
        }
    }
}
