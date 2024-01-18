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
using QR_Library.Managers;

namespace WiFi_QR_Code_Scanner_PRO.Managers
{
    public delegate void StoredCredentialsUpdateDelegate(List<WifiAccessPointData> accessPointData, bool resultIsFiltered);

    public class StoredCredentialsManager
    {
        private WifiNotesManager WifiNotesManager;
        private StorageFolder ApplicationDataFolder;

        private IReadOnlyList<StorageFile> Files;

        private List<WifiAccessPointData> lastResult;

        private const string ApplicationFolderName = ApplicationSettings.WiFiQRCodeScannerPROFolderName;

        private StorageFileQueryResult profileFileQuery;

        StoredCredentialsUpdateDelegate StoredCredentialsUpdateDelegate { get; set; }

        public StoredCredentialsManager(StoredCredentialsUpdateDelegate storedCredentialsUpdateDelegate, WifiNotesManager wifiNotesManager)
        {
            StoredCredentialsUpdateDelegate = storedCredentialsUpdateDelegate;
            WifiNotesManager = wifiNotesManager;
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
            else
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            
            
        }

        public void filterStoredCredentials(string filterKeyWord)
        {
            var filteredResult = lastResult;
            if (filterKeyWord.Length > 0)
            {
                filteredResult = lastResult.Where(r => r.ssid.Contains(filterKeyWord,StringComparison.InvariantCultureIgnoreCase) || r.password.Contains(filterKeyWord, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }
            
            this.StoredCredentialsUpdateDelegate(filteredResult, true);
        }

        private async void SetupApplicationDataFolderAndSubscribeChanges()
        {
            //ApplicationDataFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync(ApplicationFolderName, CreationCollisionOption.OpenIfExists);
            //ApplicationDataFolder = await KnownFolders.DocumentsLibrary.GetFolderAsync(ApplicationFolderName);
            ApplicationDataFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            //var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            //folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            //folderPicker.FileTypeFilter.Add("*");
            //folderPicker.CommitButtonText = "Create and select new folder for profiles";

            //ApplicationDataFolder = await folderPicker.PickSingleFolderAsync();
            if (ApplicationDataFolder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("ApplicationDataFolder", ApplicationDataFolder);//todo use const for field name
                ApplicationData.Current.LocalSettings.Values[ApplicationSettings.LocalSettingsApplicationDataFolderSettingName] = ApplicationDataFolder.Path;
                ApplicationData.Current.LocalSettings.Values[ApplicationSettings.LocalSettingsFullTrustApplicationModeSettingName] = FullTrustApplicationMode.ExportProfilesToFilesystem.ToString();
                //this.textBlock.Text = "Picked folder: " + folder.Name;
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

                List<string> fileTypeFilter = new List<string>();
                fileTypeFilter.Add(".xml");
                var fileQueryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
                profileFileQuery = ApplicationDataFolder.CreateFileQueryWithOptions(fileQueryOptions);
                //subscribe on query's ContentsChanged event
                profileFileQuery.ContentsChanged += Query_ContentsChanged;
                //trigger once needed for init
                Files = await profileFileQuery.GetFilesAsync();
            }
            else
            {
                //this.textBlock.Text = "Operation cancelled.";
                //todo
            }

            
        }

        public async void ExportAllProfiles()
        {
            if (Files != null && Files.Any())
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*");
                folderPicker.CommitButtonText = "Choose export location";

                var exportFolder = await folderPicker.PickSingleFolderAsync();
                if (exportFolder != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("ExportFolder", exportFolder);//todo use const for field name

                    foreach(var file in Files)
                    {
                        await file.CopyAsync(exportFolder, file.Name, NameCollisionOption.GenerateUniqueName);
                    }
                }
            }
        }
        public async void ImportProfiles()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add(".xml");
            folderPicker.CommitButtonText = "Choose Import XML Folder Location";
            var importFolder = await folderPicker.PickSingleFolderAsync();
            if (importFolder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("ImportFolder", importFolder);//todo use const for field name


                ApplicationData.Current.LocalSettings.Values[ApplicationSettings.LocalSettingsApplicationDataFolderSettingName] = ApplicationDataFolder.Path;
                ApplicationData.Current.LocalSettings.Values[ApplicationSettings.LocalSettingsImportFolderSettingName] = importFolder.Path;
                ApplicationData.Current.LocalSettings.Values[ApplicationSettings.LocalSettingsFullTrustApplicationModeSettingName] = FullTrustApplicationMode.ImportProfiles.ToString();
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
        }

        private void Query_ContentsChanged(Windows.Storage.Search.IStorageQueryResultBase sender, object args)
        {
            LoadProfileDataFromFiles();
        }

        private async void LoadProfileDataFromFiles()
        {
            Files = await profileFileQuery.GetFilesAsync();
            XmlSerializer serializer = new XmlSerializer(typeof(WLANProfile));
            var result = new List<WifiAccessPointData>();
            foreach (var file in Files)
            {
                var wifiProfileData = await Windows.Storage.FileIO.ReadTextAsync(file);

                using (StringReader reader = new StringReader(wifiProfileData))
                {
                    try
                    {
                        var wlanProfile = (WLANProfile)serializer.Deserialize(reader);
                        result.Add(await MapWLANProfileToWifiAccessPointData(wlanProfile));
                    }
                    catch(Exception e)
                    {

                    }
                    
                    
                }
            }
            lastResult = result;
            this.StoredCredentialsUpdateDelegate(result, false);
        }

        private async Task<WifiAccessPointData> MapWLANProfileToWifiAccessPointData(WLANProfile wlanProfile)
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

            result.Note = await WifiNotesManager.GetWifiNote(result.GetUniqueId());

            return result;
        }
    }
}
