using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Wifi_QR_code_scanner.Managers;
using Wifi_QR_code_scanner.Business;

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
        
        
        public MainPage()
        {
            this.InitializeComponent();
            QrCodeDecodedDelegate handler = new QrCodeDecodedDelegate(handleQRcodeFound);
            cameraManager = new QRCameraManager(PreviewControl, Dispatcher, handler);
            wifiConnectionManager = new WifiConnectionManager();
            Application.Current.Suspending += Application_Suspending;
            cameraManager.StartPreviewAsync();
        }

        /// <summary>
        /// Method to be triggered by delegate to display message to start connecting to a network
        /// </summary>
        /// <param name="qrmessage"></param>
        public async void handleQRcodeFound(string qrmessage)
        {
            var wifiAPdata = WifiStringParser.parseWifiString(qrmessage);
            MessageDialog msgbox;
            if (wifiAPdata == null)
            {
                msgbox = new MessageDialog("QR code does not contain WiFi connection data.");
            }
            else
            {
                msgbox = new MessageDialog(wifiAPdata.ToString());
                // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                msgbox.Commands.Add(new UICommand(
                    "Connect",
                    new UICommandInvokedHandler(this.ConnectHandler), qrmessage));
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

        private void ConnectHandler(IUICommand command)
        {
            var wifistringToConnectTo = command.Id as string;
            var wifiAPdata = WifiStringParser.parseWifiString(wifistringToConnectTo);
            this.wifiConnectionManager.ConnectToWifiNetwork(wifiAPdata);
            //enable scanning again
            this.cameraManager.ScanForQRcodes = true;
        }

        private void CancelHandler(IUICommand command)
        {
            //enable scanning again
            this.cameraManager.ScanForQRcodes = true;
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await cameraManager.CleanupCameraAsync();
        }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await cameraManager.CleanupCameraAsync();
                deferral.Complete();
            }
        }
    }
}
