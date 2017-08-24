using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Net;
using NHotkey.Wpf;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using SteamKit2;

namespace Steam_Dock
{
    /// <summary>
    /// Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window
    {

        public const string ICONS_DIR = @"icons\";

        public LauncherWindow()
        {
            InitializeComponent();
            GrabHotkey();            
        }

        // Grab hotkey
        private void GrabHotkey()
        {            
            try
            {
                KeysConverter keyConv = new KeysConverter();
                Key key = (Key)Enum.Parse(typeof(Key), MainWindow.shortcutKey);
                ModifierKeys mod = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), MainWindow.shortcutModifier);                
                HotkeyManager.Current.AddOrReplace("Maximize", key, mod, Toggle);
                io.LogToFile("Hotkey grabbed: " + MainWindow.shortcutModifier + " + " + MainWindow.shortcutKey);                           
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error occured assigning the global hotkey selected. \n " + ex.Message);
                io.LogToFile(ex.ToString());
            }
        }

        // Release hotkey
        private void ReleaseHotkey()
        {
            try
            {
                HotkeyManager.Current.Remove("Maximize");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error occured releasing the global hotkey selected. \n " + ex.Message);
                io.LogToFile(ex.ToString());
            }
        }

        // Toggle the launcher window.
        private void Toggle(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Maximized;
                this.Show();
                return;
            }
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Minimized;
                this.Hide();
                return;
            }
            e.Handled = true;
        }

        private void fmLauncher_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            Hide();
        }

        private void fmLauncher_Initialized(object sender, EventArgs e)
        {
            io.LogToFile("Launcher initialized.");
            // Set fullscreen based on primary monitor's resolution
            System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;
            Width = resolution.Width;
            Height = resolution.Height;
            io.LogToFile("Resolution: " + Width.ToString() + "X" + Height.ToString());

            // Show loding screen
            LoadingScreen Loading = new LoadingScreen();
            Loading.Width = resolution.Width;
            Loading.Height = resolution.Height;
            Loading.tbLoading.Margin = new Thickness(0,0,0,0);
            Loading.Show();

            // Set main panel margins and stack panel's size. Double.NaN sets it to auto.
            MainDockPanel.Height = resolution.Height;
            MainDockPanel.Width = resolution.Width;
            spMain.Width = Double.NaN;
            spMain.Height = Double.NaN;
            spMain.Margin = new Thickness(MainWindow.panelMargin[0], MainWindow.panelMargin[1], MainWindow.panelMargin[2], MainWindow.panelMargin[3]);

            iconTray.TrayMouseDoubleClick += IconTray_TrayMouseDoubleClick;

            // Set Background image and opacity
            if (!string.IsNullOrEmpty(MainWindow.background) && File.Exists(MainWindow.background))
            {
                string fullPath = Path.GetFullPath(MainWindow.background);
                ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(fullPath)));
                Background = null;
                brush.Opacity = MainWindow.bgOpacity / 100;
                Background = brush;
            }
            
            GenerateIcons();
            UpdateLayout();
            Loading.Close();
        }

        // tray icon double click handler
        private void IconTray_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            this.Show();
            this.Focus();
        }

        // Convert Bitmap to BitmapImage
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        // Generates icons image and places them on the stack panel
        private void GenerateIcons()
        {
            // Get all icons from the icons directory
            io.iconsList = Directory.GetFiles(ICONS_DIR, "*.png", SearchOption.TopDirectoryOnly).ToList<string>();
            io.gamesList.Sort();
                   
            foreach (Game game in io.gamesList)
            {
                io.LogToFile("Processing " + game.Name);
                // Skip hidden games.
                if (!game.Visible) { continue; }                
                // Try to get the icon
                string foundIcon = FindIcon(game);
                // Create the image to hold the icon
                HolderImage icon = new HolderImage();
                BitmapImage iconSource = new BitmapImage();
                iconSource.BeginInit();
                if (string.IsNullOrEmpty(foundIcon) || !File.Exists(ICONS_DIR + foundIcon + ".png"))
                {
                    iconSource.UriSource = new Uri("default.png", UriKind.Relative);
                    io.LogToFile("Using default icon.");
                }
                else
                {
                    iconSource.UriSource = new Uri(ICONS_DIR + foundIcon + ".png", UriKind.Relative);
                    io.LogToFile("Using " + iconSource.UriSource.ToString());
                }

                iconSource.CacheOption = BitmapCacheOption.OnLoad;
                try
                {
                    iconSource.EndInit();
                }
                catch (InvalidOperationException ex)
                {
                    io.LogToFile("Error processing " + game.Name + ". " + ex.ToString());
                    continue;
                }
                icon.Source = iconSource;

                // Create the events handlers
                icon.MouseLeftButtonDown += (sender, eventArgs) => { ButtonEffectDown(icon); };
                icon.MouseLeftButtonUp += (sender, eventArgs) => { LaunchGame(game); };
                icon.MouseEnter += (sender, eventArgs) => { Glow(icon, true); };
                icon.MouseLeave += (sender, eventArgs) => { Glow(icon, false); };
                // Add context menu
                System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu();
                System.Windows.Controls.MenuItem mItem = new System.Windows.Controls.MenuItem();
                mItem.Header = "Hide";
                mItem.Click += (s, eventArgs) => { HideIcon(icon); };
                menu.Items.Add(mItem);
                icon.ContextMenu = menu;

                // Set the icons size based on config file
                icon.Width = double.Parse(MainWindow.iconSize);
                icon.Height = double.Parse(MainWindow.iconSize);
                icon.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                icon.VerticalAlignment = VerticalAlignment.Center;
                icon.Effect = new System.Windows.Media.Effects.DropShadowEffect();

                // Create a label and stack panel
                TextBlock lbl = new TextBlock();
                lbl.Text = game.Name;
                lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                lbl.TextAlignment = TextAlignment.Center;
                lbl.TextTrimming = TextTrimming.CharacterEllipsis;
                lbl.Height = Double.NaN;    // auto
                lbl.Width = Double.NaN;     // auto
                lbl.FontFamily = MainWindow.lblTemplate.FontFamily;
                lbl.FontSize = MainWindow.lblTemplate.FontSize;
                lbl.FontStyle = MainWindow.lblTemplate.FontStyle;
                lbl.FontWeight = MainWindow.lblTemplate.FontWeight;
                lbl.Padding = new Thickness(0, MainWindow.labelPaddingTop, 0, 0);
                lbl.Foreground = MainWindow.lblTemplate.Foreground;
                lbl.Effect = new System.Windows.Media.Effects.DropShadowEffect();
                StackPanel sp = new StackPanel();
                sp.Width = icon.Width + icon.Margin.Left + icon.Margin.Right;
                sp.Height = icon.Height + lbl.Height + icon.Margin.Top + icon.Margin.Bottom;
                sp.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                sp.Margin = new Thickness(MainWindow.iconSpacing[0], MainWindow.iconSpacing[1], MainWindow.iconSpacing[2], MainWindow.iconSpacing[3]);

                // Store the game object
                icon.game = game;

                // Add the icon to the stack panel created, then add that stack panel on the main panel.
                sp.Children.Add(icon);
                sp.Children.Add(lbl);
                spMain.Children.Add(sp);
            }
            if (!MainWindow.hideRandomIcon)
            {
                MakeRandomIcon();
            }
        }

        // Create "Random" button outside of WrapPanel
        private void MakeRandomIcon()
        {            
            double iconSize = Double.Parse(io.GetSetting("Main", "icon_size"));
            HolderImage randomIcon = new HolderImage();
            BitmapImage randomIconSource = new BitmapImage();
            randomIconSource.BeginInit();
            randomIconSource.UriSource = new Uri("dice.png", UriKind.Relative);
            randomIconSource.CacheOption = BitmapCacheOption.OnLoad;
            randomIconSource.EndInit();
            randomIcon.Source = randomIconSource;

            // Create the events handlers
            randomIcon.MouseEnter += (sender, eventArgs) => { Glow(randomIcon, true); };
            randomIcon.MouseLeave += (sender, eventArgs) => { Glow(randomIcon, false); };
            randomIcon.MouseLeftButtonDown += (sender, eventArgs) => { ButtonEffectDown(randomIcon); };
            Random random = new Random();
            if (!(io.gamesList.Count == 0))
            {
                randomIcon.MouseLeftButtonUp += (sender, eventArgs) => { LaunchGame(io.gamesList[random.Next(0, io.gamesList.Count - 1)]); };
            }
            randomIcon.VerticalAlignment = VerticalAlignment.Center;
            randomIcon.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            randomIcon.Height = iconSize / 2;
            randomIcon.Width = iconSize / 2;
            bottomPanel.Height = iconSize;

            // Create a label and stack panel
            TextBlock lbl = new TextBlock();
            lbl.Text = "Random";
            lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            lbl.TextAlignment = TextAlignment.Center;
            lbl.TextTrimming = TextTrimming.CharacterEllipsis;
            lbl.Height = Double.NaN;    // auto
            lbl.Width = Double.NaN;     // auto
            lbl.FontFamily = MainWindow.lblTemplate.FontFamily;
            lbl.FontSize = MainWindow.lblTemplate.FontSize;
            lbl.FontStyle = MainWindow.lblTemplate.FontStyle;
            lbl.FontWeight = MainWindow.lblTemplate.FontWeight;
            lbl.Padding = new Thickness(0, MainWindow.labelPaddingTop, 0, 0);
            lbl.Foreground = MainWindow.lblTemplate.Foreground;
            lbl.Effect = new System.Windows.Media.Effects.DropShadowEffect();
            StackPanel sp = new StackPanel();
            sp.Width = Double.NaN;
            sp.Height = Double.NaN;
            sp.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            sp.Children.Add(randomIcon);
            sp.Children.Add(lbl);
            bottomPanel.Children.Add(sp);
        }

        // Remove icon visually, add to hiddenlist
        private void HideIcon(HolderImage icon)
        {
            try
            {
                StackPanel panel = (StackPanel)icon.Parent;
                io.hiddenList.Add(icon.game.ID.ToString());
                panel.Children.RemoveRange(0, panel.Children.Count);
                WrapPanel mainPanel = (WrapPanel)panel.Parent;
                mainPanel.Children.Remove(panel);
                UpdateLayout();
                io.WriteSetting("Main", "Hide", string.Join(",", io.hiddenList));
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show("Could not hide icon. \n " + e.Message);
                io.LogToFile(e.ToString());
            }
        }

        // Find the best matching item using the game information.
        private string FindIcon(Game game)
        {            
            foreach (string f in io.iconsList)
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(f);
                string cleanedName = game.Name.Replace(":", " ").Replace("?", "");
                // Check first with the ID
                if (file == game.ID.ToString())
                {
                    return file;
                }
                //Second check is with the installdir name (works well with single word titles)
                else if (file == game.SafeName)
                {
                    return file;
                }
                // Third check with actual name removing invalid chars.                
                else if (file == cleanedName)
                {
                    return file;
                }
                // Fourth is to put all lowercase, remove spaces and hyphens
                else if (file.ToLower().Replace(" ", "").Replace("-", "").Replace(".", "") == cleanedName.ToLower().Replace(" ", "").Replace("-", ""))
                {
                    return file;
                }
            }

            // Last is to download the icon from Steam
            if (MainWindow.downloadIcons && DownloadIcon(game) && File.Exists(ICONS_DIR + game.SafeName + ".png"))
            {
                return game.SafeName;
            }
            else
            {
                return null;
            }
        }

        // Download icon after parsing url
        private bool DownloadIcon(Game game)
        {
            string iconURL = IconParser.GetIconURL(game.ID.ToString());
            if (string.IsNullOrEmpty(iconURL))
            {
                return false;
            }
            try
            {
                WebClient Client = new WebClient();
                io.LogToFile("Downloading icon from " + iconURL);
                Client.DownloadFile(iconURL, ICONS_DIR + game.SafeName + ".png");
                return true;
            }
            catch (Exception e)
            {
                io.LogToFile(e.ToString() + " GAMEID:" + game.ID);
                io.LogToFile(game.SafeName);
                return false;
            }
        }

        private void LaunchGame(Game game)
        {
            Process.Start(game.Exec);
            WindowState = WindowState.Minimized;
            Hide();
        }

        private void Quit()
        {
            iconTray.Dispose();
            Environment.Exit(0);
        }

        // Context menu exit
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        // Creates the button pressed down effect.
        private void ButtonEffectDown(HolderImage icon)
        {
            TranslateTransform trans = new TranslateTransform();
            icon.RenderTransform = trans;
            Double currentLeft = icon.Margin.Left;
            Double currentTop = icon.Margin.Top;
            DoubleAnimation anim1 = new DoubleAnimation(icon.Margin.Left + 3, currentLeft , TimeSpan.FromSeconds(0.2));
            DoubleAnimation anim2 = new DoubleAnimation(icon.Margin.Top + 3, currentTop , TimeSpan.FromSeconds(0.2));
            trans.BeginAnimation(TranslateTransform.XProperty, anim1);
            trans.BeginAnimation(TranslateTransform.YProperty, anim2);
        }

        // Hover effect
        private void Glow(HolderImage icon, bool hover)
        {
            System.Windows.Media.Effects.DropShadowEffect dse = new System.Windows.Media.Effects.DropShadowEffect();
            dse.Color = hover ? Colors.White : Colors.Black;
            dse.ShadowDepth = 0;
            dse.BlurRadius = 5;
                                     
            icon.Effect = dse;
        }

        // "Settings" from trayicon context menu
        private void MenuItem_Click_Settings(object sender, RoutedEventArgs e)
        {
            ClearLists();            
            Hide();
            MainWindow.userRequestedPage = true;  // Set the settings to show since user requested    
            MainWindow mw = new MainWindow();
            mw.Show();
            iconTray.Dispose();            
            Close();
        }

        // Clear lists manifest, icons, games, etc. (When relaunching settings)
        private void ClearLists()
        {
            io.gamesList.Clear();
            io.manifestsList.Clear();
            //io.steamLibrariesList.Clear();
            io.iconsList.Clear();
        }

        // Release the hotkey on close
        private void fmLauncher_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ReleaseHotkey();
        }
    }
}
