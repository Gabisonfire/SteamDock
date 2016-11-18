# SteamGameLauncher

SteamGameLauncher creates a launch pad for your installed games.

  - Customizable
  - Automatic icons download
  - Lists only installed games!
  - Launch a random game

[Download Here](https://github.com/Gabisonfire/SteamGameLauncher/releases/latest)  
  
![Not found](/Screenshots/main.png?raw=true "Launcher")
![Not found](/Screenshots/main2.png?raw=true "Launcher")

### Installation
Installation is really easy, just extract the archive to any directory. Use a directory you have write access to write the config file. You will need to have the *.NET Framework 4.5* installed. Launch SteamGameLauncher.exe.

### First Run
On the first run, and subsequent unless you choose otherwise), you will be prompted with this window:

![Not found](/Screenshots/settings.png?raw=true "Settings")

 - You can change the *font* size, type and color for the labels under the icons
 - *Panel Margins*: Sets the margins between the icons panel and the end of the screen
 - *Icons Spacing*: Spacing between each icons
 - *Hidden Apps*: Here is where the apps you wanted to hide will appear if you want to unhide them. You can hide unwanted apps by right-clicking them and choosing "Hide" from the context menu on the launcher screen.
 - *Icon size*: Change the size of each icons, 128px looks the best in my opinion.
 - *Don't show settings page*: Check this box to skip the settings screen on startup. It will still be accessible from the context menu on the tray icon.
 - *Run when Windows starts*: Will set the registry to auto launch on startup.
 - *Download missing icons*: Will attempt to download icons from Steam's cdn.
 - *Hide "Random" button*: Hide the random button at the bottom of the launcher.
 - *Launch Toggle*: This is the key combination to show/hide the Launcher screen.
 - *Background & Opacity*: You can set your own background and opacity here.
 - *Launch*: Fire this when you are ready! The first run may take a while depending on how many games you have since it will have to dowload the icons.

If you install new games, all you need to do is relaunch the application.

### Custom Icons
Some of the downloaded icons are at a pretty low resolution due to the icon available on the site. You can add your own in the "icons" folder either overwriting the originals, or naming them after their appID. They can be easily found on SteamDB's website. For example, Portal 2 is id 620. Putting an png file named "620.png" would make the launcher use this one over the "Portal 2.png".

### Contribute

Feel free to contribute to this project if you like it or if you have any suggestions, drop a pull request and i'll be happy to take a look.

SteamGameLauncher uses the following NuGet packages:
 - Extended.Wpf.Toolkit by Xceed
 - Hardcodet.NotifyIcon.Wpf by Philipp Sumi
 - HtmlAgilityPack by Simon Mourrier, Jeff Klawiter and Stephan Grell
 - MadMilkman.Ini by Mario Zorica
 - NHotkey.Wpf by Thomas Levesque
 - Steam-Local by ObsidianMinor

### Credits
The original wallpapers included with the application and showed in the screenshots are "Lost Aura" by filipe-ps and "Linear Retro" by MindWav3 from DeviantArt

The default icon is made by Chanut is Industries from www.flaticon.com, is licensed by "Creative Commons BY 3.0".

