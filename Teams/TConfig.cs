using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TShockAPI;

namespace Teams
{
    class TConfig
    {
        public static bool UsingPerma = false;
        public static List<string> PermaTeams = new List<string>();
        public static bool PermaPerm = false;
        public static int MaxPublicTeams = 0;
        public static int MaxPrivateTeams = 0;

        public static void SetupConfig()
        {
            if (!Directory.Exists(@"tshock/PluginConfigs"))
            {
                Directory.CreateDirectory(@"tshock/PluginConfigs");
            }
            if (!File.Exists(@"tshock/PluginConfigs/TeamsConfig.txt"))
            {
                File.WriteAllText(@"tshock/PluginConfigs/TeamsConfig.txt",
                    "######################" + Environment.NewLine +
                    "##Config Explaination:" + Environment.NewLine +
                    "######################" + Environment.NewLine +
                    "##OnlyAllowPermanentTeams --- (Boolean, Default: false) --- If this is set to true you will only be able to join the teams specified in PermanentTeams (unless you have the permission TeamAdmin)" + Environment.NewLine +
                    "##PermanentTeams --- (String, Default: \"\") --- This is a list of teams seperated by a '|' That are permanently in the Public teams list." + Environment.NewLine +
                    "##PermanentTeamPermissions --- (Boolean, Default: false) --- ." + Environment.NewLine +
                    "##MaxPublicTeams --- (Integer, Default: \"0\") --- This is the maximum number of Public teams that can be created, Set to \"0\" for unlimited public teams." + Environment.NewLine +
                    "##MaxPrivateTeams --- (Integer, Default: \"0\") --- This is the maximum number of Private teams that can be created, Set to \"0\" for unlimited public teams." + Environment.NewLine +
                    "################" + Environment.NewLine +
                    "##Actual Config:" + Environment.NewLine +
                    "################" + Environment.NewLine +
                    "OnlyAllowPermanentTeams:false" + Environment.NewLine +
                    "PermanentTeams:\"\"" + Environment.NewLine +
                    "PermanentTeamPermissions:false" + Environment.NewLine +
                    "MaxPublicTeams:\"0\"" + Environment.NewLine +
                    "MaxPrivateTeams:\"0\"");
                UsingPerma = false;
                PermaTeams = new List<string>();
                PermaPerm = false;
                MaxPublicTeams = 0;
                MaxPrivateTeams = 0;
            }
            else
            {
                using (StreamReader file = new StreamReader(@"tshock/PluginConfigs/TeamsConfig.txt", true))
                {
                    string[] rFile = (file.ReadToEnd()).Split('\n');
                    foreach (string currentLine in rFile)
                    {
                        try
                        {
                            if (currentLine.StartsWith("OnlyAllowPermanentTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 24);
                                if (tempLine.StartsWith("false"))
                                    UsingPerma = false;
                                else if (tempLine.StartsWith("true"))
                                    UsingPerma = true;
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error in TeamsConfig file - OnlyAllowPermanentTeams");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    UsingPerma = false;
                                }
                            }
                            else if (currentLine.StartsWith("PermanentTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 15);
                                string splithis = tempLine.Split('\"')[1];
                                string[] tempTeams = splithis.Split('|');
                                PermaTeams = new List<string>();
                                bool tell = false;
                                foreach (string permt in tempTeams)
                                {
                                    string addt = permt.ToLower();
                                    if (addt.Contains(' '))
                                        tell = true;
                                    else
                                    {
                                        if (addt != "")
                                        {
                                            PermaTeams.Add(addt);
                                            if (!Teams.TeamList.ContainsKey(addt))
                                                Teams.TeamList.Add(addt, "");
                                        }
                                    }
                                }

                                if (tell)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error in TeamsConfig file - PermanentTeams can not have spaces in them");
                                    Console.WriteLine("Any teams with spaces have been removed!");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                }
                            }
                            else if (currentLine.StartsWith("PermanentTeamPermissions:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 25);
                                if (tempLine.StartsWith("false"))
                                    PermaPerm = false;
                                else if (tempLine.StartsWith("true"))
                                    PermaPerm = true;
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error in TeamsConfig file - PermanentTeamPermissions");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    UsingPerma = false;
                                }
                            }
                            else if (currentLine.StartsWith("MaxPublicTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 15);
                                int len = int.Parse(tempLine.Split('\"')[1]);
                                MaxPublicTeams = len;
                            }
                            else if (currentLine.StartsWith("MaxPrivateTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 16);
                                int len = int.Parse(tempLine.Split('\"')[1]);
                                MaxPrivateTeams = len;
                            }
                        }
                        catch (Exception)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error in TeamsConfig file, Default Config being used!", Color.IndianRed);
                            Console.ForegroundColor = ConsoleColor.Gray;
                            UsingPerma = false;
                            PermaTeams = new List<string>();
                            MaxPublicTeams = 0;
                            MaxPrivateTeams = 0;
                            return;
                        }
                    }
                }
            }
        }

        public static void ReloadConfig(CommandArgs args)
        {
            bool oldusing = UsingPerma;
            List<string> oldteams = PermaTeams;

            #region RL Config
            if (!Directory.Exists(@"tshock/PluginConfigs"))
            {
                Directory.CreateDirectory(@"tshock/PluginConfigs");
            }
            if (!File.Exists(@"tshock/PluginConfigs/TeamsConfig.txt"))
            {
                File.WriteAllText(@"tshock/PluginConfigs/TeamsConfig.txt",
                    "######################" + Environment.NewLine +
                    "##Config Explaination:" + Environment.NewLine +
                    "######################" + Environment.NewLine +
                    "##OnlyAllowPermanentTeams --- (Boolean, Default: false) --- If this is set to true you will only be able to join the teams specified in PermanentTeams (unless you have the permission TeamAdmin)" + Environment.NewLine +
                    "##PermanentTeams --- (String, Default: \"\") --- This is a list of teams seperated by a '|' That are permanently in the Public teams list." + Environment.NewLine +
                    "##PermanentTeamPermissions --- (Boolean, Default: false) --- ." + Environment.NewLine +
                    "##MaxPublicTeams --- (Integer, Default: \"0\") --- This is the maximum number of Public teams that can be created, Set to \"0\" for unlimited public teams." + Environment.NewLine +
                    "##MaxPrivateTeams --- (Integer, Default: \"0\") --- This is the maximum number of Private teams that can be created, Set to \"0\" for unlimited public teams." + Environment.NewLine +
                    "################" + Environment.NewLine +
                    "##Actual Config:" + Environment.NewLine +
                    "################" + Environment.NewLine +
                    "OnlyAllowPermanentTeams:false" + Environment.NewLine +
                    "PermanentTeams:\"\"" + Environment.NewLine +
                    "PermanentTeamPermissions:false" + Environment.NewLine +
                    "MaxPublicTeams:\"0\"" + Environment.NewLine +
                    "MaxPrivateTeams:\"0\"");
                UsingPerma = false;
                PermaTeams = new List<string>();
                PermaPerm = false;
                MaxPublicTeams = 0;
                MaxPrivateTeams = 0;
            }
            else
            {
                using (StreamReader file = new StreamReader(@"tshock/PluginConfigs/TeamsConfig.txt", true))
                {
                    string[] rFile = (file.ReadToEnd()).Split('\n');
                    foreach (string currentLine in rFile)
                    {
                        try
                        {
                            if (currentLine.StartsWith("OnlyAllowPermanentTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 24);
                                if (tempLine.StartsWith("false"))
                                    UsingPerma = false;
                                else if (tempLine.StartsWith("true"))
                                    UsingPerma = true;
                                else
                                {
                                    args.Player.SendMessage("Error in TeamsConfig file - OnlyAllowSpecifiedTeams", Color.IndianRed);
                                    UsingPerma = false;
                                }
                            }
                            else if (currentLine.StartsWith("PermanentTeams:"))
                            {
                                string tempLine = currentLine.ToLower();
                                tempLine = tempLine.Remove(0, 15);
                                string splithis = tempLine.Split('\"')[1];
                                string[] tempTeams = splithis.Split('|');
                                PermaTeams = new List<string>();
                                bool tell = false;
                                foreach (string permt in tempTeams)
                                {
                                    string addt = permt.ToLower();
                                    if (addt.Contains(' '))
                                        tell = true;
                                    else
                                    {
                                        if (addt != "")
                                        {
                                            PermaTeams.Add(addt);
                                            if (!Teams.TeamList.ContainsKey(addt))
                                                Teams.TeamList.Add(addt, "");
                                        }
                                    }
                                }
                                if (tell)
                                {
                                    args.Player.SendMessage("Error in TeamsConfig file - PermanentTeams can not have spaces in them", Color.IndianRed);
                                    args.Player.SendMessage("Any teams with spaces have been removed!", Color.IndianRed);
                                }
                            }
                            else if (currentLine.StartsWith("PermanentTeamPermissions:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 25);
                                if (tempLine.StartsWith("false"))
                                    PermaPerm = false;
                                else if (tempLine.StartsWith("true"))
                                    PermaPerm = true;
                                else
                                {
                                    args.Player.SendMessage("Error in TeamsConfig file - PermanentTeamPermissions", Color.IndianRed);
                                    UsingPerma = false;
                                }
                            }
                            else if (currentLine.StartsWith("MaxPublicTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 15);
                                int len = int.Parse(tempLine.Split('\"')[1]);
                                MaxPublicTeams = len;
                            }
                            else if (currentLine.StartsWith("MaxPrivateTeams:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 16);
                                int len = int.Parse(tempLine.Split('\"')[1]);
                                MaxPrivateTeams = len;
                            }
                        }
                        catch (Exception)
                        {
                            args.Player.SendMessage("Error in TeamsConfig file, Default Config being used!", Color.IndianRed);
                            UsingPerma = false;
                            PermaTeams = new List<string>();
                            MaxPublicTeams = 0;
                            MaxPrivateTeams = 0;
                            return;
                        }
                    }
                }
            }
            #endregion

            if (UsingPerma)
            {
                Teams.TeamList = new Dictionary<string, string>();
                foreach (string team in TConfig.PermaTeams)
                {
                    if (!Teams.TeamList.ContainsKey(team))
                        Teams.TeamList.Add(team, "");
                    else
                    {
                        Dictionary<string, string> newteams = new Dictionary<string, string>();
                        foreach (KeyValuePair<string, string> Pair in Teams.TeamList)
                        {
                            if (Pair.Key == team && Pair.Value != "")
                            {
                                newteams.Add(team, "");
                            }
                            else
                                newteams.Add(Pair.Key, Pair.Value);
                        }
                        //Teams.TeamList.Clear();
                        Teams.TeamList = newteams;
                    }
                }
            }
            args.Player.SendMessage("Config Reloaded!", Color.MediumSeaGreen);
        }
    }
}
