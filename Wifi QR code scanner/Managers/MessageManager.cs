using System;
using Windows.UI.Popups;

namespace Wifi_QR_code_scanner.Managers
{
    public class MessageManager
    {
        public static async void ShowMessageToUserAsync(string message)
        {
            var msgbox = new MessageDialog(message);

            // Show the message dialog
            await msgbox.ShowAsync();
        }
    }
}
