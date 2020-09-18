using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wifi_QR_code_scanner.Display
{
    public class NetworkDisplayItem
    {
        public string Ssid { get; set; }

        public NetworkDisplayItem(string ssid)
        {
            if (!string.IsNullOrEmpty(ssid))
            {
                this.Ssid = ssid;
            }
            else
            {
                this.Ssid = "<Hidden network>";
            }
        }
    }
}
