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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Wifi_QR_code_scanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    
    public sealed partial class MainPage : Page
    {
        MediaCapture mediaCapture;
        bool isPreviewing;
        DisplayRequest displayRequest = new DisplayRequest();
        public MainPage()
        {
            this.InitializeComponent();
            //Windows.Devices.WiFi.;
            Application.Current.Suspending += Application_Suspending;
            StartPreviewAsync();
        }
        private async Task StartPreviewAsync()
        {
            try
            {

                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();
                

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                //ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
                int imgCaptureWidth = 800;
                int imgCaptureHeight = 800;
                var imgProp = new ImageEncodingProperties
                {
                    Subtype = "BMP",
                    Width = (uint)imgCaptureWidth,
                    Height = (uint)imgCaptureHeight
                };
                var bcReader = new BarcodeReader();

                var exit = 0;
                while (exit == 0)
                {
                    var stream = new InMemoryRandomAccessStream();
                    await mediaCapture.CapturePhotoToStreamAsync(imgProp, stream);


                    stream.Seek(0);
                    var wbm = new WriteableBitmap(imgCaptureWidth, imgCaptureHeight);
                    await wbm.SetSourceAsync(stream);
                    var result = bcReader.Decode(wbm);


                    if (result != null)
                    {
                        var torch = mediaCapture.VideoDeviceController.TorchControl;
                        
                        if (torch.Supported) torch.Enabled = false;
                        //await mediaCapture.StopPreviewAsync();
                        var msgbox = new MessageDialog(result.Text);
                        await msgbox.ShowAsync();
 
                    }

                }
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
        }
        private async void mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                //ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }
    }
}
