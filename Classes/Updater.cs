﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace youtube_dl_gui {
    class Updater {
        public static string rawURL = "https://raw.githubusercontent.com/murrty/youtube-dl-gui";
        public static string githubURL = "https://github.com/murrty/youtube-dl-gui";
        public static string githubJSON = "http://api.github.com/repos/murrty/youtube-dl-gui/releases/latest";
        public static string downloadURL = "https://github.com/murrty/youtube-dl-gui/releases/download/%upVersion%/youtube-dl-gui.exe";
        public static string updateFile = @"\ydgu.bat";

        public static string getJSON(string url) {
            if (!Properties.Settings.Default.jsonSupport)
                return null;

            try {
                using (WebClient wc = new WebClient()) {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    wc.Headers.Add("User-Agent: " + Program.UserAgent);
                    string json = wc.DownloadString(url);
                    byte[] bytes = Encoding.ASCII.GetBytes(json);
                    using (var stream = new MemoryStream(bytes)) {
                        var quotas = new XmlDictionaryReaderQuotas();
                        var jsonReader = JsonReaderWriterFactory.CreateJsonReader(stream, quotas);
                        var xml = XDocument.Load(jsonReader);
                        stream.Flush();
                        stream.Close();
                        return xml.ToString();
                    }
                }
            }
            catch (WebException WebE) {
                Debug.Print(WebE.ToString());
                ErrorLog.reportWebError(WebE, url);
                return null;
                throw WebE;
            }
            catch (Exception ex) {
                Debug.Print(ex.ToString());
                ErrorLog.reportError(ex);
                return null;
                throw ex;
            }
        }

        public static decimal getCloudVersion() {
            try {
                string xml = getJSON(githubJSON);
                if (xml == null)
                    return -1;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlNodeList xmlTag = doc.DocumentElement.SelectNodes("/root/tag_name");

                return decimal.Parse(xmlTag[0].InnerText.Replace(".", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator), NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) {
                Debug.Print(ex.ToString());
                ErrorLog.reportError(ex);
                return -1;
            }
        }
        public static bool isUpdateAvailable(decimal cloudVersion) {
            try {
                if (Properties.Settings.Default.appVersion < cloudVersion) { return true; }
                else { return false; }
            }
            catch (Exception ex) {
                Debug.Print(ex.ToString());
                ErrorLog.reportError(ex);
                return false;
            }
        }

        public static bool downloadNewVersion(decimal cloudVersion) {
            if (!Properties.Settings.Default.jsonSupport)
                return false;

            try {
                using (WebClient wc = new WebClient()) {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    wc.Headers.Add("User-Agent: " + Program.UserAgent);
                    wc.DownloadFile("https://github.com/murrty/youtube-dl-gui/releases/download/" + (cloudVersion) + "/youtube-dl-gui.exe", Environment.CurrentDirectory + "\\youtube-dl-gui.exe.part");
                    return true;
                }
            }
            catch (WebException webe) {
                ErrorLog.reportWebError(webe, "https://github.com/murrty/youtube-dl-gui/releases/download/this-is-the-detected-updated-version/youtube-dl-gui.exe");
                return false;
            }
            catch (Exception ex) {
                ErrorLog.reportError(ex);
                return false;
            }
        }
        public static void runMerge() {
            Process runUpdater = new Process();
            runUpdater.StartInfo.FileName = Environment.CurrentDirectory + "\\youtube-dl-gui-updater.exe";
            runUpdater.StartInfo.Arguments = System.AppDomain.CurrentDomain.FriendlyName;
            runUpdater.Start();
            Environment.Exit(0);
        }

        public static bool updateStub() {
            try {
                if (File.Exists(Environment.CurrentDirectory + "\\youtube-dl-gui-updater.exe")) {
                    FileVersionInfo stubVersion = FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + "\\youtube-dl-gui-updater.exe");
                    if (stubVersion.ProductMajorPart > 1) {
                        File.Delete(Environment.CurrentDirectory + "\\youtube-dl-gui-updater.exe");
                    }
                    else {
                        return true;
                    }
                }

                File.WriteAllBytes(Environment.CurrentDirectory + "\\youtube-dl-gui-updater.exe", Properties.Resources.youtube_dl_gui_updater);

                return true;
            }
            catch (Exception ex) {
                ErrorLog.reportError(ex);
                return false;
            }
        }
    }
}