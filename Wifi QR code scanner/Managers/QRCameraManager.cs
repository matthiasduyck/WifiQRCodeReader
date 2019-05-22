using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.Media.Capture;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
using ZXing;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;

namespace Wifi_QR_code_scanner.Managers
{
    public class QRCameraManager
    {
        MediaCapture mediaCapture;
        bool isPreviewing;
        DisplayRequest displayRequest = new DisplayRequest();
        CaptureElement previewWindowElement;
        CoreDispatcher dispatcher;
        CancellationTokenSource qrAnalyzerCancellationTokenSource;
        QrCodeDecodedDelegate qrCodeDecodedDelegate;

        public bool ScanForQRcodes { get; set; }

        public QRCameraManager(CaptureElement previewWindowElement, CoreDispatcher dispatcher, QrCodeDecodedDelegate qrCodeDecodedDelegate)
        {
            this.previewWindowElement = previewWindowElement;
            this.dispatcher = dispatcher;
            this.qrCodeDecodedDelegate = qrCodeDecodedDelegate;
            var qrAnalyzerCancellationTokenSource = new CancellationTokenSource();
            this.qrAnalyzerCancellationTokenSource = qrAnalyzerCancellationTokenSource;
        }
        public async Task StartPreviewAsync()
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
                MessageManager.ShowMessageToUserAsync("The app was denied access to the camera");
                return;
            }

            try
            {
                this.ScanForQRcodes = true;
                previewWindowElement.Source = mediaCapture;
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
                
                while (!qrAnalyzerCancellationTokenSource.Token.IsCancellationRequested)
                {
                    //try capture qr code here
                    if (ScanForQRcodes)
                    {
                        await findQRinImageAsync(imgCaptureWidth, imgCaptureHeight, imgProp, bcReader);
                    }

                    var qrCaptureInterval = 500;
                    await Task.Delay(qrCaptureInterval, qrAnalyzerCancellationTokenSource.Token);
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
                MessageManager.ShowMessageToUserAsync("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        public async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.previewWindowElement.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }

        private async Task findQRinImageAsync(int imgCaptureWidth, int imgCaptureHeight, ImageEncodingProperties imgProp, BarcodeReader bcReader)
        {
            var stream = new InMemoryRandomAccessStream();
            await mediaCapture.CapturePhotoToStreamAsync(imgProp, stream);


            stream.Seek(0);
            var wbm = new WriteableBitmap(imgCaptureWidth, imgCaptureHeight);
            await wbm.SetSourceAsync(stream);
            var result = bcReader.Decode(wbm);


            if (result != null)
            {
                ScanForQRcodes = false;

                var torch = mediaCapture.VideoDeviceController.TorchControl;

                if (torch.Supported) torch.Enabled = false;

                qrCodeDecodedDelegate.Invoke(result.Text);

                //await mediaCapture.StopPreviewAsync();
            }
        }
    }
}
