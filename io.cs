using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using MadMilkman.Ini;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Linq;

namespace Steam_Game_Launcher
{
    class io
    {
        public static List<string> steamLibrariesList = new List<string>();                     // To hold the steam libraries folders
        public static List<string> manifestsList = new List<string>();                          // To hold the manifests files            
        public static List<Game> gamesList = new List<Game>();                                  // To hold the games and IDs found in the manifests        
        public static List<string> iconsList = new List<string>();                                                      // Holds a list of all icons in the icons folder
        public static string regexNamePattern = GetSetting("Advanced", "regexNamePattern", configType.application);     // Regex patterns to parse manifests
        public static string regexIDPattern = GetSetting("Advanced", "regexIDPattern", configType.application);
        public static string regexInstallPattern = GetSetting("Advanced", "regexInstallPattern", configType.application);
        public static List<string> hiddenList = new List<string>();                             // Holds the list of hidden apps.

        public const string SETTINGS_FILE = "userconfig.ini";   // Config file for user settings
        public const string APPCONFIG_FILE = "appconfig.ini";   // Config file for application settings
        public const string LOG_FILE = "sgl.log";

        // Searches for appmanifests files in the selected folders.
        public static void GenerateManifestList()
        {
            foreach(string lib in steamLibrariesList)
            {
                LogToFile("Getting manifests in " + lib);
                if(Directory.Exists(lib))
                {
                    string[] man = Directory.GetFiles(lib, "appmanifest*.acf", SearchOption.TopDirectoryOnly);                   
                    manifestsList.AddRange(man);
                    foreach(string m in man)
                    {
                        LogToFile(m);
                    }
                }
                LogToFile("Found " + manifestsList.Count.ToString() + ".");
            }
        }

        // Parse the appmanifests to get installed games.
        public static void ParseGameNames()
        {
            foreach(string man in manifestsList)
            {
                if(!File.Exists(man))
                {
                    LogToFile("File not found in manifest list..." + man);
                    continue;                   
                }
                try
                {                    
                    string manifest = File.ReadAllText(man);
                    Regex regexName = new Regex(regexNamePattern);
                    Regex regexID = new Regex(regexIDPattern);
                    Regex regexInstallDir = new Regex(regexInstallPattern);
                    Match matchName = regexName.Match(manifest);
                    Match matchID = regexID.Match(manifest);
                    Match matchInstall = regexInstallDir.Match(manifest);
                    bool Visible = true;
                    if(matchName.Success && matchID.Success && matchInstall.Success)
                    {
                        // Check ID against the list of hidden games.
                        foreach (string id in hiddenList)
                        {
                            if (id == matchID.Value)
                            {
                                Visible = false;
                                break;
                            }
                        }
                        gamesList.Add(new Game(matchName.Value, matchID.Value, matchInstall.Value, Visible));
                    }
                    else
                    {
                        LogToFile("One of the regex patterns did not generate any results for manifest: " + man);
                    }                                                    
                }
                catch(Exception e)
                {
                    MessageBox.Show("There was an error parsing the manifests. " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogToFile(e.ToString());
                }
            }
        }

        // Enum to set the config to load
        public enum configType { application, user };

        // Get a setting from a config file.
        public static string GetSetting(string section, string setting, configType cfgtype = configType.user)
        {
            try
            {
                IniFile config = new IniFile();
                if (cfgtype == configType.user)
                {
                    config.Load(SETTINGS_FILE);
                }
                else if (cfgtype == configType.application)
                {
                    config.Load(APPCONFIG_FILE);
                }
                IniSection main = config.Sections[section];
                string value;
                main.Keys[setting].TryParseValue(out value);
                return value;
            }
            // Catch NullReferences for VerifyUserSettings() to handle
            catch (NullReferenceException) { throw new NullReferenceException(); }
            catch (Exception e)
            {
                MessageBox.Show("Could not read setting from config file. " + setting + " " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                io.LogToFile(e.ToString());
            }
            return null;
        }

        // Write a setting to a config file
        public static void WriteSetting(string section, string key, string value)
        {
            try
            {
                IniFile config = new IniFile();
                config.Load(SETTINGS_FILE);
                IniSection main = config.Sections[section];
                main.Keys[key].Value = value;
                config.Save(SETTINGS_FILE);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " ("+section+","+key+","+value+")", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                io.LogToFile(e.ToString());
            }
        }

        // Simple method to log to text file
        public static void LogToFile(string msg)
        {
            PurgeLog();
            msg = "[" + DateTime.Now + "] " + msg;
            try
            {
                File.AppendAllText(LOG_FILE, msg + Environment.NewLine);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Keep log file around 5000 lines (below 1Mb)
        private static void PurgeLog()
        {
            if(!File.Exists(LOG_FILE)) { return; }
            List<string> log = File.ReadAllLines(LOG_FILE).ToList<string>();
            if(log.Count > 5000)
            {
                log.RemoveRange(0, 2000);
                File.WriteAllLines(LOG_FILE, log.ToArray());
            }
        }

        // Set the app to run on startup or not
        public static void SetStartup(bool set)
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (set)
                    rk.SetValue(MainWindow.APPNAME, Application.ExecutablePath.ToString());
                else
                    rk.DeleteValue(MainWindow.APPNAME, false);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Writing Registry Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogToFile(e.ToString());
            }
        }

        // Verify user settings file and rebuilds it if necessary.
        public static void VerifyUserSettings()
        {
            // KeyValue list to hold the settings and their default value to append.
            List<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>();

            // Add settings to check here
            settings.Add(new KeyValuePair<string, string>("libraries", ""));
            settings.Add(new KeyValuePair<string, string>("icon_size", "128"));
            settings.Add(new KeyValuePair<string, string>("panel_margin", "100,10,100,10"));
            settings.Add(new KeyValuePair<string, string>("icon_spacing", "40,40,40,40"));
            settings.Add(new KeyValuePair<string, string>("label_padding", "10"));
            settings.Add(new KeyValuePair<string, string>("font", "Arial,14,Bold,Normal,#FFFFFFFF,#FFFFFFFF"));
            settings.Add(new KeyValuePair<string, string>("background", ""));
            settings.Add(new KeyValuePair<string, string>("bg_opacity", "90"));
            settings.Add(new KeyValuePair<string, string>("hide", ""));
            settings.Add(new KeyValuePair<string, string>("hide_settings", "False"));
            settings.Add(new KeyValuePair<string, string>("startup", "False"));
            settings.Add(new KeyValuePair<string, string>("download_icons", "True"));
            settings.Add(new KeyValuePair<string, string>("shortcut_key", "Tab"));
            settings.Add(new KeyValuePair<string, string>("modifier", "Control"));
            settings.Add(new KeyValuePair<string, string>("hide_random", "False"));

            foreach (KeyValuePair<string, string> s in settings)
            {
                try
                {
                    GetSetting("Main", s.Key);
                }
                catch (NullReferenceException)
                {
                    File.AppendAllText(SETTINGS_FILE, Environment.NewLine + s.Key + "=" + s.Value);
                    LogToFile("Setting \"" + s.Key + "\" had to be rebuilt.");
                }
            }

            // Remove blank lines
            var lines = File.ReadAllLines(SETTINGS_FILE).Where(arg => !string.IsNullOrWhiteSpace(arg));
            File.WriteAllLines(SETTINGS_FILE, lines);
        }

}
}
