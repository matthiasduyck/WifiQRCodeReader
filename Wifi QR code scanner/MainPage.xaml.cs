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
//using Wifi_QR_Code_Sanner_Library.Managers;
//using Wifi_QR_Code_Sanner_Library.Domain;

namespace Wifi_QR_code_scanner
{
    /// <summary>
    /// Delegate to be triggered on QR capture
    /// </summary>
    /// <param name="qrmessage"></param>
    public delegate void QrCodeDecodedDelegate(string qrmessage);

    public sealed partial class MainPage : Page
    {
        QRCameraManager cameraManager;
        WifiConnectionManager wifiConnectionManager;

        private int activeTab = 0;

        public MainPage()
        {
            this.InitializeComponent();
            QrCodeDecodedDelegate handler = new QrCodeDecodedDelegate(handleQRcodeFound);
            cameraManager = new QRCameraManager(PreviewControl, Dispatcher, handler);
            wifiConnectionManager = new WifiConnectionManager();
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Current_Resuming;
            Application.Current.LeavingBackground += Current_LeavingBackground;
            cameraManager.EnumerateCameras(cmbCameraSelect);
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

        /// <summary>
        /// Method to be triggered by delegate to display message to start connecting to a network
        /// </summary>
        /// <param name="qrmessage"></param>
        public async void handleQRcodeFound(string qrmessage)
        {
            ChangeAppStatus(AppStatus.waitingForUserInput);
            var wifiAPdata = WifiStringParser.parseWifiString(qrmessage);
            MessageDialog msgbox;
            if (wifiAPdata == null)
            {
                msgbox = new MessageDialog("QR code does not contain WiFi connection data.");
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
            Debug.WriteLine("Application Suspending");
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await cameraManager.CleanupCameraAsync();
                deferral.Complete();
            }
        }

        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activeTab = this.TabsView.SelectedIndex;
            if (activeTab == 0)
            {
                this.cameraManager.ScanForQRcodes = true;
                ChangeAppStatus(AppStatus.scanningForQR);
            }
            else
            {
                this.cameraManager.ScanForQRcodes = false;
                ChangeAppStatus(AppStatus.waitingForUserInput);
            }
        }

        private void BtnGenerateQR_Click(object sender, RoutedEventArgs e)
        {
            //grab values
            var ssid = this.txtSSID.Text;
            var password = this.txtPass.Text;
            var security = ((ComboBoxItem)cmbSecurity.SelectedItem).Content.ToString();
            //verify they are filled in
            if (string.IsNullOrEmpty(ssid))
            {
                MessageManager.ShowMessageToUserAsync("Wifi Network Name empty.");
                return;
            }

            var wifiData = new WifiAccessPointData();
            wifiData.password = password;
            wifiData.ssid = ssid;
            if (security == "WEP")
            {
                wifiData.wifiAccessPointSecurity = WifiAccessPointSecurity.WEP;
            }
            else if (security == "WPA" || security== "WPA2 (default)")
            {
                wifiData.wifiAccessPointSecurity = WifiAccessPointSecurity.WPA;
            }
            else if (security=="None")
            {
                wifiData.wifiAccessPointSecurity = WifiAccessPointSecurity.nopass;
            }

            //verify they are valid
            if (!wifiData.isvalid())
            {
                MessageManager.ShowMessageToUserAsync("Wifi data not valid, please verify all fields.");
                return;
            }

            var wifiQrString = WifiStringParser.createWifiString(wifiData);

            //create image
            var options = new QrCodeEncodingOptions
            {
                DisableECI = true,
                CharacterSet = "UTF-8",
                Width = 512,
                Height = 512,
            };
            var qr = new ZXing.BarcodeWriter();
            qr.Options = options;
            qr.Format = ZXing.BarcodeFormat.QR_CODE;
            var result = qr.Write(wifiQrString);
            //set as source
            this.imgQrCode.Source = result;
        }

        private async void BtnSaveFile_Click(object sender, RoutedEventArgs e)
        {
            var _bitmap = new RenderTargetBitmap();
            //verify they are filled in
            if (this.imgQrCode.Source ==null)
            {
                MessageManager.ShowMessageToUserAsync("No image to save, please generate one first.");
                return;
            }
            await _bitmap.RenderAsync(this.imgQrCode);    //-----> This is my ImageControl.

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("Image", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = "QRCodeImage" + DateTime.Now.ToString("yyyyMMddhhmmss"); //todo add ssid name
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
    }
}
