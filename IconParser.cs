using System;
using HtmlAgilityPack;

namespace Steam_Game_Launcher
{
    /// <summary>
    /// Class to parse the url from the SteamDB databse.
    /// </summary>
    public class IconParser
    {
        public static readonly string STEAM_CDN = io.GetSetting("Advanced", "steam_cdn", io.configType.application);

        public static string GetIconURL(string gameID)
        {
            string Url = "https://steamdb.info/app/" + gameID + "/info/";
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(Url);

                string xpath = io.GetSetting("Advanced", "xpath", Steam_Game_Launcher.io.configType.application);
                string iconID = doc.DocumentNode.SelectNodes(xpath)[0].InnerText;
                if (string.IsNullOrEmpty(iconID))
                {
                    return null;
                }
                return STEAM_CDN + gameID + "/" + iconID + ".ico";
            }
            catch(Exception e)
            {
                io.LogToFile(e.ToString());
                return null;
            }
        }
    }
}
