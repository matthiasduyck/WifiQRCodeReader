using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using Wifi_QR_code_scanner.Business;
//using NativeWifi;
using Windows.Networking.Connectivity;
using System.Collections.Immutable;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;

namespace WiFi_QR_Code_Scanner_PRO.Managers
{
    public static class StoredCredentialsManager
    {
        /// <span class="code-SummaryComment"><summary></span>
        /// Executes a shell command synchronously.
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="command">string command</param></span>
        /// <span class="code-SummaryComment"><returns>string, as output of the command.</returns></span>
        public async static void ExecuteCommandAsync()
        {
            try
            {
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }
                //// create the ProcessStartInfo using "cmd" as the program to be run,
                //// and "/c " as the parameters.
                //// Incidentally, /c tells cmd that we want it to execute the command that follows,
                //// and then exit.
                //System.Diagnostics.ProcessStartInfo procStartInfo =
                //    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                //// The following commands are needed to redirect the standard output.
                //// This means that it will be redirected to the Process.StandardOutput StreamReader.
                //procStartInfo.RedirectStandardOutput = true;
                //procStartInfo.UseShellExecute = false;
                //// Do not create the black window.
                //procStartInfo.CreateNoWindow = true;
                //// Now we create a process, assign its ProcessStartInfo and start it
                //System.Diagnostics.Process proc = new System.Diagnostics.Process();
                //proc.StartInfo = procStartInfo;
                //proc.Start();
                //// Get the output into a string
                //string result = proc.StandardOutput.ReadToEnd();
                //// Display the command output.
                ////return result;
            }
            catch (Exception objException)
            {
                // Log the exception
                //return "fail";
            }
        }
        private static string GetWifiNetworks()
        {
            //WlanClient client = new WlanClient();
            //foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            //{
            //    var bla = wlanIface.GetProfiles();
            //}

            //execute the netsh command using process class
            //Process processWifi = new Process();
            //processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //processWifi.StartInfo.FileName = "cmd.exe";
            //processWifi.StartInfo.Arguments = "netsh wlan show profile";

            //processWifi.StartInfo.UseShellExecute = false;
            //processWifi.StartInfo.RedirectStandardError = true;
            //processWifi.StartInfo.RedirectStandardInput = true;
            //processWifi.StartInfo.RedirectStandardOutput = true;
            //processWifi.StartInfo.CreateNoWindow = true;


            //System.Diagnostics.Process process = new System.Diagnostics.Process();
            //System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //startInfo.RedirectStandardError = true;
            //startInfo.RedirectStandardInput = true;
            //startInfo.RedirectStandardOutput = true;
            //startInfo.CreateNoWindow = false;
            //startInfo.FileName = "netsh";
            //startInfo.Arguments = "wlan show profiles";
            //process.StartInfo = startInfo;
            //process.Start();

            ProcessStartInfo commandInfo = new ProcessStartInfo();
            commandInfo.RedirectStandardInput = false;
            commandInfo.RedirectStandardOutput = true;
            commandInfo.FileName = "netsh";
            commandInfo.Arguments = "wlan show profiles";

            Process process = Process.Start(commandInfo);



            /*processWifi.Start()*/
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();

            
            return output;
        }

        private static string ReadPassword(string Wifi_Name)
        {

            string argument = "netsh wlan show profile name=\"" + Wifi_Name + "\" key=clear";
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "cmd.exe";
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

        private static string GetWifiPassword(string Wifi_Name)
        {
            string get_password = ReadPassword(Wifi_Name);
            using (StringReader reader = new StringReader(get_password))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex2 = new Regex(@"Key Content * : (?<after>.*)");
                    Match match2 = regex2.Match(line);

                    if (match2.Success)
                    {
                        string current_password = match2.Groups["after"].Value;
                        return current_password;
                    }
                }
            }
            return "Open Network - NO PASSWORD";
        }
        // main Method 
        public static List<WifiAccessPointData> get_Wifi_passwords()
        {

            List<WifiAccessPointData> result = new List<WifiAccessPointData>();
            string WifiNetworks = GetWifiNetworks();
            using (StringReader reader = new StringReader(WifiNetworks))
            {

                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    Regex regex1 = new Regex(@"All User Profile * : (?<after>.*)");
                    Match match1 = regex1.Match(line);

                    if (match1.Success)
                    {
                        string Wifi_name = match1.Groups["after"].Value;
                        string Wifi_password = GetWifiPassword(Wifi_name);
                        result.Add(new WifiAccessPointData { password=Wifi_password,ssid=Wifi_name });
                    }
                }
            }
            return result;


        }
    }
}
