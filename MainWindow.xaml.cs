using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Drawing;
using Media = System.Windows.Media;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Steam.Local;

namespace Steam_Game_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static string iconSize;                          // Refered to by the launcher window to set image size (icons)
        public static double[] panelMargin = new double[4];     // Margin around the panel holding the icons
        public static double[] iconSpacing = new double[4];     // Spacing around the icons
        public static double labelPaddingTop;                   // Padding between icons and labels
        public static TextBlock lblTemplate = new TextBlock();  // Template label for icons
        public static string background = null;                 // Holds the background path
        public static double bgOpacity = 15;                    // Holds background opacity
        public static bool downloadIcons;                       // Holds download icons setting    
        public static bool hideSettings;                        // Holds the settings page hiding settings    
        public static bool userRequestedPage = false;           // Used to show hidden settings page when requested
        public static string shortcutKey;                       // Shortcut key
        public static string shortcutModifier;                  // Shortcut Modifier
        public static bool hideRandomIcon = false;              // Setting to hide random icon

        const string SETTINGS_FILE = "userconfig.ini";
        public const string APPNAME = "SteamGameLauncher";
        const string APPVER = "0.3 BETA";

        public MainWindow()
        {
            // Set the working directory to the current EXE Path
            Environment.CurrentDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            InitializeComponent();
            
        }

        // First run check for config file.
        public static void FirstRunCheck()
        {
            if (!File.Exists(SETTINGS_FILE) || string.IsNullOrEmpty(File.ReadAllText(SETTINGS_FILE)))
            {
                try
                {
                    File.Copy(SETTINGS_FILE +".default", SETTINGS_FILE);
                }
                catch(Exception e)
                {
                    System.Windows.MessageBox.Show("Unable to find the user config file and unable to rebuild it. Please download a fresh copy. (" +
                        e.ToString() + ") ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }
            }
        }

        // Browse for steam library folders.
        private void btBrowseLibrary_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select your \"steamapps\" folder.";
            if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                io.steamLibrariesList.Add(fbd.SelectedPath + "\\");
                lbSteamLibraries.Items.Add(fbd.SelectedPath + "\\");
                io.manifestsList.Clear();
                io.gamesList.Clear();
                ParseGames();
            }
        }

        // Show Launcher
        private void ShowLauncher(bool saveSettings = true)
        {
            if (saveSettings)
            {
                SaveSettings();
            }
            Hide();
            LauncherWindow launcher = new LauncherWindow();
            launcher.Show();
            Close();
        }

        private void btLauncher_Click(object sender, RoutedEventArgs e)
        {
            ShowLauncher();   
        }

        private void LoadSettings()
        {            
            // Load libraries from config
            Log("Loading steam libraries...");
            string libs = io.GetSetting("Main", "libraries");
            if (!string.IsNullOrEmpty(libs))
            {
                string[] libraries = libs.Split(',');
                foreach (string s in libraries)
                {
                    lbSteamLibraries.Items.Add(s);
                    io.steamLibrariesList.Add(s);
                }
            }

            // Icon Size
            string size = io.GetSetting("Main", "icon_size");
            if (size == "64")
                rb64.IsChecked = true;
            else if (size == "128")
                rb128.IsChecked = true;
            else if (size == "256")
                rb256.IsChecked = true;
            else
                size = "128"; // Defaults to medium
            iconSize = size;

            // Panel margin
            string sMargin = io.GetSetting("Main", "panel_margin");            
            if (!string.IsNullOrEmpty(sMargin))
            {
                string[] margin;
                margin = sMargin.Split(',');
                if (margin.Length < 4)
                {
                    Log("**ERROR** The margin value length is incorrect in the config file.");
                }
                else
                {
                    panelMargin[0] = double.Parse(margin[0]);
                    panelMargin[1] = double.Parse(margin[1]);
                    panelMargin[2] = double.Parse(margin[2]);
                    panelMargin[3] = double.Parse(margin[3]);
                    tbPanelMarginLeft.Text = margin[0];
                    tbPanelMarginTop.Text = margin[1];
                    tbPanelMarginRight.Text = margin[2];
                    tbPanelMarginBottom.Text = margin[3];                    
                }
            }

            // Icon Spacing
            string sSpacing = io.GetSetting("Main", "icon_spacing");
            if (!string.IsNullOrEmpty(sSpacing))
            {
                string[] spacing;
                spacing= sSpacing.Split(',');
                if (spacing.Length < 4)
                {
                    Log("**ERROR** The icon spacing value length is incorrect in the config file.");
                }
                else
                {
                    iconSpacing[0] = double.Parse(spacing[0]);
                    iconSpacing[1] = double.Parse(spacing[1]);
                    iconSpacing[2] = double.Parse(spacing[2]);
                    iconSpacing[3] = double.Parse(spacing[3]);
                    tbIconSpacingLeft.Text = spacing[0];
                    tbIconSpacingTop.Text = spacing[1];
                    tbIconSpacingRight.Text = spacing[2];
                    tbIconSpacingBottom.Text = spacing[3];
                    
                }
            }

            // Label Padding
            string padding = io.GetSetting("Main", "label_padding");
            if(!string.IsNullOrEmpty(padding))
            {
                labelPaddingTop = double.Parse(padding);
            }

            // Font Settings
            string fontSetup = io.GetSetting("Main", "font");
            if (!string.IsNullOrEmpty(fontSetup))
            {
                string[] fontSettings = fontSetup.Split(',');
                if (fontSettings.Length >= 5)
                {
                    lblTemplate.FontFamily = new Media.FontFamily(fontSettings[0]);
                    lblTemplate.FontSize = double.Parse(fontSettings[1]);
                    lblTemplate.FontWeight = fontSettings[2] == "Bold" ? FontWeights.Bold : FontWeights.Regular;
                    lblTemplate.FontStyle = fontSettings[3] == "Italic" ? FontStyles.Italic : FontStyles.Normal;
                    lblTemplate.Foreground = new SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(fontSettings[4]));
                    colorPicker.SelectedColor = (Media.Color)Media.ColorConverter.ConvertFromString(fontSettings[4]);
                }
            }

            // Background
            tbBackground.Text = io.GetSetting("Main", "background");
            background = tbBackground.Text;
            string op = io.GetSetting("Main", "bg_opacity");
            if (!string.IsNullOrEmpty(op))
            {
                slOpacity.Value = double.Parse(op);
                bgOpacity = slOpacity.Value;
            }

            // Checkboxes
            cbSkipSettings.IsChecked = io.GetSetting("Main", "hide_settings") == "True" ? true : false;
            cbRunOnStartup.IsChecked = io.GetSetting("Main", "startup") == "True" ? true : false;
            cbDownloadIcons.IsChecked = io.GetSetting("Main", "download_icons") == "True" ? true : false;
            downloadIcons = cbDownloadIcons.IsChecked.Value;
            hideSettings = cbSkipSettings.IsChecked.Value;

            // Shortcut
            tbShortcut.Text = io.GetSetting("Main", "shortcut_key");
            tbModifier.Text = io.GetSetting("Main", "modifier");
            shortcutKey = tbShortcut.Text;
            shortcutModifier = tbModifier.Text;

            // Hidden apps
            io.hiddenList = new List<string>(io.GetSetting("Main", "hide").Split(','));
            lbHidden.ItemsSource = io.hiddenList;

            // Random button
            cbHideRandom.IsChecked = io.GetSetting("Main", "hide_random") == "True" ? true : false;

        }

        // Write all settings to ini file (user)
        private void SaveSettings()
        {
            io.WriteSetting("Main", "libraries", string.Join(",",io.steamLibrariesList.ToArray()));
            io.WriteSetting("Main", "icon_size", iconSize);
            io.WriteSetting("Main", "panel_margin", string.Join(",", panelMargin));
            io.WriteSetting("Main", "icon_spacing", string.Join(",", iconSpacing));
            io.WriteSetting("Main", "label_padding", labelPaddingTop.ToString());
            // THERE MUST BE A BETTER WAY
            string fontSettings;
            fontSettings = lblTemplate.FontFamily.ToString() + "," + lblTemplate.FontSize.ToString()
            + "," + lblTemplate.FontWeight.ToString() + "," + lblTemplate.FontStyle.ToString()
            + "," + lblTemplate.Foreground.ToString() + "," + colorPicker.SelectedColor.ToString();
            io.WriteSetting("Main", "font", fontSettings);
            // BUT HEY IT WORKS FOR NOW
            io.WriteSetting("Main", "background", tbBackground.Text);
            io.WriteSetting("Main", "bg_opacity", (slOpacity.Value).ToString());
            io.WriteSetting("Main", "hide", string.Join(",", io.hiddenList));
            io.WriteSetting("Main", "hide_settings", cbSkipSettings.IsChecked.ToString());
            io.WriteSetting("Main", "startup", cbRunOnStartup.IsChecked.ToString());
            io.WriteSetting("Main", "download_icons", cbDownloadIcons.IsChecked.ToString());
            io.WriteSetting("Main", "shortcut_key", tbShortcut.Text);
            io.WriteSetting("Main", "modifier", tbModifier.Text);
            io.WriteSetting("Main", "hide_random", cbHideRandom.IsChecked.ToString());
        }

        // On text changed, update the arrays holding the informations on margins/spacing
        private void SaveSpacingMargins(object sender, TextChangedEventArgs e)
        {            
            string control = ((System.Windows.Controls.TextBox)sender).Name;
            switch (control)
            {
                case "tbPanelMarginLeft": panelMargin[0] = Double.Parse(tbPanelMarginLeft.Text); break;
                case "tbPanelMarginTop": panelMargin[1] = Double.Parse(tbPanelMarginTop.Text); break;
                case "tbPanelMarginRight": panelMargin[2] = Double.Parse(tbPanelMarginRight.Text); break;
                case "tbPanelMarginBottom": panelMargin[3] = Double.Parse(tbPanelMarginBottom.Text); break;
                case "tbIconSpacingLeft": iconSpacing[0] = Double.Parse(tbIconSpacingLeft.Text); break;
                case "tbIconSpacingTop": iconSpacing[1] = Double.Parse(tbIconSpacingTop.Text); break;
                case "tbIconSpacingRight": iconSpacing[2] = Double.Parse(tbIconSpacingRight.Text); break;
                case "tbIconSpacingBottom": iconSpacing[3] = Double.Parse(tbIconSpacingBottom.Text); break;
                default: return;
            }        
        }

        // Populate gameList with Game objects
        private void ParseGames()
        {            
            Log("Parsing games...");
            io.GenerateManifestList();
            io.ParseGameNames();
            Log("Found " + io.gamesList.Count.ToString() + " games.");
        }

        // Enum for logging
        public enum msgType { Normal, Alert };

        // Easier logging because im lazy
        public void Log(string msg, msgType msgT = msgType.Normal)
        {
            lbLog.Items.Add(msg);
            lbLog.SelectedIndex = lbLog.Items.Count - 1;
            lbLog.ScrollIntoView(lbLog.SelectedItem);
        }

        // Start the building process once the form is initialized
        private void fmSettings_Initialized(object sender, EventArgs e)
        {
            Title = "Settings - SteamGameLauncher " + APPVER;
            FirstRunCheck();
            io.VerifyUserSettings();     
            LoadSettings();
            ParseGames();

            // Skip hide settings is set, chow launcher immediately
            if (hideSettings && !userRequestedPage)
            {
                ShowLauncher(false); // Show launcher but don't save settings
            }
        }

        // Validate text for panel margins and icon spacing
        private void tbPanelMargin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Save selected font to template
        private void btFont_Click(object sender, RoutedEventArgs e)
        {
            FontDialog fd = new FontDialog();
            DialogResult result = fd.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                Font fnt = fd.Font;
                lblTemplate.FontFamily = new Media.FontFamily(fnt.Name);
                lblTemplate.FontSize = fnt.Size;
                lblTemplate.FontWeight = fnt.Bold ? FontWeights.Bold : FontWeights.Regular;
                lblTemplate.FontStyle = fnt.Italic ? FontStyles.Italic : FontStyles.Normal;                
            }
        }

        // Browse for background
        private void btBgBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Choose a background image.";
            ofd.Filter = "Images|*.png;*.jpeg;*.jpg;*.bmp";
            ofd.CheckFileExists = true;
            DialogResult result = ofd.ShowDialog();
            {
                if(result == System.Windows.Forms.DialogResult.OK)
                {
                    if (File.Exists(ofd.FileName))
                    {
                        background = ofd.FileName;
                        tbBackground.Text = background;                        
                    }
                }
            }
        }
       
        private void slOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bgOpacity = slOpacity.Value;
            lblOpacity.Content = Math.Truncate(slOpacity.Value).ToString() + " %";
        }

        private void colorPicker_Closed(object sender, RoutedEventArgs e)
        {
            lblTemplate.Foreground = new SolidColorBrush((Media.Color)colorPicker.SelectedColor);
        }

        private void cbDownloadIcons_Click(object sender, RoutedEventArgs e)
        {
            downloadIcons = cbDownloadIcons.IsChecked.Value;
        }

        private void cbRunOnStartup_Click(object sender, RoutedEventArgs e)
        {
            io.SetStartup(cbRunOnStartup.IsChecked.Value);
        }

        // Capture key
        private void tbShortcut_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            // Ignore modifier keys.
            if (key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin)
            {
                return;
            }           

            tbShortcut.Text = key.ToString();
            shortcutKey = tbShortcut.Text;
        }

        // Capture modifier key
        private void tbModifier_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            // Ignore modifier keys.
            
            // Build the shortcut key name.
            StringBuilder shortcutText = new StringBuilder();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append("Control");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append("Shift");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append("Alt");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
            {
                shortcutText.Append("Windows");
            }

            // Update the text box.
            tbModifier.Text = shortcutText.ToString();
            shortcutModifier = tbModifier.Text;
        }

        // Save settings when closing the window
        private void fmSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        // Remove item from hidden list.
        private void btAddHidden_Click(object sender, RoutedEventArgs e)
        {
            int index = lbHidden.SelectedIndex;
            if(index > -1)
            {
                MakeGameVisible(lbHidden.Items[index].ToString());
                io.hiddenList.Remove(lbHidden.Items[index].ToString());
                lbHidden.Items.Refresh();
            }
        }

        // Make game object visible again
        private void MakeGameVisible(string id)
        {
            foreach (Game game in io.gamesList)
            {
                if(game.ID == id)
                {
                    game.Visible = true;
                    break;
                }
            }
        }  
        
        // Gets the game name from the ID
        private string GetGameName(string id)
        {
            foreach(Game game in io.gamesList)
            {
                if(game.ID == id)
                {
                    return game.name;
                }
            }
            return "Not found.";
        }

        // Display game name
        private void lbHidden_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbHidden.SelectedIndex > -1)
            {
                lblName.Content = GetGameName(lbHidden.SelectedItem.ToString());
            }
        }

        private void btRemoveLibrary_Click(object sender, RoutedEventArgs e)
        {
            int index = lbSteamLibraries.SelectedIndex;
            if (index > -1)
            {
                io.steamLibrariesList.Remove(lbSteamLibraries.Items[index].ToString());
                lbSteamLibraries.Items.RemoveAt(index);
            }
        }

        // Scan libraries automatically
        private void btScanLibraries_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (string lib in Libraries.Folders)
                {
                    string path = Path.GetFullPath(lib);
                    DirectoryInfo dinfo = new DirectoryInfo(path);
                    path = UppercaseFirst(GetProperDirectoryCapitalization(dinfo));
                    // Check if not already in the list.
                    if (lbSteamLibraries.Items.IndexOf(path) == -1)
                    {
                        lbSteamLibraries.Items.Add(path);
                        io.steamLibrariesList.Add(path);                        
                    }
                }
                io.manifestsList.Clear();
                io.gamesList.Clear();
                ParseGames();
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show("Unable to scan libraries. " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                io.LogToFile(ex.ToString());
            }
        }

        private static string GetProperDirectoryCapitalization(DirectoryInfo dirInfo)
        {
            DirectoryInfo parentDirInfo = dirInfo.Parent;
            if (null == parentDirInfo)
                return dirInfo.Name;
            return Path.Combine(GetProperDirectoryCapitalization(parentDirInfo),
                                parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
        }

        private static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        private void cbHideRandom_Click(object sender, RoutedEventArgs e)
        {
            hideRandomIcon = cbHideRandom.IsChecked.Value;
        }

    }
}
