using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Wifi_QR_code_scanner.Business;
using System.Security.Cryptography;
using Wifi_QR_code_scanner;
using System.Windows.Forms;
using Windows.Storage;

namespace FullTrust
{
    class Program
    {
        private static string ApplicationDataFolder = ApplicationSettings.WiFiQRCodeScannerPROFolder;

        [STAThread]
        static void Main(string[] args)
        {
            //This should not be needed as the UWP side should have done this already.
            //Directory.CreateDirectory(ApplicationDataFolder);
            ExportWifiProfilesAsXML();

            //File.WriteAllText(ApplicationDataFolder + "\\wifidata.json", serializedWifiData);
        }

        //private static void Alternative()
        //{
        //    var blo = ManagedNativeWifi.NativeWifi.EnumerateProfiles().ToList();
        //}

        private static void ExportWifiProfilesAsXML()
        {
            //using (var fbd = new FolderBrowserDialog())
            //{
            //    DialogResult result = fbd.ShowDialog();

            //    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            //    {
            //        fbd.SelectedPath = fbd.SelectedPath;
            //    }
            //}

            //netsh wlan export profile key=clear folder="%UserProfile%\Desktop"
            //execute the netsh command using process class
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            var applicationDataFolder = ApplicationData.Current.LocalSettings.Values[@"ApplicationDataFolder"] as string;
            processWifi.StartInfo.Arguments = "wlan export profile key=clear folder=\""+ applicationDataFolder + "\"";

            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();

            string output = processWifi.StandardOutput.ReadToEnd();

            processWifi.WaitForExit();
        }

        private static string decodePassword(string hexEncodedPassword)
        {
            try
            {
                // Convert to a byte array
                byte[] passwordByteArray = StringToByteArray(hexEncodedPassword);

                // Decrypt byte array
                byte[] unprotectedBytes = ProtectedData.Unprotect(passwordByteArray, null, DataProtectionScope.LocalMachine);

                return Encoding.ASCII.GetString(unprotectedBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return "";
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private static string GetWifiNetworks()
        {
            //execute the netsh command using process class
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            processWifi.StartInfo.Arguments = "wlan show profile";

            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();

            string output = processWifi.StandardOutput.ReadToEnd();

            processWifi.WaitForExit();
            return output;
        }

        private static string ReadProfileData(string WifiProfileName)
        {

            string argument = "wlan show profile name=\"" + WifiProfileName + "\" key=clear";
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            processWifi.StartInfo.Arguments = argument;


            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();

            string output = processWifi.StandardOutput.ReadToEnd();

            processWifi.WaitForExit();
            return output;
        }

        private static KeyValuePair<string, string> GetWifiPassword(string Wifi_Name)
        {
            string wifiProfileData = ReadProfileData(Wifi_Name);
            string wifiPassword = null;
            string authenticationString = null;
            using (StringReader reader = new StringReader(wifiProfileData))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex keyContentDataRegex = new Regex(@"Key Content * : (?<after>.*)");
                    Regex authenticationModeRegex = new Regex(@"Authentication * : (?<after>.*)");

                    Match keyContentDataRegexMatch = keyContentDataRegex.Match(line);
                    Match authenticationModeRegexMatch = authenticationModeRegex.Match(line);

                    if (keyContentDataRegexMatch.Success)
                    {
                        wifiPassword = keyContentDataRegexMatch.Groups["after"].Value;
                    }
                    else if (authenticationModeRegexMatch.Success)
                    {
                        authenticationString = authenticationModeRegexMatch.Groups["after"].Value;
                    }
                }
            }
            return new KeyValuePair<string, string>(authenticationString, wifiPassword);
        }

        // main Method 
        private static List<WifiAccessPointData> getWifiData()
        {
            var result = new List<WifiAccessPointData>();
            string WifiNetworks = GetWifiNetworks();
            using (StringReader reader = new StringReader(WifiNetworks))
            {

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    //wifiCount++;
                    Regex regex1 = new Regex(@"All User Profile * : (?<after>.*)");
                    Match match1 = regex1.Match(line);

                    if (match1.Success)
                    {
                        //Wifi_count_names++;
                        string Wifi_name = match1.Groups["after"].Value;
                        var wifiAuthenticationData = GetWifiPassword(Wifi_name);
                        string Wifi_password = wifiAuthenticationData.Value;
                        WifiAccessPointSecurity wifiAccessPointSecurity = wifiAuthenticationData.Key.Contains("WPA") ? WifiAccessPointSecurity.WPA : WifiAccessPointSecurity.WEP;
                        if (string.IsNullOrEmpty(Wifi_password))
                        {
                            wifiAccessPointSecurity = WifiAccessPointSecurity.nopass;
                        }
                        result.Add(new WifiAccessPointData() { ssid = Wifi_name, password=Wifi_password, wifiAccessPointSecurity = wifiAccessPointSecurity });
                    }
                }
            }
            return result;
        }
    }
}
