﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Wifi_QR_code_scanner.Business;

namespace FullTrust
{
    class Program
    {
        //todo: get from settings?
        private static string ApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Wifi QR Code Scanner PRO";

        static void Main(string[] args)
        {
            //This should not be needed as the UWP side should have done this already.
            Directory.CreateDirectory(ApplicationDataFolder);
            var allWifiData = getWifiData();
            var serializedWifiData = Newtonsoft.Json.JsonConvert.SerializeObject(allWifiData);


            File.WriteAllText(ApplicationDataFolder + "\\wifidata.json", serializedWifiData);
            //Console.Title = "Hello World";
            //Console.WriteLine("This process has access to the entire public desktop API surface");
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