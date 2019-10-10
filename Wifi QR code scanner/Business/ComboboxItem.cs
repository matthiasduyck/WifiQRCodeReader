using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture.Frames;

namespace Wifi_QR_code_scanner.Business
{
    public class ComboboxItem
    {
        public string Name;
        public string ID;
        public FrameSourceInformation MediaFrameSourceInformation;
        public ComboboxItem(string name, string id, FrameSourceInformation mediaFrameSourceInformation)
        {
            Name = name; ID = id; MediaFrameSourceInformation = mediaFrameSourceInformation;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
