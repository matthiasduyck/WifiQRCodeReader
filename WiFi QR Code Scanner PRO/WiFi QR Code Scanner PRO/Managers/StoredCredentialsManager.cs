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

namespace WiFi_QR_Code_Scanner_PRO.Managers
{
    public delegate void StoredCredentialsUpdateDelegate(List<WifiAccessPointData> accessPointData);

    public class StoredCredentialsManager
    {
        
        private StorageFolder ApplicationDataFolder;
        //todo: get from settings?
        private const string ApplicationFolderName = "Wifi QR Code Scanner PRO";

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
            fileTypeFilter.Add(".json");
            var fileQueryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            var fileQuery = ApplicationDataFolder.CreateFileQueryWithOptions(fileQueryOptions);
            //subscribe on query's ContentsChanged event
            fileQuery.ContentsChanged += Query_ContentsChanged;
            //trigger once needed for init
            var files = await fileQuery.GetFilesAsync();
        }

        private void Query_ContentsChanged(Windows.Storage.Search.IStorageQueryResultBase sender, object args)
        {
            LoadProfileDataFromFile();
        }

        private async void LoadProfileDataFromFile()
        {
            var applicationFolder = await KnownFolders.DocumentsLibrary.GetFolderAsync(ApplicationFolderName);
            //todo get filename from shared const
            var wifiDataFile = await applicationFolder.GetFileAsync("wifidata.json");
            var serializedWifiData = await Windows.Storage.FileIO.ReadTextAsync(wifiDataFile);
            var allWifiData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WifiAccessPointData>>(serializedWifiData);
            this.StoredCredentialsUpdateDelegate(allWifiData);
        }
    }
}
