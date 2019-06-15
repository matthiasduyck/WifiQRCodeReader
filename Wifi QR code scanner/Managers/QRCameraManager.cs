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
using System.Diagnostics;

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
        InMemoryRandomAccessStream inMemoryRandomAccessStream;
        WriteableBitmap writeableBitmap;
        Result bcResult;
        static int imgCaptureWidth = 800;
        static int imgCaptureHeight = 800;

        public bool ScanForQRcodes { get; set; }

        public QRCameraManager(CaptureElement previewWindowElement, CoreDispatcher dispatcher, QrCodeDecodedDelegate qrCodeDecodedDelegate)
        {
            this.previewWindowElement = previewWindowElement;
            this.dispatcher = dispatcher;
            this.qrCodeDecodedDelegate = qrCodeDecodedDelegate;
            var qrAnalyzerCancellationTokenSource = new CancellationTokenSource();
            this.qrAnalyzerCancellationTokenSource = qrAnalyzerCancellationTokenSource;
            this.inMemoryRandomAccessStream = new InMemoryRandomAccessStream();
            writeableBitmap = new WriteableBitmap(imgCaptureWidth, imgCaptureHeight);
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
                
                var imgProp = new ImageEncodingProperties
                {
                    Subtype = "BMP",
                    Width = (uint)imgCaptureWidth,
                    Height = (uint)imgCaptureHeight
                };
                var bcReader = new BarcodeReader();
                var qrCaptureInterval = 200;
                while (!qrAnalyzerCancellationTokenSource.IsCancellationRequested && qrAnalyzerCancellationTokenSource != null && qrAnalyzerCancellationTokenSource.Token!=null)
                {
                    //try capture qr code here
                    if (ScanForQRcodes)
                    {
                        await findQRinImageAsync(imgProp, bcReader);
                    }


                    await Task.Delay(qrCaptureInterval, qrAnalyzerCancellationTokenSource.Token);
                }
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
            catch(System.ObjectDisposedException)
            {
                Debug.WriteLine("object was disposed");
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
                qrAnalyzerCancellationTokenSource.Dispose();
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }
                isPreviewing = false;

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

        private async Task findQRinImageAsync(ImageEncodingProperties imgProp, BarcodeReader bcReader)
        {
            //When the camera is suspending, the stream can fail
            try
            {
                await mediaCapture.CapturePhotoToStreamAsync(imgProp, inMemoryRandomAccessStream);


                inMemoryRandomAccessStream.Seek(0);
                
                await writeableBitmap.SetSourceAsync(inMemoryRandomAccessStream);
                bcResult = bcReader.Decode(writeableBitmap);


                if (bcResult != null)
                {
                    ScanForQRcodes = false;

                    var torch = mediaCapture.VideoDeviceController.TorchControl;

                    if (torch.Supported) torch.Enabled = false;

                    qrCodeDecodedDelegate.Invoke(bcResult.Text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
