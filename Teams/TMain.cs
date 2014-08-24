using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Timers;
using System.Linq;
using System.Text;
using System.IO;
using Terraria;
using TShockAPI;
using Hooks;
using TPlayers;


namespace Teams
{
    [APIVersion(1, 11)]
    public class Teams : TerrariaPlugin
    {
        public static List<TPlayer> TPlayers = new List<TPlayer>();
        public static Dictionary<string, string> TeamList = new Dictionary<string, string>();
        public static Timer CheckT = new Timer(1000);
        public static int playercount = 0;
        public static bool locked = false;

        public override string Name
        {
            get { return "Teams"; }
        }

        public override string Author
        {
            get { return "by Scavenger"; }
        }

        public override string Description
        {
            get { return "Unlimited Teams!"; }
        }

        public override Version Version
        {
            get { return new Version("1.2.1"); }
        }

        public override void Initialize()
        {
            ServerHooks.Chat += OnChat;
            GameHooks.Initialize += OnInitialize;
            NetHooks.GreetPlayer += OnGreetPlayer;
            ServerHooks.Leave += OnLeave;
            NetHooks.GetData += GetData;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerHooks.Chat -= OnChat;
                GameHooks.Initialize -= OnInitialize;
                NetHooks.GreetPlayer -= OnGreetPlayer;
                ServerHooks.Leave -= OnLeave;
                NetHooks.GetData -= GetData;
            }
            base.Dispose(disposing);
        }

        public Teams(Main game)
            : base(game)
        {
            Order = -1;
        }

        public void OnInitialize()
        {
            CheckT.Elapsed += new ElapsedEventHandler(CheckT_Elapsed);
            if (playercount > 0)
                CheckT.Start();

            foreach (Group grp in TShock.Groups.groups)
            {
                if (grp.HasPermission("teamadmin") || grp.HasPermission("createpublicteam") || grp.HasPermission("createprivateteam"))
                {
                    if (!grp.HasPermission("jointeam"))
                        grp.AddPermission("jointeam");
                }
            }

            Commands.ChatCommands.Add(new Command("jointeam", teamcmd, "team"));
            Commands.ChatCommands.Add(new Command("canpartychat", teamchat, "tc"));

            TConfig.SetupConfig();
            if (TConfig.UsingPerma)
            {
                TeamList = new Dictionary<string, string>();
                foreach (string team in TConfig.PermaTeams)
                {
                    TeamList.Add(team, "");
                }
            }
        }

        public void OnGreetPlayer(int who, HandledEventArgs e)
        {
            lock (TPlayers)
                TPlayers.Add(new TPlayer(who));

            playercount++;
            if (playercount == 1)
                CheckT.Start();
        }

        public void OnLeave(int ply)
        {
            lock (TPlayers)
            {
                for (int i = 0; i < TPlayers.Count; i++)
                {
                    if (TPlayers[i].Index == ply)
                    {
                        TPlayers.RemoveAt(i);
                        break;
                    }
                }
            }
            playercount--;
            if (playercount == 0)
                CheckT.Stop();
        }

        public void OnChat(messageBuffer msgb, int ply, string text, HandledEventArgs e)
        {
            if (e.Handled)
                return;

            if (text.StartsWith("/p "))
            {
                var tsply = TShock.Players[ply];
                var tply = GetTPlayerByID(ply);
                int playerTeam = tsply.Team;

                if (playerTeam == 4 || (playerTeam == 0 && tply.team != ""))
                {
                    e.Handled = true;
                    TShockAPI.Commands.HandleCommand(tsply, "/tc" + text.Remove(0, 2));
                }
            }
        }

        public void GetData(GetDataEventArgs e)
        {
            try
            {
                if (e.MsgID == PacketTypes.PlayerTeam)
                {
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        var play = reader.ReadByte();
                        var team = reader.ReadByte();
                        TPlayer ply = GetTPlayerByID(play);
                        var tply = TShock.Players[play];
                        if (team == 4)
                        {
                            e.Handled = true;
                            tply.SetTeam(tply.Team);
                            if (ply.team != "")
                            {
                                bool lastperson = true;
                                foreach (TPlayer stply in TPlayers)
                                {
                                    if (stply.team == ply.team)
                                    {
                                        try
                                        {
                                            NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", stply.Index);
                                        }
                                        catch (Exception) { }
                                        if (stply != ply && stply.team != "x Yellow")
                                        {
                                            tply.SendMessage(stply.TSPlayer.Name + " has left the team.", Color.GreenYellow);
                                            lastperson = false;
                                        }
                                    }
                                }

                                if (ply.team == "admin" || ply.team == "x Yellow")
                                    lastperson = false;

                                if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                    TeamList.Remove(ply.team);

                                ply.team = "";
                                ply.TSPlayer.SetTeam(0);
                            }

                            ply.team = "x Yellow";
                        }
                        else if (team != 4 && ply.team != "")
                        {
                            bool lastperson = true;
                            foreach (TPlayer stply in TPlayers)
                            {
                                if (stply.team == ply.team)
                                {
                                    try
                                    {
                                        NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", stply.Index);
                                    }
                                    catch (Exception) { }
                                    if (stply != ply && stply.team != "x Yellow")
                                    {
                                        tply.SendMessage(stply.TSPlayer.Name + " has left the team.", Color.GreenYellow);
                                        lastperson = false;
                                    }
                                }
                            }

                            if (ply.team == "admin" || ply.team == "x Yellow")
                                lastperson = false;

                            if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                TeamList.Remove(ply.team);

                            ply.team = "";
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        static void CheckT_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (TPlayers)
                {
                    foreach (TPlayer playO in TPlayers)
                    {
                        foreach (TPlayer playD in TPlayers)
                        {
                            if ((playO.team == playD.team && playO.team != "") || playO.team == "admin")
                            {
                                Main.player[playO.Index].team = 4;
                                Main.player[playD.Index].team = 4;
                                NetMessage.SendData((int)PacketTypes.PlayerTeam, playO.Index, -1, "", playD.Index);
                                Main.player[playO.Index].team = 0;
                                Main.player[playD.Index].team = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public static TPlayer GetTPlayerByID(int id)
        {
            TPlayer player = null;
            foreach (TPlayer ply in TPlayers)
            {
                if (ply.Index == id)
                    return ply;
            }
            return player;
        }

        public static TPlayer GetTPlayerByName(string name)
        {
            var player = TShock.Utils.FindPlayer(name)[0];
            if (player != null)
            {
                foreach (TPlayer ply in TPlayers)
                {
                    if (ply.TSPlayer == player)
                        return ply;
                }
            }
            return null;
        }

        public static void teamcmd(CommandArgs args)
        {
            #region Check Erros and Permissions
            bool admin = args.Player.Group.HasPermission("teamadmin");
            bool create = args.Player.Group.HasPermission("createpublicteam");
            bool createprivate = args.Player.Group.HasPermission("createprivateteam");

            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You cannot use team commands!", Color.Red);
                return;
            }
            if (locked && !args.Player.Group.HasPermission("teamadmin"))
            {
                args.Player.SendMessage("Teams are on lockdown, You cannot join or leave teams!", Color.IndianRed);
                return;
            }
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Usage: /team <join/leave/list>", Color.IndianRed);
                return;
            }

            if (TConfig.PermaTeams.Count != 0)
            {
                foreach (string tm in TConfig.PermaTeams)
                {
                    if (!TeamList.ContainsKey(tm))
                    {
                        TeamList.Add(tm, "");
                    }
                }
            }

            TPlayer ply = GetTPlayerByID(args.Player.Index);
            string subcmd = args.Parameters[0].ToLower();
            #endregion

            if (subcmd == "join")
            {
                #region Join Team
                #region check errors
                if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
                {
                    args.Player.SendMessage("Usage: /team join <team name> [password]", Color.IndianRed);
                    args.Player.SendMessage("Team Name and Password can only be 1 world!", Color.IndianRed);
                    return;
                }

                string jointeam = args.Parameters[1].ToLower();
                string joinpassword = "";
                if (args.Parameters.Count == 3)
                    joinpassword = args.Parameters[2];

                if (jointeam == ply.team)
                {
                    args.Player.SendMessage("You are already in that team!", Color.IndianRed);
                    return;
                }
                if (jointeam == "admin" && !admin)
                {
                    args.Player.SendMessage("That team is reserved for admins!", Color.IndianRed);
                    return;
                }
                #endregion

                #region Join Admin Team
                if (jointeam == "admin" && admin)
                {
                    #region reset team
                    if (ply.team != "")
                    {
                        bool lastperson = true;
                        foreach (TPlayer tply in TPlayers)
                        {
                            if (tply.team == ply.team)
                            {
                                try
                                {
                                    NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                                }
                                catch (Exception) { }
                                if (tply != ply && tply.team != "x Yellow")
                                {
                                    tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                                    lastperson = false;
                                }
                            }
                        }

                        if (ply.team == "admin" || ply.team == "x Yellow")
                            lastperson = false;

                        if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                            TeamList.Remove(ply.team);

                        ply.team = "";
                        ply.TSPlayer.SetTeam(0);
                    }
                    #endregion
                    args.Player.SendMessage("You have joined the admin team. (you can see everyone!)", Color.MediumSeaGreen);
                    foreach (TPlayer tplay in TPlayers)
                    {
                        if (tplay.team == jointeam)
                            tplay.SendMessage(args.Player.Name + " has joined the team.", Color.GreenYellow);
                    }
                    args.Player.SetTeam(0);
                    ply.team = jointeam;
                    return;
                }
                #endregion

                #region Join Permanent Team
                if (TConfig.UsingPerma && (!admin || (admin && TeamList.ContainsKey(jointeam))))
                {
                    if (TConfig.PermaTeams.Contains(jointeam))
                    {
                        if (!TConfig.PermaPerm  || (TConfig.PermaPerm && args.Player.Group.HasPermission("joinperma-" + jointeam)))
                        {
                            #region reset team
                            if (ply.team != "")
                            {
                                bool lastperson = true;
                                foreach (TPlayer tply in TPlayers)
                                {
                                    if (tply.team == ply.team)
                                    {
                                        try
                                        {
                                            NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                                        }
                                        catch (Exception) { }
                                        if (tply != ply && tply.team != "x Yellow")
                                        {
                                            tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                                            lastperson = false;
                                        }
                                    }
                                }

                                if (ply.team == "admin" || ply.team == "x Yellow")
                                    lastperson = false;

                                if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                    TeamList.Remove(ply.team);


                                ply.team = "";
                                ply.TSPlayer.SetTeam(0);
                            }
                            #endregion
                            args.Player.SendMessage("You have joined the public team: " + jointeam, Color.MediumSeaGreen);
                            foreach (TPlayer tplay in TPlayers)
                            {
                                if (tplay.team == jointeam)
                                    tplay.SendMessage(args.Player.Name + " has joined the team.", Color.GreenYellow);
                            }
                            args.Player.SetTeam(0);
                            ply.team = jointeam;
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage("You do not have permission to join this team!", Color.IndianRed);
                            return;
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("Only the teams in \"/team list\" are avalible to join!", Color.IndianRed);
                        return;
                    }
                }
                #endregion

                if (TeamList.ContainsKey(jointeam))
                {
                    #region join existing team
                    if (TeamList[jointeam] == "")
                    {
                        #region reset team
                        if (ply.team != "")
                        {
                            bool lastperson = true;
                            foreach (TPlayer tply in TPlayers)
                            {
                                if (tply.team == ply.team)
                                {
                                    try
                                    {
                                        NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                                    }
                                    catch (Exception) { }
                                    if (tply != ply && tply.team != "x Yellow")
                                    {
                                        tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                                        lastperson = false;
                                    }
                                }
                            }

                            if (ply.team == "admin" || ply.team == "x Yellow")
                                lastperson = false;

                            if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                TeamList.Remove(ply.team);

                            ply.team = "";
                            ply.TSPlayer.SetTeam(0);
                        }
                        #endregion
                        args.Player.SendMessage("You have joined the public team: " + jointeam, Color.MediumSeaGreen);
                        foreach (TPlayer tplay in TPlayers)
                        {
                            if (tplay.team == jointeam)
                                tplay.SendMessage(args.Player.Name + " has joined the team.", Color.GreenYellow);
                        }
                        args.Player.SetTeam(0);
                        ply.team = jointeam;
                    }
                    else
                    {
                        if (TeamList[jointeam] == joinpassword || admin)
                        {
                            #region reset team
                            if (ply.team != "")
                            {
                                bool lastperson = true;
                                foreach (TPlayer tply in TPlayers)
                                {
                                    if (tply.team == ply.team)
                                    {
                                        try
                                        {
                                            NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                                        }
                                        catch (Exception) { }
                                        if (tply != ply && tply.team != "x Yellow")
                                        {
                                            tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                                            lastperson = false;
                                        }
                                    }
                                }

                                if (ply.team == "admin" || ply.team == "x Yellow")
                                    lastperson = false;

                                if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                    TeamList.Remove(ply.team);

                                ply.team = "";
                                ply.TSPlayer.SetTeam(0);
                            }
                            #endregion
                            args.Player.SendMessage("You have joined the private team: " + jointeam, Color.MediumSeaGreen);
                            foreach (TPlayer tplay in TPlayers)
                            {
                                if (tplay.team == jointeam)
                                    tplay.SendMessage(args.Player.Name + " has joined the team.", Color.GreenYellow);
                            }
                            args.Player.SetTeam(0);
                            ply.team = jointeam;
                        }
                        else if (TeamList[jointeam] != joinpassword && !admin)
                        {
                            args.Player.SendMessage("You have entered an Incorrect Password. (This team already Exists!)", Color.IndianRed);
                            return;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region Create team
                    if (create || createprivate)
                    {
                        if (joinpassword != "" && createprivate)
                        {
                            if (TConfig.MaxPrivateTeams != 0 && !admin)
                            {
                                int privateteamcount = 0;
                                foreach (KeyValuePair<string, string> Pair in TeamList)
                                {
                                    if (Pair.Value != "")
                                        privateteamcount++;
                                }

                                if (privateteamcount >= TConfig.MaxPrivateTeams)
                                {
                                    args.Player.SendMessage("You cannot create a private team as the number of teams have reached maximum!", Color.IndianRed);
                                    return;
                                }
                            }
                            #region reset team
                            if (ply.team != "")
                            {
                                bool lastperson = true;
                                foreach (TPlayer tply in TPlayers)
                                {
                                    if (tply.team == ply.team)
                                    {
                                        try
                                        {
                                            NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                                        }
                                        catch (Exception) { }
                                        if (tply != ply && tply.team != "x Yellow")
                                        {
                                            tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                                            lastperson = false;
                                        }
                                    }
                                }

                                if (ply.team == "admin" || ply.team == "x Yellow")
                                    lastperson = false;

                                if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                    TeamList.Remove(ply.team);

                                ply.team = "";
                                ply.TSPlayer.SetTeam(0);
                            }
                            #endregion
                            args.Player.SendMessage("You have created the private team: " + jointeam + " With the password: " + joinpassword, Color.MediumSeaGreen);
                            TeamList.Add(jointeam, joinpassword);
                            args.Player.SetTeam(0);
                            ply.team = jointeam;

                        }
                        else if (joinpassword != "" && !createprivate)
                        {
                            args.Player.SendMessage("You do not have permission to create private teams!", Color.IndianRed);
                            return;
                        }
                        else if (joinpassword == "" && create)
                        {
                            if (TConfig.MaxPublicTeams != 0 && !admin)
                            {
                                int publicteamcount = 0;
                                foreach (KeyValuePair<string, string> Pair in TeamList)
                                {
                                    if (Pair.Value == "")
                                        publicteamcount++;
                                }

                                if (publicteamcount >= TConfig.MaxPublicTeams)
                                {
                                    args.Player.SendMessage("You cannot create a public team as the number of teams have reached maximum!", Color.IndianRed);
                                    return;
                                }
                            }
                            #region reset team
                            if (ply.team != "")
                            {
                                bool lastperson = true;
                                foreach (TPlayer tply in TPlayers)
                                {
                                    if (tply.team == ply.team)
                                    {
                                        try
                                        {
                                            NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                                        }
                                        catch (Exception) { }
                                        if (tply != ply && tply.team != "x Yellow")
                                        {
                                            tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                                            lastperson = false;
                                        }
                                    }
                                }

                                if (ply.team == "admin" || ply.team == "x Yellow")
                                    lastperson = false;

                                if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                                    TeamList.Remove(ply.team);

                                ply.team = "";
                                ply.TSPlayer.SetTeam(0);
                            }
                            #endregion
                            args.Player.SendMessage("You have ceated the public team: " + jointeam, Color.MediumSeaGreen);
                            TeamList.Add(jointeam, joinpassword);
                            args.Player.SetTeam(0);
                            ply.team = jointeam;

                        }
                        else if (joinpassword == "" && !create)
                        {
                            args.Player.SendMessage("You do not have permission to create public teams!", Color.IndianRed);
                            return;
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("You do not have permission to create teams, type \"/team list\" for a list of teams!", Color.IndianRed);
                        return;
                    }
                    #endregion
                }
                #endregion
            }
            else if (subcmd == "leave")
            {
                #region Leave Team
                if (ply.team == "" || ply.team == "x Yellow")
                {
                    args.Player.SendMessage("You cannot leave a team if you are not in one!", Color.IndianRed);
                    return;
                }

                bool lastperson = true;
                foreach (TPlayer tply in TPlayers)
                {
                    if (tply.team == ply.team)
                    {
                        try
                        {
                            NetMessage.SendData((int)PacketTypes.PlayerTeam, ply.Index, -1, "", tply.Index);
                        }
                        catch (Exception) { }
                        if (tply != ply && tply.team != "x Yellow")
                        {
                            tply.SendMessage(args.Player.Name + " has left the team.", Color.GreenYellow);
                            lastperson = false;
                        }
                    }
                }

                if (ply.team == "admin" || ply.team == "x Yellow")
                    lastperson = false;

                if (lastperson && (!TConfig.UsingPerma || (TConfig.UsingPerma && !TConfig.PermaTeams.Contains(ply.team))))
                    TeamList.Remove(ply.team);

                ply.team = "";
                ply.TSPlayer.SetTeam(0);
                args.Player.SendMessage("You are no longer in a team!", Color.MediumSeaGreen);
                #endregion
            }
            else if (subcmd == "list")
            {
                #region List teams
                List<string> publiclist = new List<string>();
                List<string> privatelist = new List<string>();
                foreach (KeyValuePair<string, string> pair in TeamList)
                {
                    if (pair.Value == "")
                        publiclist.Add(pair.Key);

                    privatelist.Add(pair.Key);
                }

                if (TConfig.UsingPerma)
                {
                    publiclist = TConfig.PermaTeams;
                }
                #region ListBuilder
                if (admin)
                {
                    //How many warps per page
                    const int pagelimit = 9;
                    //How many warps per line
                    const int perline = 3;
                    //Pages start at 0 but are displayed and parsed at 1
                    int page = 0;

                    if (args.Parameters.Count > 1)
                    {
                        if (!int.TryParse(args.Parameters[1], out page) || page < 1)
                        {
                            args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
                            return;
                        }
                        page--; //Substract 1 as pages are parsed starting at 1 and not 0
                    }

                    //Check if they are trying to access a page that doesn't exist.
                    int pagecount = privatelist.Count / pagelimit;
                    if (page > pagecount)
                    {
                        args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
                        return;
                    }

                    //Display the current page and the number of pages.
                    args.Player.SendMessage(string.Format("Public and Private teams ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

                    //Add up to pagelimit names to a list
                    var nameslist = new List<string>();
                    for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < privatelist.Count; i++)
                    {
                        nameslist.Add(privatelist[i]);
                    }

                    //convert the list to an array for joining
                    var names = nameslist.ToArray();
                    for (int i = 0; i < names.Length; i += perline)
                    {
                        args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
                    }

                    if (page < pagecount)
                    {
                        args.Player.SendMessage(string.Format("Type /team list {0} for more warps.", (page + 2)), Color.Yellow);
                    }
                }
                else
                {
                    //How many warps per page
                    const int pagelimit = 9;
                    //How many warps per line
                    const int perline = 3;
                    //Pages start at 0 but are displayed and parsed at 1
                    int page = 0;

                    if (args.Parameters.Count > 1)
                    {
                        if (!int.TryParse(args.Parameters[1], out page) || page < 1)
                        {
                            args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
                            return;
                        }
                        page--; //Substract 1 as pages are parsed starting at 1 and not 0
                    }

                    //Check if they are trying to access a page that doesn't exist.
                    int pagecount = publiclist.Count / pagelimit;
                    if (page > pagecount)
                    {
                        args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
                        return;
                    }

                    //Display the current page and the number of pages.
                    args.Player.SendMessage(string.Format("Public teams ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

                    //Add up to pagelimit names to a list
                    var nameslist = new List<string>();
                    for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < publiclist.Count; i++)
                    {
                        nameslist.Add(publiclist[i]);
                    }

                    //convert the list to an array for joining
                    var names = nameslist.ToArray();
                    for (int i = 0; i < names.Length; i += perline)
                    {
                        args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
                    }

                    if (page < pagecount)
                    {
                        args.Player.SendMessage(string.Format("Type /team list {0} for more warps.", (page + 2)), Color.Yellow);
                    }
                }
                #endregion
                #endregion
            }
            else if (subcmd == "remove")
            {
                #region ADMIN  Remove
                if (!admin)
                {
                    args.Player.SendMessage("Usage: /team <join/leave/list>", Color.IndianRed);
                    return;
                }
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendMessage("Usage: /team remove <team name>", Color.IndianRed);
                    return;
                }
                string removeteam = args.Parameters[1];
                foreach (TPlayer rply in TPlayers)
                {
                    if (rply.team == removeteam)
                    {
                        foreach (TPlayer remply in TPlayers)
                        {
                            if (remply.team == removeteam)
                            {
                                try
                                {
                                    NetMessage.SendData((int)PacketTypes.PlayerTeam, rply.Index, -1, "", remply.Index);
                                }
                                catch (Exception) { }
                            }
                        }
                        rply.SendMessage("The team " + removeteam + " has been removed!", Color.GreenYellow);
                    }
                }
                TeamList.Remove(removeteam);
                args.Player.SendMessage("Sucessfully removed the team: " + removeteam, Color.MediumSeaGreen);
                #endregion
            }
            else if (subcmd == "lock")
            {
                #region ADMIN lock
                if (!admin)
                {
                    args.Player.SendMessage("Usage: /team <join/leave/list>", Color.IndianRed);
                    return;
                }
                if (args.Parameters.Count < 2)
                {
                    if (locked)
                    {
                        locked = false;
                        args.Player.SendMessage("Teams are now unlocked!", Color.MediumSeaGreen);
                    }
                    else
                    {
                        locked = true;
                        args.Player.SendMessage("Teams are now locked (no one can join or leave teams!)", Color.MediumSeaGreen);
                    }
                }
                else
                {
                    string boolean = args.Parameters[1];
                    if (boolean == "true" || boolean == "enable")
                    {
                        locked = true;
                        args.Player.SendMessage("Teams are now locked (no one can join or leave teams!)", Color.MediumSeaGreen);
                    }
                    else if (boolean == "false" || boolean == "disable")
                    {
                        locked = false;
                        args.Player.SendMessage("Teams are now unlocked!", Color.MediumSeaGreen);
                    }
                }
                #endregion
            }
            else if (subcmd == "reload")
            {
                #region ADMIN reload
                if (!admin)
                {
                    args.Player.SendMessage("Usage: /team <join/leave/list>", Color.IndianRed);
                    return;
                }
                TConfig.ReloadConfig(args);
                #endregion
            }
            else
            {
                args.Player.SendMessage("Usage: /team <join/leave/list>", Color.IndianRed);
                return;
            }
        }

        public static void teamchat(CommandArgs args)
        {
            var sply = GetTPlayerByName(args.Player.Name);
            if (sply.team == "")
            {
                args.Player.SendMessage("You are not in a Team!", 255, 240, 20);
                return;
            }
            if (args.Parameters.Count == 0)
            {
                if (sply.team == "x Yellow")
                    args.Player.SendMessage("Invalid syntax! Proper syntax: /p <team chat text>", Color.Red);
                else
                    args.Player.SendMessage("Usage: /tc <team chat text>", Color.Red);
                return;
            }

            string msg = string.Format("<{0}> {1}", args.Player.Name, String.Join(" ", args.Parameters));
            foreach (TPlayer ply in TPlayers)
            {
                if (ply != null && ply.TSPlayer.Active && ply.team == sply.team)
                {
                    if (ply.team == "x Yellow")
                        ply.SendMessage(msg, Main.teamColor[4]);
                    else
                        ply.SendMessage(msg, Color.YellowGreen);
                }
            }
        }
    }
}
