﻿using System;
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
using System.Collections.ObjectModel;
using System.Linq;
using QR_Code_Scanner.Business;
using QR_Library.Managers;
//using Wifi_QR_Code_Sanner_Library.Managers;
//using Wifi_QR_Code_Sanner_Library.Domain;

namespace Wifi_QR_code_scanner
{
    

    public partial class MainPage : Page
    {
        QRCameraManager cameraManager;
        WifiConnectionManager wifiConnectionManager;
        BarcodeManager barcodeManager;
        System.Threading.Timer scanningTimer;
        CancellationTokenSource qrAnalyzerCancellationTokenSource;

        private bool lastResultFromScanner;

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
            cameraManager = new QRCameraManager(PreviewControl, Dispatcher, handler, qrAnalyzerCancellationTokenSource);
            wifiConnectionManager = new WifiConnectionManager();
            barcodeManager = new BarcodeManager();
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Current_Resuming;
            Application.Current.LeavingBackground += Current_LeavingBackground;
            cameraManager.EnumerateCameras(cmbCameraSelect);
            StartScanningForNetworks();

            this.donateLnk.NavigateUri = new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MSU2BD59P7442&source=url");            
            this.donateLnkGenerate.NavigateUri = new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MSU2BD59P7442&source=url");            
            this.donateLnkOpen.NavigateUri = new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MSU2BD59P7442&source=url");
            PreviewStoredCredentials();
            if (NagwareManager.ShouldNag())
            {
                GrdNagware.Visibility = Visibility.Visible;
            }
        }

        private QrCodeEncodingOptions GetQREncodingOptions {
            get {
                return new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 512,
                    Height = 512,
                };
            }
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
        public async void handleQRcodeFound(string qrmessage,bool fromScanner)
        {
            lastResultFromScanner = fromScanner;
            ChangeAppStatus(AppStatus.waitingForUserInput);
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
                msgbox = new MessageDialog(wifiAPdata.ToObfuscatedString());
                // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                msgbox.Commands.Add(new UICommand(
                    "Connect",
                    new UICommandInvokedHandler(this.ConnectHandlerAsync), qrmessage));
                msgbox.Commands.Add(new UICommand(
                    "Copy Password",
                    new UICommandInvokedHandler(this.CopyPasswordToClipboardHandlerAsync), qrmessage));
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

        private async void CopyPasswordToClipboardHandlerAsync(IUICommand command)
        {
            ChangeAppStatus(AppStatus.waitingForUserInput);
            var wifistringToConnectTo = command.Id as string;
            var wifiAPdata = WifiStringParser.parseWifiString(wifistringToConnectTo);
            var dataPackage = new DataPackage();
            dataPackage.SetText(wifiAPdata.password);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            //enable scanning again
            this.cameraManager.ScanForQRcodes = true;
            ChangeAppStatus(AppStatus.scanningForQR);
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
            catch (Exception ex){
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

        private void BtnGenerateQR_Click(object sender, RoutedEventArgs e)
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
            var options = GetQREncodingOptions;
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
            var _bitmap = new RenderTargetBitmap();
            //verify they are filled in
            if (imageControl.Source == null)
            {
                MessageManager.ShowMessageToUserAsync("No image to save, please generate one first.");
                return;
            }
            await _bitmap.RenderAsync(imageControl);    //-----> This is my ImageControl.

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("Image", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = "QRCodeImage_" + this.lastQrSSid + "_" + DateTime.Now.ToString("yyyyMMddhhmmss");
            StorageFile savefile = await savePicker.PickSaveFileAsync();
            if (savefile == null)
                return;

            var pixels = await _bitmap.GetPixelsAsync();
            using (IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await
                BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                byte[] bytes = pixels.ToArray();
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        (uint)_bitmap.PixelWidth,
                                    (uint)_bitmap.PixelHeight,
                                        200,
                                        200,
                                        bytes);

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

        private void PreviewStoredCredentials()
        {
            var options = GetQREncodingOptions;
            var qr = new ZXing.BarcodeWriter();
            qr.Options = options;
            qr.Format = ZXing.BarcodeFormat.QR_CODE;
            var result = qr.Write("https://www.microsoft.com/en-us/p/wifi-qr-code-scanner-pro/9nkj4pt4llj6");
            this.imgQrCodeFromStoredNetwork.Source = result;
            this.qrCodeFromStoredNetwork.Visibility = Visibility.Visible;

            var fakeAccessPointData = new List<WifiAccessPointData> { };
            fakeAccessPointData.Add(new WifiAccessPointData() { ssid = "My ISP Box 45867" });
            fakeAccessPointData.Add(new WifiAccessPointData() { ssid = "Public City Wi-Fi network" });
            fakeAccessPointData.Add(new WifiAccessPointData() { ssid = "Hotel Seaside Shared" });
            var accessPointViewData = fakeAccessPointData.Select(x => new WifiAccessPointDataViewModelWrapper(x));
            ObservableCollection<WifiAccessPointDataViewModelWrapper> observableCollectionWifiData = new ObservableCollection<WifiAccessPointDataViewModelWrapper>(accessPointViewData);
            ContactsLV.ItemsSource = observableCollectionWifiData;
        }

        private void BtnCloseNagware_Click(object sender, RoutedEventArgs e)
        {
            GrdNagware.Visibility = Visibility.Collapsed;
        }
    }
    // This wrapper is needed because the base class cannot be linked in the main page
    public class WifiAccessPointDataViewModelWrapper : WifiAccessPointDataViewModel
    {
        public WifiAccessPointDataViewModelWrapper(WifiAccessPointData wifiAccessPointData) : base(wifiAccessPointData)
        {
        }
    }
}
