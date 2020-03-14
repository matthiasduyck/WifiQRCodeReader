using Windows.Graphics.Imaging;
using ZXing;

namespace Wifi_QR_code_scanner.Managers
{
    public class BarcodeManager
    {
        BarcodeReader bcReader;
        public BarcodeManager()
        {
            bcReader = new BarcodeReader();
        }
        public string DecodeBarcodeImage(SoftwareBitmap image) {
            var result = bcReader.Decode(image);
            if (result != null)
            {
                return result.Text;
            }
            else
            {
                return "";
            }
        }
    }
}
