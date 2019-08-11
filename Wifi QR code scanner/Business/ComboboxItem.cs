using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wifi_QR_code_scanner.Business
{
    public class ComboboxItem
    {
        public string Name;
        public string ID;
        public ComboboxItem(string name, string id)
        {
            Name = name; ID = id;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
