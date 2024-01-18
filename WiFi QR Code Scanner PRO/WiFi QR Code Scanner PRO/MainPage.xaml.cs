using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Wifi_QR_code_scanner.Managers;
using Wifi_QR_code_scanner.Business;
using System.Diagnostics;
using ZXing.QrCode;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Core;
using Wifi_QR_code_scanner.Display;
using System.Threading;
//using Wifi_QR_Code_Sanner_Library.Managers;
//using Wifi_QR_Code_Sanner_Library.Domain;
using Wifi_QR_code_scanner;
using System.Collections.ObjectModel;
using WiFi_QR_Code_Scanner_PRO.Managers;
using Windows.Foundation.Metadata;
using System.IO;
using Windows.Storage.Search;
using static WiFi_QR_Code_Scanner_PRO.Managers.StoredCredentialsManager;
using System.Linq;
using QR_Code_Scanner.Business;
using QR_Library.Managers;
using QR_Library.Business;
using System.Threading.Tasks;

namespace WiFi_QR_Code_Scanner_PRO
{

    public partial class MainPage : Page
    {
        QRCameraManager cameraManager;
        WifiConnectionManager wifiConnectionManager;
        StoredCredentialsManager storedCredentialsManager;
        BarcodeManager barcodeManager;
        ISettingsManager SettingsManager;
        WifiNotesManager WifiNotesManager;
        System.Threading.Timer scanningTimer;
        CancellationTokenSource qrAnalyzerCancellationTokenSource;

        //private bool lastResultFromScanner;

        private bool HasBeenDeactivated { get; set; }

        private string lastQrSSid { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            //catchall crash handler
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CrashHandler);

            QrCodeDecodedDelegate handler = new QrCodeDecodedDelegate(handleQRcodeFound);
            qrAnalyzerCancellationTokenSource = new CancellationTokenSource();

            SettingsManager = new WifiSettingsManager();
            cameraManager = new QRCameraManager(PreviewControl, Dispatcher, handler, qrAnalyzerCancellationTokenSource, SettingsManager);
            WifiNotesManager = new WifiNotesManager();
            StoredCredentialsUpdateDelegate storedCredentialsUpdateDelegate = new StoredCredentialsUpdateDelegate(StoredCredentialsUpdateAsync);
            storedCredentialsManager = new StoredCredentialsManager(storedCredentialsUpdateDelegate, WifiNotesManager);


            wifiConnectionManager = new WifiConnectionManager();

            barcodeManager = new BarcodeManager();
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Current_Resuming;
            Application.Current.LeavingBackground += Current_LeavingBackground;
            cameraManager.EnumerateCameras(cmbCameraSelect);
            StartScanningForNetworks();
            grdSettings.Visibility = Visibility.Collapsed;
        }

        private async Task<QrCodeEncodingOptions> GetQREncodingOptionsAsync() {
            var imageWidthHeightSetting = (await SettingsManager.GetSettingsAsync()).QRImageResolution;
            var imageWidthHeight = imageWidthHeightSetting != null ? imageWidthHeightSetting.SettingItem : 512;
            //create image
            return new QrCodeEncodingOptions
            {
                DisableECI = true,
                CharacterSet = "UTF-8",
                Width = imageWidthHeight,
                Height = imageWidthHeight,
            };
        }
        static void CrashHandler(object sender, System.UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("CrashHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        public void ChangeAppStatus(AppStatus appStatus)
        {
            switch (appStatus)
            {
                case AppStatus.connectingToNetwork:
                    this.Status.Text = "Connecting to network.";
                    break;
                case AppStatus.scanningForQR:
                    this.Status.Text = "Looking for QR code.";
                    break;
                case AppStatus.waitingForUserInput:
                    this.Status.Text = "Waiting for user input.";
                    break;
            }
        }

        private void Current_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            Debug.WriteLine("leaving bg");
            cameraManager.StartPreviewAsync(null);
        }

        private void Current_Resuming(object sender, object e)
        {
            Debug.WriteLine("resuming");
        }

        private void StartScanningForNetworks()
        {
            //setup panel to display first
            var brush = (SolidColorBrush)this.Resources["ApplicationPageBackgroundThemeBrush"];
            var color = brush.Color;// = Color.FromArgb(255, 242, 101, 34);
            this.lstNetworks.Background = brush;
            scanningTimer = new System.Threading.Timer(
            e => ScanForNetworksAndDisplay(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(3));
        }

        private async void ScanForNetworksAndDisplay()
        {
            var availaibleNetworks = wifiConnectionManager.ScanForAvailableNetworks();
            if (availaibleNetworks != null)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                    {
                        var availableNetworksDisplay = new List<NetworkDisplayItem>();

                        foreach (var availableNetwork in availaibleNetworks)
                        {
                            availableNetworksDisplay.Add(availableNetwork);
                        }

                        this.lstNetworks.ItemsSource = availableNetworksDisplay;
                    }
                );
            }
        }


        /// <summary>
        /// Method to be triggered by delegate to display message to start connecting to a network
        /// </summary>
        /// <param name="qrmessage"></param>
        public async void handleQRcodeFound(string qrmessage, bool fromScanner)
        {
            //lastResultFromScanner = fromScanner;
            ChangeAppStatus(AppStatus.waitingForUserInput);
            this.cameraManager.ScanForQRcodes = false;
            var wifiAPdata = WifiStringParser.parseWifiString(qrmessage);
            MessageDialog msgbox;
            if (wifiAPdata == null)
            {
                msgbox = new MessageDialog("This QR code is not recognized as a WiFi QR code. This QR code contains the following information:"
                    + Environment.NewLine + Environment.NewLine
                    + qrmessage
                    + Environment.NewLine + Environment.NewLine
                    + "You can use my free 'QR Code Scanner' app or 'QR Code Scanner PRO' for general purpose QR codes.");
            }
            else
            {
                // load it into clipboard if settings say so:
                var CopyResultToClipboardInstantlyWhenFoundSetting = (await SettingsManager.GetSettingsAsync()).CopyResultToClipboardInstantlyWhenFound;
                var copyResultToClipboardInstantlyWhenFound = CopyResultToClipboardInstantlyWhenFoundSetting != null ? CopyResultToClipboardInstantlyWhenFoundSetting.SettingItem : false;
                if (copyResultToClipboardInstantlyWhenFound)
                {
                    CopyPasswordToClipboard(qrmessage);
                }
                msgbox = new MessageDialog(wifiAPdata.ToObfuscatedString());
                // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                msgbox.Commands.Add(new UICommand(
                    "Connect",
                    new UICommandInvokedHandler(this.ConnectHandlerAsync), qrmessage));
                msgbox.Commands.Add(new UICommand(
                    "Copy Password",
                    new UICommandInvokedHandler(this.CopyPasswordToClipboardHandler), qrmessage));
            }

            msgbox.Commands.Add(new UICommand(
                "Close",
                new UICommandInvokedHandler(this.CancelHandler)));

            // Set the command that will be invoked by default
            msgbox.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            msgbox.CancelCommandIndex = 1;

            // Show the message dialog
            await msgbox.ShowAsync();
        }

        private async void ShowPasswordHandlerAsync(IUICommand command)
        {
            var wifiPassword = command.Id as string;
            MessageDialog msgbox = new MessageDialog(wifiPassword);
            await msgbox.ShowAsync();
        }

        private void CopyPasswordToClipboardHandler(IUICommand command)
        {
            CopyPasswordToClipboard(command.Id as string);
            //enable scanning again
            this.cameraManager.ScanForQRcodes = true;
            ChangeAppStatus(AppStatus.scanningForQR);
        }

        private void CopyPasswordToClipboard(string wifiString){
            ChangeAppStatus(AppStatus.waitingForUserInput);
            var wifiAPdata = WifiStringParser.parseWifiString(wifiString);
            var dataPackage = new DataPackage();
            dataPackage.SetText(wifiAPdata.password);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        private async void ConnectHandlerAsync(IUICommand command)
        {
            ChangeAppStatus(AppStatus.connectingToNetwork);
            var wifistringToConnectTo = command.Id as string;
            var wifiAPdata = WifiStringParser.parseWifiString(wifistringToConnectTo);
            await this.wifiConnectionManager.ConnectToWifiNetwork(wifiAPdata);
            //enable scanning again
            this.cameraManager.ScanForQRcodes = true;
            ChangeAppStatus(AppStatus.scanningForQR);
        }

        private void CancelHandler(IUICommand command)
        {
            //enable scanning again
            this.cameraManager.ScanForQRcodes = true;
            ChangeAppStatus(AppStatus.scanningForQR);
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedFrom");
            await cameraManager.CleanupCameraAsync();
        }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            try
            {
                //Debug.WriteLine("Application Suspending");
                var deferral = e.SuspendingOperation.GetDeferral();

                if (this.scanningTimer != null) { this.scanningTimer.Dispose(); }
                this.cameraManager.ScanForQRcodes = false;
                this.qrAnalyzerCancellationTokenSource.Cancel();

                await cameraManager.CleanupCameraAsync();

                this.barcodeManager = null;
                this.cameraManager = null;
                this.wifiConnectionManager = null;
                deferral.Complete();
            }
            catch (Exception ex)
            {
            }
        }

        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var activeTabName = ((PivotItem)(sender as Pivot).SelectedItem).Name;
            //activeTab = this.TabsView.SelectedIndex;
            if (!string.IsNullOrEmpty(activeTabName) && activeTabName=="scan")
            {
                ActivateCameraPreviewAndScan();
                this.cameraManager.ScanForQRcodes = true;
                ChangeAppStatus(AppStatus.scanningForQR);

                
            }
            else
            {
                DeActivateCameraPreviewAndScan();
                this.cameraManager.ScanForQRcodes = false;
                ChangeAppStatus(AppStatus.waitingForUserInput);

                
            }
        }
        private async void ActivateCameraPreviewAndScan()
        {
            var selected = cmbCameraSelect.SelectedItem;
            if (cameraManager != null && cameraManager.ScanForQRcodes == false && this.HasBeenDeactivated)
            {
                this.HasBeenDeactivated = false;
                if (selected != null && selected is QR_Code_Scanner.Business.ComboboxItem)
                {
                    var selectedCamera = ((QR_Code_Scanner.Business.ComboboxItem)cmbCameraSelect.SelectedItem);
                    //start cam again
                    await cameraManager.StartPreviewAsync(selectedCamera);
                }
                else
                {
                    //start cam again
                    await cameraManager.StartPreviewAsync(null);
                }
            }
        }
        private async void DeActivateCameraPreviewAndScan()
        {
            if (cameraManager != null && cameraManager.ScanForQRcodes == true && !this.HasBeenDeactivated)
            {
                this.HasBeenDeactivated = true;
                //stop cam
                cameraManager.ScanForQRcodes = false;
                try
                {
                    await cameraManager.CleanupCameraAsync();
                }
                catch (Exception)
                {
                    //todo, investigate why this fails sometimes
                }
            }
        }

        private async void BtnGenerateQR_Click(object sender, RoutedEventArgs e)
        {
            //grab values
            var ssid = this.txtSSID.Text;
            var password = this.txtPass.Text;
            var security = ((ComboBoxItem)cmbSecurity.SelectedItem).Content.ToString();
            var hidden = chckHidden.IsChecked ?? false;
            //verify they are filled in
            if (string.IsNullOrEmpty(ssid))
            {
                MessageManager.ShowMessageToUserAsync("Wifi Network Name empty.");
                return;
            }

            var wifiData = new WifiAccessPointData();
            wifiData.password = password;
            wifiData.hidden = hidden;
            wifiData.ssid = ssid;
            if (security == "WEP")
            {
                wifiData.wifiAccessPointSecurity = WifiAccessPointSecurity.WEP;
            }
            else if (security == "WPA" || security == "WPA2 (default)")
            {
                wifiData.wifiAccessPointSecurity = WifiAccessPointSecurity.WPA;
            }
            else if (security == "None")
            {
                wifiData.wifiAccessPointSecurity = WifiAccessPointSecurity.nopass;
            }

            //verify they are valid
            string validationReason;
            if (!wifiData.isvalid(out validationReason))
            {
                MessageManager.ShowMessageToUserAsync("Wifi data not valid: " + validationReason);
                return;
            }

            var wifiQrString = WifiStringParser.createWifiString(wifiData);

            //create image
            var options = await GetQREncodingOptionsAsync();
            var qr = new ZXing.BarcodeWriter();
            qr.Options = options;
            qr.Format = ZXing.BarcodeFormat.QR_CODE;
            var result = qr.Write(wifiQrString);
            //set as source
            this.imgQrCode.Source = result;
            this.lastQrSSid = wifiData.ssid;
            //make save button visible
            this.btnSaveFile.Visibility = Visibility.Visible;
        }

        private void BtnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveQRFile(this.imgQrCode);
        }

        private async void SaveQRFile(Image imageControl)
        {
            // Verify if the image source is filled in
            if (this.imgQrCode.Source == null || !(this.imgQrCode.Source is WriteableBitmap))
            {
                MessageManager.ShowMessageToUserAsync("No image to save, please generate one first.");
                return;
            }

            WriteableBitmap writeableBitmap = this.imgQrCode.Source as WriteableBitmap;

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("Image", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = "QRCodeImage_" + this.lastQrSSid + "_" + DateTime.Now.ToString("yyyyMMddhhmmss");
            StorageFile savefile = await savePicker.PickSaveFileAsync();
            if (savefile == null)
                return;

            using (IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Get pixel data directly from the WriteableBitmap
                Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        (uint)writeableBitmap.PixelWidth,
                                        (uint)writeableBitmap.PixelHeight,
                                        96, // this is just dpi
                                        96, // this is just dpi
                                        pixels);

                await encoder.FlushAsync();
            }
        }

        private async void CmbCameraSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var selected = cmbCameraSelect.SelectedItem;
            if (cameraManager != null && cameraManager.ScanForQRcodes == true && selected is ComboboxItem)
            {

                //stop cam
                cameraManager.ScanForQRcodes = false;
                try
                {
                    await cameraManager.CleanupCameraAsync();
                }
                catch (Exception)
                {
                    //todo, investigate why this fails sometimes
                }
                var selectedCamera = ((ComboboxItem)cmbCameraSelect.SelectedItem);
                //start cam again
                await cameraManager.StartPreviewAsync(selectedCamera);
            }
        }

        private void BtnGenerateRandomPassword_Click(object sender, RoutedEventArgs e)
        {
            var security = ((ComboBoxItem)cmbSecurity.SelectedItem).Content.ToString();
            switch (security.ToLower())
            {
                case "wep":
                    MessageManager.ShowMessageToUserAsync("WEP is not a secure standard!");
                    this.txtPass.Text = "";
                    break;
                case "none":
                    MessageManager.ShowMessageToUserAsync("No security standard selected! Select one of the WPA options to get a random password.");
                    this.txtPass.Text = "";
                    break;
                default:
                    string allowedCharsWPA = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+=~[]{}|\\:;<>,.?/";//only include simple chars for typability when needed, full list: "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_+=~`[]{}|\\:;\"'<>,.?/"
                    this.txtPass.Text = PasswordGenerator.GenerateRandomPassword(60, allowedCharsWPA);
                    break;
            }
        }

        private void LstNetworks_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.lstNetworks.Opacity = 0.9;
        }

        private void LstNetworks_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.lstNetworks.Opacity = 0.4;
        }

        private async void BtnOpenQRImage_ClickAsync(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder;
                    try
                    {
                        // Create the decoder from the stream
                        decoder = await BitmapDecoder.CreateAsync(stream);
                        // Get the SoftwareBitmap representation of the file
                        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                        var QRcodeResult = barcodeManager.DecodeBarcodeImage(softwareBitmap);
                        handleQRcodeFound(QRcodeResult,false);
                    }
                    catch (Exception ex)
                    {
                        MessageDialog msgbox = new MessageDialog("An error occurred: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageDialog msgbox = new MessageDialog("No file selected.");

                // Set the command that will be invoked by default
                msgbox.DefaultCommandIndex = 0;

                // Set the command to be invoked when escape is pressed
                msgbox.CancelCommandIndex = 1;

                // Show the message dialog
                await msgbox.ShowAsync();
            }
        }

        #region PRO section        

        private void BtnRefreshStoredCredentials_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                proLoadStoredProfiles.IsActive = true;
                proLoadStoredProfiles.Visibility = Visibility.Visible;
                storedCredentialsManager.UpdateStoredCredentials();
            }
            else
            {
                MessageManager.ShowMessageToUserAsync("An error occurred: The application did not get Full Trust App Capabilities.");
            }
        }


        public async void StoredCredentialsUpdateAsync(List<WifiAccessPointData> accessPointData, bool resultIsFiltered)
        {
            var accessPointViewData = accessPointData.Select(x => new WifiAccessPointDataViewModelWrapper(x));
            ObservableCollection<WifiAccessPointDataViewModelWrapper> observableCollectionWifiData = new ObservableCollection<WifiAccessPointDataViewModelWrapper>(accessPointViewData);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                ContactsLV.ItemsSource = observableCollectionWifiData;
                proLoadStoredProfiles.IsActive = false;
                proLoadStoredProfiles.Visibility = Visibility.Collapsed;
                if (accessPointViewData.Any())
                {
                    btnExportAllProfiles.Visibility = Visibility.Visible;
                    btnImportProfiles.Visibility = Visibility.Visible;
                    txtStoredWifiFilter.Visibility = Visibility.Visible;
                }
                else
                {
                    if (!resultIsFiltered)
                    {
                        txtStoredWifiFilter.Visibility = Visibility.Collapsed;
                    }
                }
            }
            );
        }

        
        

        private async void BtnShowStoredWifiData_Click(object sender, RoutedEventArgs e)
        {
            var networkToGenerateQRCodeFor = ((Windows.UI.Xaml.FrameworkElement)sender).DataContext as WifiAccessPointDataViewModelWrapper;
            var wifiQrString = WifiStringParser.createWifiString(networkToGenerateQRCodeFor.AccessPointData);

            //create image
            var options = await GetQREncodingOptionsAsync();
            var qr = new ZXing.BarcodeWriter();
            qr.Options = options;
            qr.Format = ZXing.BarcodeFormat.QR_CODE;
            var result = qr.Write(wifiQrString);
            //set as source
            this.imgQrCodeFromStoredNetwork.Source = result;
            this.qrCodeFromStoredNetwork.Visibility = Visibility.Visible;

            //do the text stuff
            this.txtAuthenticationTypeFromStoredNetwork.Text = networkToGenerateQRCodeFor.AccessPointData.wifiAccessPointSecurity.ToString();
            this.txtPasswordFromStoredNetwork.Text = !string.IsNullOrEmpty(networkToGenerateQRCodeFor.AccessPointData.password) ? networkToGenerateQRCodeFor.AccessPointData.password : "No password";
            this.txtSSIDFromStoredNetwork.Text = networkToGenerateQRCodeFor.AccessPointData.ssid;
            this.txtStoredNetworkNote.Text = networkToGenerateQRCodeFor.AccessPointData.Note ?? "None";
        }

        private void BtnCloseQrCodeFromStoredNetwork_Click(object sender, RoutedEventArgs e)
        {
            this.qrCodeFromStoredNetwork.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveQrCodeImageFromStoredNetwork_Click(object sender, RoutedEventArgs e)
        {
            SaveQRFile(this.imgQrCodeFromStoredNetwork);
        }

        private void BtnCopyNetworkNameFromStoredNetwork_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(this.txtSSIDFromStoredNetwork.Text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        private void BtnCopyPasswordFromStoredNetwork_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(this.txtPasswordFromStoredNetwork.Text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        private void BtnExportAllProfiles_Click(object sender, RoutedEventArgs e)
        {
            storedCredentialsManager.ExportAllProfiles();
        }

        private void BtnImportProfiles_Click(object sender, RoutedEventArgs e)
        {
            storedCredentialsManager.ImportProfiles();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Windows.System.Launcher.LaunchUriAsync(new Uri("https://matthiasduyck.wordpress.com/wifi-qr-code-scanner/help-faq/"));
        }

        private async void btnTglSettings_Click(object sender, RoutedEventArgs e)
        {
            if (this.btnTglSettings.IsChecked ?? false)
            {
                this.grdSettings.Visibility = Visibility.Visible;
                await loadSettingsAsync();
            }
            else
            {
                this.grdSettings.Visibility = Visibility.Collapsed;
            }
        }

        private async void lnkSettingsClear_Click(object sender, RoutedEventArgs e)
        {
            await SettingsManager.DeleteSettings();
            await loadSettingsAsync();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            closeSettings();
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            saveSettings();
            closeSettings();
        }
        private void closeSettings()
        {
            this.grdSettings.Visibility = Visibility.Collapsed;
            this.btnTglSettings.IsChecked = false;
        }
        private async Task loadSettingsAsync()
        {
            QRSettings settings = new QRSettings();
            try
            {
                settings = (QRSettings)await SettingsManager.RetrieveOrCreateSettings();
            }
            catch (Exception ex)
            {
                // Todo, should we provide more information to the user here?
                await ShowSettingsLoadingFailedMessageBoxAsync();
            }

            nmbQRCodeImageResolution.Value = settings.QRImageResolution != null ? settings.QRImageResolution.SettingItem : nmbQRCodeImageResolution.Value;
            chckCopyToClipboardImmediately.IsChecked = settings.CopyResultToClipboardInstantlyWhenFound != null ? settings.CopyResultToClipboardInstantlyWhenFound.SettingItem : chckCopyToClipboardImmediately.IsChecked;
            var settingsRefreshRate = settings.ScanningRefreshRate;
            if (settingsRefreshRate != null)
            {
                switch (settingsRefreshRate.SettingItem)
                {
                    case 50:
                        cmbRefreshRate.SelectedIndex = 0;
                        break;
                    case 100:
                        cmbRefreshRate.SelectedIndex = 1;
                        break;
                    case 150:
                        cmbRefreshRate.SelectedIndex = 2;
                        break;
                    case 200:
                        cmbRefreshRate.SelectedIndex = 3;
                        break;
                    default:
                        cmbRefreshRate.SelectedIndex = 2;
                        break;
                }
            }

            var settingsScanningResolution = settings.ScanningResolution;
            if (settingsScanningResolution != null)
            {
                switch (settingsScanningResolution.SettingItem)
                {
                    case ScanningResolutionEnum.lowest:
                        cmbScanResolution.SelectedIndex = 0;
                        break;
                    case ScanningResolutionEnum.auto:
                        cmbScanResolution.SelectedIndex = 1;
                        break;
                    case ScanningResolutionEnum.highest:
                        cmbScanResolution.SelectedIndex = 2;
                        break;
                    default:
                        cmbScanResolution.SelectedIndex = 1;
                        break;
                }
            }
        }

        private async Task ShowSettingsLoadingFailedMessageBoxAsync()
        {
            var msgbox = new MessageDialog("Loading settings file failed. You can try deleting it.");
            msgbox.Commands.Add(new UICommand(
            "Delete",
            new UICommandInvokedHandler(this.DeleteSettingsHandler)));

            // Set the command that will be invoked by default
            msgbox.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            msgbox.CancelCommandIndex = 1;

            // Show the message dialog
            await msgbox.ShowAsync();
        }
        private void DeleteSettingsHandler(IUICommand command)
        {
            _ = SettingsManager.DeleteSettings();
            _ = loadSettingsAsync();
        }

        private void saveSettings()
        {
            var qRSettings = new WifiQRSettings();
            qRSettings.QRImageResolution.SettingItem = (int)nmbQRCodeImageResolution.Value;
            qRSettings.CopyResultToClipboardInstantlyWhenFound.SettingItem = (bool)chckCopyToClipboardImmediately.IsChecked;
            var cmbRefreshRateValue = ((ComboBoxItem)cmbRefreshRate.SelectedItem).Content.ToString();
            switch (cmbRefreshRateValue)
            {
                case "50":
                    qRSettings.ScanningRefreshRate = new NullableQRSettingItem<int>(50);
                    break;
                case "100":
                    qRSettings.ScanningRefreshRate = new NullableQRSettingItem<int>(100);
                    break;
                case "150":
                    qRSettings.ScanningRefreshRate = new NullableQRSettingItem<int>(150);
                    break;
                case "200":
                    qRSettings.ScanningRefreshRate = new NullableQRSettingItem<int>(200);
                    break;
                default:
                    qRSettings.ScanningRefreshRate = new NullableQRSettingItem<int>(150);
                    break;
            }
            var cmbScanResolutionValue = ((ComboBoxItem)cmbScanResolution.SelectedItem).Content.ToString();
            switch (cmbScanResolutionValue)
            {
                case "lowest":
                    qRSettings.ScanningResolution = new NullableQRSettingItem<ScanningResolutionEnum>(ScanningResolutionEnum.lowest);
                    break;
                case "highest":
                    qRSettings.ScanningResolution = new NullableQRSettingItem<ScanningResolutionEnum>(ScanningResolutionEnum.highest);
                    break;
                case "auto":
                    qRSettings.ScanningResolution = new NullableQRSettingItem<ScanningResolutionEnum>(ScanningResolutionEnum.auto);
                    break;
                default:
                    qRSettings.ScanningResolution = new NullableQRSettingItem<ScanningResolutionEnum>(ScanningResolutionEnum.auto);
                    break;
            }

            SettingsManager.ReplaceExistingSetting(qRSettings);
        }

        private void txtStoredWifiFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchString = txtStoredWifiFilter.Text;
            storedCredentialsManager.filterStoredCredentials(txtStoredWifiFilter.Text);
        }

        private async void btnEditNoteFromStoredNetwork_Click(object sender, RoutedEventArgs e)
        {
            var networkToGenerateQRCodeFor = ((Windows.UI.Xaml.FrameworkElement)sender).DataContext as WifiAccessPointDataViewModelWrapper;
            txtNoteInputBox.Text = networkToGenerateQRCodeFor.AccessPointData.Note ?? "";
            txtNoteHiddenUniqueId.Text = networkToGenerateQRCodeFor.AccessPointData.GetUniqueId();
            txtNoteChangeDescription.Text = $"Change the note related to the '{networkToGenerateQRCodeFor.DisplaySsid}' network.";
            await this.networkNoteContentDialog.ShowAsync();
        }

        private void networkNoteContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            WifiNotesManager.SaveWifiNote(txtNoteInputBox.Text, txtNoteHiddenUniqueId.Text);
            BtnRefreshStoredCredentials_Click(null,null);
        }
    }
    // This wrapper is needed because the base class cannot be linked in the main page
    public class WifiAccessPointDataViewModelWrapper : WifiAccessPointDataViewModel
    {
        public WifiAccessPointDataViewModelWrapper(WifiAccessPointData wifiAccessPointData) : base(wifiAccessPointData)
        {
        }
    }
    #endregion
}
