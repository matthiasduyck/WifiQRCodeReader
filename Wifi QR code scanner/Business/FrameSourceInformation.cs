using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture.Frames;

namespace Wifi_QR_code_scanner.Business
{
    public class FrameSourceInformation
    {
        public MediaFrameSourceGroup MediaFrameSourceGroup { get; set; }
        public MediaFrameSourceInfo MediaFrameSourceInfo { get; set; }

        public FrameSourceInformation(MediaFrameSourceGroup mediaFrameSourceGroup, MediaFrameSourceInfo mediaFrameSourceInfo)
        {
            this.MediaFrameSourceGroup = mediaFrameSourceGroup;
            this.MediaFrameSourceInfo = mediaFrameSourceInfo;
        }

        public FrameSourceInformation()
        {

        }
    }
}
