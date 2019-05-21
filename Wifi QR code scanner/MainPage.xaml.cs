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
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
using Windows.UI.Popups;
using ZXing;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Wifi_QR_code_scanner.Managers;
using Wifi_QR_code_scanner.Business;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Wifi_QR_code_scanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
            //Windows.Devices.WiFi.;
            Application.Current.Suspending += Application_Suspending;
            cameraManager.StartPreviewAsync();
        }

        public async void handleQRcodeFound(string qrmessage)
        {
            var msgbox = new MessageDialog(qrmessage);
            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            msgbox.Commands.Add(new UICommand(
                "Connect",
                new UICommandInvokedHandler(this.ConnectHandler),qrmessage));
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
        }

        private void CancelHandler(IUICommand command)
        {
            //todo: do something
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
