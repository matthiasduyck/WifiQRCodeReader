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
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using System.Collections.ObjectModel;
using Wifi_QR_code_scanner.Business;
using Windows.Media.Capture.Frames;

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

        private IEnumerable<DeviceInformation> availableColorCameras;

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

        public async Task EnumerateCameras(ComboBox comboBox)
        {
            //var Videodevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var Videodevices = await GetFrameSourceGroupsAsync();
            foreach (var camera in Videodevices)
            {
                comboBox.Items.Add(new ComboboxItem(camera.Name,camera.Id));
            }
            
        }

        public async Task<IEnumerable<DeviceInformation>> GetFrameSourceGroupsAsync()
        {
            if (availableColorCameras == null)
            {
                try
                {
                    var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                    var groups = await MediaFrameSourceGroup.FindAllAsync();

                    // Filter out color video preview and video record type sources and remove duplicates video devices.
                    var _frameSourceGroups = groups.Where(g => g.SourceInfos.Any(s => s.SourceKind == MediaFrameSourceKind.Color &&
                                                                                (s.MediaStreamType == MediaStreamType.VideoPreview || s.MediaStreamType == MediaStreamType.VideoRecord))
                                                                                && g.SourceInfos.All(sourceInfo => videoDevices.Any(vd => vd.Id == sourceInfo.DeviceInformation.Id))).ToList();
                    availableColorCameras = _frameSourceGroups.SelectMany(x => x.SourceInfos.Select(y => y.DeviceInformation));
                }
                catch (Exception ex)
                {
                    MessageManager.ShowMessageToUserAsync("Tried to find all available color cameras but failed to do so.");
                }
            }

            return availableColorCameras;
        }

        public async Task StartPreviewAsync(ComboboxItem comboboxItem)
        {
            try
            {
                mediaCapture = new MediaCapture();

                
                var settings = new MediaCaptureInitializationSettings()
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };
                if (comboboxItem != null)
                {
                    settings.VideoDeviceId = comboboxItem.ID;
                }
                else
                {
                    if (availableColorCameras == null)
                    {
                        availableColorCameras = await GetFrameSourceGroupsAsync();
                    }
                    settings.VideoDeviceId = availableColorCameras.First().Id;
                }
                
                qrAnalyzerCancellationTokenSource = new CancellationTokenSource();
                try
                {
                    await mediaCapture.InitializeAsync(settings);
                }
                catch (Exception ex)
                {
                    MessageManager.ShowMessageToUserAsync("Tried to initialize a color camera but failed to do so.");
                }
                List<VideoEncodingProperties> availableResolutions = null;
                try { 
                    availableResolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Where(properties=>properties is VideoEncodingProperties).Select(properties=>(VideoEncodingProperties)properties).ToList();
                }
                catch(Exception ex)
                {
                    MessageManager.ShowMessageToUserAsync("No resolutions could be detected, trying default mode.");
                }
                //var printout = availableResolutions.Where(x => x is VideoEncodingProperties).Select(y =>" H:" + ((VideoEncodingProperties)y).Height + " W:" + ((VideoEncodingProperties)y).Width + " fpsnum:" + ((VideoEncodingProperties)y).FrameRate.Numerator + " fpsdenom:" + ((VideoEncodingProperties)y).FrameRate.Denominator + " bitrate:" + ((VideoEncodingProperties)y).Bitrate);
                VideoEncodingProperties bestVideoResolution = this.findBestResolution(availableResolutions);
                VideoEncodingProperties bestPhotoResolution = this.findBestResolution(availableResolutions);
                if (bestVideoResolution != null)
                {
                    await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, bestVideoResolution);
                }
                if (bestPhotoResolution != null)
                {
                    await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, bestPhotoResolution);
                }
                displayRequest.RequestActive();
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
            catch (System.ObjectDisposedException)
            {
                Debug.WriteLine("object was disposed");
            }
            catch (Exception)
            {
                Debug.WriteLine("another exception occurred.");
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
                    await StartPreviewAsync(null);
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
        private VideoEncodingProperties findBestResolution(List<VideoEncodingProperties> videoEncodingProperties)
        {
            if(videoEncodingProperties != null && videoEncodingProperties.Any())
            {
                //we want the highest bitrate, highest fps, with a resolution that is as square as possible, and not too small or too large
                var result = videoEncodingProperties.Where(a => (a.Width >= a.Height))//square or wider
                    .Where(b => b.Width >= 400 && b.Height >= 400)//not too small
                    .Where(c => c.Width <= 800 && c.Height <= 600)//not too large
                    .OrderBy(d => ((double)d.Width) / ((double)d.Height))//order by smallest aspect ratio(most 'square' possible)
                    .ThenBy(e => e.Width)//order by the smallest possible width
                    .ThenByDescending(f => f.Bitrate)//with the highest possible bitrate
                    .First();
                return result;
            }
            return null;
        }
    }
}
