﻿using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using Torch.Session;
using Sandbox.Common;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Groups;
using System.Collections.Concurrent;
using VRage.Game;
using Sandbox.ModAPI;
using Sandbox.Game.GameSystems;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Torch.Managers;
using Torch.API.Plugins;
using Torch.API.Managers;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Definitions;
using System.Collections;
using Torch.Managers.ChatManager;
using Torch.API.Session;
using Sandbox.Game.Multiplayer;

namespace CrunchUtilities
{
    public class Commands : CommandModule
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private Vector3 defaultColour = new Vector3(50, 168, 168);
        [Command("crunch reload", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig()
        {
            CrunchUtilitiesPlugin.LoadConfig();
            Context.Respond("Reloaded config");
        }

        [Command("crunch config", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig(string option, string value)
        {
            switch (option)
            {
                case "playermakeship":
                    CrunchUtilitiesPlugin.file.PlayerMakeShip = Boolean.TryParse(value, out bool result);
                    break;
                case "playerfixme":
                    CrunchUtilitiesPlugin.file.PlayerFixMe = Boolean.TryParse(value, out bool result2);
                    break;
                case "deletestone":
                    CrunchUtilitiesPlugin.file.DeleteStone = Boolean.TryParse(value, out bool result3);
                    break;
                case "withdraw":
                    CrunchUtilitiesPlugin.file.Withdraw = Boolean.TryParse(value, out bool result4);
                    break;
                case "deposit":
                    CrunchUtilitiesPlugin.file.Deposit = Boolean.TryParse(value, out bool result5);
                    break;
                case "factionsharedeposit":
                    CrunchUtilitiesPlugin.file.FactionShareDeposit = Boolean.TryParse(value, out bool result6);
                    break;
                case "identityupdate":
                    CrunchUtilitiesPlugin.file.IdentityUpdate = Boolean.TryParse(value, out bool result7);
                    break;
                case "cooldowninseconds":
                    CrunchUtilitiesPlugin.file.CooldownInSeconds = int.Parse(value);
                    break;
            }
           
        }

        [Command("admin makeship", "Admin command, Turn a station and connected grids into a ship")]
        [Permission(MyPromoteLevel.Admin)]
        public void MakeShip()
        {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
            foreach (var item in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;

                    if (grid.IsStatic)
                    {
                        Action m_convertToShipResult = null;
                        grid.RequestConversionToShip(m_convertToShipResult);
                        Context.Respond("Converting to ship " + grid.DisplayName);
                    }
                }
            }
        }
        [Command("removebody", "Removes every body with this display name")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteBody(string name)
        {
            List<MyEntity> temp = new List<MyEntity>();
            foreach (MyEntity entity in MyEntities.GetEntities())
            {
                if (entity.DisplayName != null && entity.DisplayName.Equals(name))
                {
                    temp.Add(entity);
                }
            }

            foreach (MyEntity id in temp)
            {
                if (id is IMyCharacter)
                {
                    //MyEntities.Remove(id);
                    Context.Respond("Removing " + id.EntityId);
                    IMyCharacter character = id as IMyCharacter;
                    character.Kill();
                    character.Delete();
                    id.Close();
                }
            }

        }
        [Command("admin makestation", "Admin command, Turn a station and connected grids into a ship")]
        [Permission(MyPromoteLevel.Admin)]
        public void MakeStation()
        {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
            foreach (var item in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;

                    if (!grid.IsStatic)
                        grid.OnConvertedToStationRequest();
                    Context.Respond("Converting to station " + grid.DisplayName);
                }
            }
        }
        private static CurrentCooldown CreateNewCooldown(Dictionary<long, CurrentCooldown> cooldownMap, long playerId, long cooldown)
        {

            var currentCooldown = new CurrentCooldown(cooldown);

            if (cooldownMap.ContainsKey(playerId))
                cooldownMap[playerId] = currentCooldown;
            else
                cooldownMap.Add(playerId, currentCooldown);

            return currentCooldown;
        }

        [Command("stone", "Delete all stone in a grid")]
        [Permission(MyPromoteLevel.None)]
        public void DeleteStone()
        {
            if (CrunchUtilitiesPlugin.file.DeleteStone)
            {
                CrunchUtilitiesPlugin plugin = (CrunchUtilitiesPlugin)Context.Plugin;
                var currentCooldownMap = plugin.CurrentCooldownMap;
                if (currentCooldownMap.TryGetValue(Context.Player.IdentityId, out CurrentCooldown currentCooldown))
                {

                    long remainingSeconds = currentCooldown.GetRemainingSeconds(null);

                    if (remainingSeconds > 0)
                    {

                        CrunchUtilitiesPlugin.Log.Info("Cooldown for Player " + Context.Player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                        Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                        return;
                    }
                    currentCooldown = CreateNewCooldown(currentCooldownMap, Context.Player.IdentityId, plugin.Cooldown);
                    currentCooldown.StartCooldown(null);
                }
                else
                {
                    currentCooldown = CreateNewCooldown(currentCooldownMap, Context.Player.IdentityId, plugin.Cooldown);
                    currentCooldown.StartCooldown(null);
                }
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
                foreach (var item in gridWithSubGrids)
                {
                    foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                    {
                        //     MyObjectBuilder_PhysicalObject stone = new MyObjectBuilder_PhysicalObject("MyObjectBuilder_Ore/Stone");
                        MyCubeGrid grid = groupNodes.NodeData;
                        var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                        var blockList = new List<Sandbox.ModAPI.IMyTerminalBlock>();
                        gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(blockList);
                        if (!FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                        {
                            Context.Respond("You dont own this");
                            continue;
                        }
                        else
                        {
                            foreach (var block in blockList)
                            {

                                //MyVisualScriptLogicProvider.SendChatMessage("blocks blocklist");
                                if (block != null && block.HasInventory)
                                {
                                    var items = block.GetInventory().GetItems();
                                    for (int i = 0; i < items.Count; i++)
                                    {

                                        if (items[i].Content.SubtypeId.ToString().Contains("Stone") && items[i].Content.TypeId.ToString().Contains("Ore"))
                                        {
                                            block.GetInventory().RemoveItems(items[i].ItemId);
                                        }
                                    }
                                }
                            }
                        }
                        Context.Respond("Deleted the stone?");
                    }
                }
            }
            else
            {
                Context.Respond("stone not enabled");
            }

        }
        [Command("convert", "Player command, Turn a ship and connected grids into a station")]
        [Permission(MyPromoteLevel.None)]
        public void MakeStationPlayer()
        {
            if (CrunchUtilitiesPlugin.file.PlayerMakeShip)
            {

                if (MyGravityProviderSystem.IsPositionInNaturalGravity(Context.Player.GetPosition()))
                {

                    Context.Respond("You cannot use this command in natural gravity!");
                    return;
                }

                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
                if (gridWithSubGrids.Count > 0)
                {

                    foreach (var item in gridWithSubGrids)
                    {
                        bool isStatic = false;
                        bool isDynamic = false;
                        foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                        {

                            MyCubeGrid grid = groupNodes.NodeData;

                            if (!FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                            {

                                continue;
                            }
                            else
                            {
                                //fix this lmao, one grid static, others dynamic it turns the dynamics to static and static to dynamic

                                if (grid.IsStatic)
                                {
                                    if (isDynamic)
                                    {
                                        break;
                                    }
                                    Action m_convertToShipResult = null;
                                    grid.RequestConversionToShip(m_convertToShipResult);
                                    Context.Respond("Converting to ship " + grid.DisplayName);
                                    isStatic = true;
                                }
                                else
                                {
                                    if (isStatic)
                                    {
                                        break;
                                    }
                                    try
                                    {
                                        isDynamic = true;
                                        grid.Physics.ClearSpeed();
                                        grid.OnConvertedToStationRequest();

                                        Context.Respond("Converting to station IF grid is not moving." + grid.DisplayName);
                                    }
                                    catch (Exception)
                                    {
                                        Context.Respond("Grid cannot be moving!");

                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Context.Respond("Cant find a grid");
                }
            }
            else
            {
                Context.Respond("PlayerMakeShip not enabled");
            }
        }

        [Command("rename", "Player command, Rename a ship")]
        [Permission(MyPromoteLevel.None)]
        public void RenameGrid(string gridname, string newname)
        {

            bool changed = false;
            if (CrunchUtilitiesPlugin.file.PlayerMakeShip)
            {
                if (Context.Player == null)
                {
                    Context.Respond("Currently only a player can use this command :(");
                    return;
                }
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindGridGroup(gridname);
                foreach (var item in gridWithSubGrids)
                {
                    foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                    {
                        MyCubeGrid grid = groupNodes.NodeData;
                        if (FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true) && grid.DisplayName.Equals(gridname))
                        {
                            Context.Respond("Renaming " + grid.DisplayName + ". You may need to relog to see changes.");
                            grid.ChangeDisplayNameRequest(newname);


                            changed = true;
                            return;
                        }
                    }
                }
                if (!changed)
                {
                    Context.Respond("Couldnt find that grid, are you sure its owned by you or faction?");
                }
            }
            else
            {
                Context.Respond("PlayerMakeShip not enabled");
            }
        }

        [Command("fixme", "Murder a player then respawn them at their current location")]
        [Permission(MyPromoteLevel.None)]
        public void FixPlayer()
        {
            if (CrunchUtilitiesPlugin.file.PlayerFixMe)
            {
                IMyPlayer player = Context.Player;
                long playerId;
                if (player == null)
                {
                    Context.Respond("Console cant do this");
                    return;
                }
                else
                {
                    playerId = player.IdentityId;
                }
                try
                {
                    Context.Respond("You should be fixed after respawning");
                    player.Character.Kill();
                    player.Character.Delete();
                    MyMultiplayer.Static.DisconnectClient(player.SteamUserId);
                }
                catch (Exception)
                {
                    Context.Respond("You are really broken, this might help");
                    player.Character.Kill();
                    player.Character.Delete();
                    MyMultiplayer.Static.DisconnectClient(player.SteamUserId);
                    return;
                }
            }
            else
            {
                Context.Respond("PlayerFixMe not enabled");
            }
        }
        [Command("getsteamid", "Get a specific identities steam name")]
        [Permission(MyPromoteLevel.None)]
        public void getSTEAMID(string target)
        {
            bool console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            Dictionary<String, String> badNames = new Dictionary<string, string>();
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {

                MyIdentity identity = CrunchUtilitiesPlugin.GetIdentityByNameOrId(player.Id.SteamId.ToString());
                if (!identity.DisplayName.Equals(target))
                {
                    string name = MyMultiplayer.Static.GetMemberName(player.Id.SteamId);
                    badNames.Add(name + " : " + identity.DisplayName, player.Id.SteamId.ToString());
                }
            }
            if (badNames.Count == 0)
            {
                if (console)
                {
                    Context.Respond("No player with that name");
                    return;
                }
                SendMessage("[C]", "No player with that name", Color.Green, (long)Context.Player.SteamUserId);
            }
            foreach (KeyValuePair<string, string> pair in badNames)
            {
                if (console)
                {
                    Context.Respond("Names: " + pair.Key + " || ID: " + pair.Value);
                    return;
                }
                SendMessage("[C]", "Names: " + pair.Key + " || ID: " + pair.Value, Color.Green, (long)Context.Player.SteamUserId);
            }
        }

        [Command("listids", "Lists players steam IDs")]
        [Permission(MyPromoteLevel.None)]
        public void listSteamIDs()
        {
            bool console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            Dictionary<String, String> badNames = new Dictionary<string, string>();
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                string name = MyMultiplayer.Static.GetMemberName(player.Id.SteamId);
                MyIdentity identity = CrunchUtilitiesPlugin.GetIdentityByNameOrId(player.Id.SteamId.ToString());
                if (player.DisplayName.Equals(identity.DisplayName)){
                    badNames.Add(name, player.Id.SteamId.ToString());
                }
                else
                {
                    badNames.Add(name + " : " + identity.DisplayName, player.Id.SteamId.ToString());
                }
            }
            if (badNames.Count == 0)
            {
                if (console)
                {
                    Context.Respond("No players online");
                    return;
                }
                SendMessage("[C]", "No players online", Color.Green, (long)Context.Player.SteamUserId);
            }
            foreach (KeyValuePair<string, string> pair in badNames)
            {
                if (console)
                {
                    Context.Respond("Names: " + pair.Key + " || ID: " + pair.Value);
                }
                else
                {
                    SendMessage("[C]", "Names: " + pair.Key + " || ID: " + pair.Value, Color.Green, (long)Context.Player.SteamUserId);
                }
            }
        }

        [Command("listnames", "Lists players names if they dont match steam names")]
        [Permission(MyPromoteLevel.None)]
        public void listNames()
        {
            bool console = false;
            if (Context.Player == null)
            {
                console = true;
            }
            Dictionary<String, String> badNames = new Dictionary<string, string>();
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                string name = MyMultiplayer.Static.GetMemberName(player.Id.SteamId);
                MyIdentity identity = CrunchUtilitiesPlugin.GetIdentityByNameOrId(player.Id.SteamId.ToString());
                if (!identity.DisplayName.Equals(name))
                {
                    badNames.Add(name, identity.DisplayName);
                }
            }
            if (badNames.Count == 0)
            {
                if (console)
                {
                    Context.Respond("No players with mismatching names :D");
                    return;
                }
                else
                {
                    SendMessage("[C]", "No players with mismatching names :D", Color.Green, (long)Context.Player.SteamUserId);
                    return;
                }
            }
            foreach (KeyValuePair<string, string> pair in badNames)
            {
                string temp;
                if (console)
                {
                    Context.Respond("Steam: " + pair.Key + " || Identity: " + pair.Value);
                }
                else
                {
                    SendMessage("[C]", "Steam: " + pair.Key + " || Identity: " + pair.Value, Color.Green, (long)Context.Player.SteamUserId);
                }
            }
        }
        [Command("updatename", "updates identity names")]
        [Permission(MyPromoteLevel.Admin)]
        public void UpdateIdentities(String playerNameOrId, String newName)
        {


            MyIdentity identity = CrunchUtilitiesPlugin.GetIdentityByNameOrId(playerNameOrId);

            if (identity == null)
            {
                Context.Respond("Error cant find that guy");
                return;
            }
            else
            {
                identity.SetDisplayName(newName);
                Context.Respond("New Identity Name : " + identity.DisplayName);
            }

        }






        [Command("eco", "list commands")]
        [Permission(MyPromoteLevel.None)]
        public void EcoHelp()
        {
            Context.Respond("\n"
            + "!eco balance <player/faction> <name/tag> - Shows a players balance \n"
            + "!eco give <player/faction> <name/tag> amount \n"
            + "!eco take <player/faction> <name/tag> amount \n"
            + "!eco pay <player/faction> <name/tag> amount \n"
            + "!eco deposit - Deposits credits in the grid you are looking at \n"
            + "!eco withdraw <amount> - Withdraws credits into the grid you are looking at");

        }

        [Command("eco balance", "See a players balance")]
        [Permission(MyPromoteLevel.Admin)]
        public void CheckMoneysPlayer(string type, string target)
        {
            type = type.ToLower();
            switch (type)
            {
                case "player":
                    //Context.Respond("Error Player not online");
                    IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(target);
                    if (id == null)
                    {
                        Context.Respond("Cant find that player.");
                        return;
                    }
                    Context.Respond(id.DisplayName + " Player Balance : " + String.Format("{0:n0}", EconUtils.getBalance(id.IdentityId)));


                    break;
                case "faction":
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(target);
                    if (fac == null)
                    {
                        Context.Respond("Cant find that faction");
                        return;
                    }

                    Context.Respond(fac.Name + " Faction Balance : " + String.Format("{0:n0}", EconUtils.getBalance(fac.FactionId)));

                    break;

                default:
                    Context.Respond("Incorrect usage, example - !eco balance player PlayerName or !eco balance faction tag");
                    break;


            }
        }
        [Command("eco withdrawall", "Withdraw all moneys, buggy as fuck, try not to use this")]
        [Permission(MyPromoteLevel.Admin)]
        public void PlayerWithdrawAll()
        {
            Int64 balance;
            Int64 withdrew = 0;
            IMyPlayer player = Context.Player;
            balance = EconUtils.getBalance(player.IdentityId);

            if (player == null)
            {
                Context.Respond("Console cant withdraw money.....");
            }
            MyCubeBlock container = null;
            VRage.Game.ModAPI.IMyInventory invent = null;
            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(player.Character);
            if (gridWithSubGrids.Count < 1)
            {
                Context.Respond("Couldnt find a grid");
                return;
            }

            MyItemType itemType = new MyInventoryItemFilter("MyObjectBuilder_PhysicalObject/SpaceCredit").ItemType;
            foreach (var item in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;

                    if (!FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                        continue;
                    else
                    {
                        foreach (VRage.Game.ModAPI.IMySlimBlock block in grid.GetBlocks())
                        {
                            if (block != null && block.BlockDefinition.Id.SubtypeName.Contains("Container"))
                            {
                                Int64 min = Int64.Parse(block.FatBlock.GetInventory().CurrentVolume.RawValue.ToString());
                                Int64 max = Int64.Parse(block.FatBlock.GetInventory().MaxVolume.RawValue.ToString());
                                Int64 difference = (max - min) * 1000;
                                if (balance >= Int32.MaxValue)
                                {
                                    Int64 newBalance = balance;
                                    bool canAdd = true;
                                    while (canAdd)
                                    {
                                        if (newBalance >= Int32.MaxValue)
                                        {
                                            newBalance -= Int32.MaxValue;
                                            if ((block.FatBlock.GetInventory().CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(Int32.MaxValue.ToString()), itemType)))
                                            {
                                                container = block.FatBlock as MyCubeBlock;
                                                invent = container.GetInventory();
                                                invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(Int32.MaxValue.ToString()), new MyObjectBuilder_PhysicalObject() { SubtypeName = "SpaceCredit" });
                                                EconUtils.takeMoney(player.IdentityId, Int32.MaxValue);
                                                withdrew += Int32.MaxValue;
                                                Context.Respond("Added the credits to " + container.DisplayNameText);
                                            }
                                            else
                                            {
                                                canAdd = false;
                                            }
                                        }
                                        else
                                        {
                                            if ((block.FatBlock.GetInventory().CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(newBalance.ToString()), itemType)))
                                            {
                                                container = block.FatBlock as MyCubeBlock;
                                                invent = container.GetInventory();
                                                invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(newBalance.ToString()), new MyObjectBuilder_PhysicalObject() { SubtypeName = "SpaceCredit" });
                                                EconUtils.takeMoney(player.IdentityId, newBalance);
                                                withdrew += newBalance;
                                                Context.Respond("Added the credits to " + container.DisplayNameText);
                                            }
                                            else
                                            {
                                                canAdd = false;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (block.FatBlock.GetInventory().CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(balance.ToString()), itemType))
                                    {

                                        container = block.FatBlock as MyCubeBlock;
                                        invent = container.GetInventory();

                                        invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(balance.ToString()), new MyObjectBuilder_PhysicalObject() { SubtypeName = "SpaceCredit" });
                                        EconUtils.takeMoney(player.IdentityId, balance);

                                        withdrew += balance;
                                        balance = 0;
                                        Context.Respond("Added the credits to " + container.DisplayNameText);

                                        Context.Respond("Withdrew : " + String.Format("{0:n0}", withdrew));
                                        return;
                                    }
                                }
                            }
                        }
                    }

                }
            }
            Context.Respond("Withdrew : " + String.Format("{0:n0}", withdrew));
        }
        [Command("eco deposit", "Deposit moneys")]
        [Permission(MyPromoteLevel.None)]
        public void PlayerDeposit()
        {
            if (CrunchUtilitiesPlugin.file.Deposit)
            {
                IMyPlayer player = Context.Player;
                Int64 deposited = 0;
                if (player == null)
                {
                    Context.Respond("Console cant withdraw money.....");
                }

                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(player.Character);
                if (gridWithSubGrids.Count < 1)
                {
                    Context.Respond("Couldnt find a grid");
                    return;
                }

                foreach (var item in gridWithSubGrids)
                {
                    foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                    {
                        MyCubeGrid grid = groupNodes.NodeData;
                        if (!FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                            continue;
                        else
                        {
                            foreach (VRage.Game.ModAPI.IMySlimBlock block in grid.GetBlocks())
                            {
                                if (block.FatBlock != null && block.FatBlock.HasInventory)
                                {
                                    bool owned = false;
                                    switch (block.FatBlock.GetUserRelationToOwner(this.Context.Player.IdentityId))
                                    {
                                        case MyRelationsBetweenPlayerAndBlock.Owner:
                                            owned = true;
                                            break;
                                        case MyRelationsBetweenPlayerAndBlock.FactionShare:
                                            if (CrunchUtilitiesPlugin.file.FactionShareDeposit)
                                            {
                                                owned = true;
                                            }
                                            break;
                                        case MyRelationsBetweenPlayerAndBlock.Neutral:
                                            owned = true;
                                            break;
                                        case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                                            owned = false;
                                            break;
                                        case MyRelationsBetweenPlayerAndBlock.Enemies:
                                            owned = false;
                                            break;
                                    }
                                    List<VRage.Game.ModAPI.IMyInventoryItem> itemList2 = new List<VRage.Game.ModAPI.IMyInventoryItem>();
                                    itemList2 = block.FatBlock.GetInventory().GetItems();
                                    int i = 0;
                                    if (owned)
                                    {

                                        for (i = 0; i < itemList2.Count; i++)
                                        {
                                            string itemId = itemList2[i].Content.SubtypeId.ToString();
                                            if (itemId.Contains("SpaceCredit"))
                                            {
                                                Int64 amount = Int64.Parse(itemList2[i].Amount.ToString());
                                                if (amount >= Int32.MaxValue)
                                                {
                                                    bool hasCredits = true;
                                                    while (hasCredits)
                                                    {
                                                        deposited += amount;

                                                        block.FatBlock.GetInventory().RemoveItemAmount(itemList2[i], itemList2[i].Amount);
                                                        //Context.Respond("Stack exceeds 2.147 billion, split the stack!");
                                                        if (!block.FatBlock.GetInventory().GetItems().Contains(itemList2[i]))
                                                        {
                                                            hasCredits = false;
                                                        }
                                                    }
                                                    EconUtils.addMoney(player.IdentityId, amount);

                                                }
                                                else
                                                {
                                                    deposited += itemList2[i].Amount.ToIntSafe();
                                                    EconUtils.addMoney(player.IdentityId, itemList2[i].Amount.ToIntSafe());
                                                    block.FatBlock.GetInventory().RemoveItemAmount(itemList2[i], itemList2[i].Amount);
                                                }

                                            }

                                        }
                                    }
                                    else
                                    {
                                        Context.Respond("You dont own this container.");
                                    }
                                }
                            }

                        }
                    }

                }
                Context.Respond("Deposited : " + String.Format("{0:n0}", deposited));
            }
            else
            {
                Context.Respond("Deposit not enabled.");
            }
        }


        [Command("eco withdraw", "Withdraw moneys")]
        [Permission(MyPromoteLevel.None)]
        public void PlayerWithdraw(Int64 amount)
        {
            if (CrunchUtilitiesPlugin.file.Withdraw)
            {
                Int64 balance;
                if (amount >= Int32.MaxValue)
                {
                    Context.Respond("Keen code doesnt allow stacks over 2.147 billion, try again with a smaller number");
                    return;
                }
                IMyPlayer player = Context.Player;
                balance = EconUtils.getBalance(player.IdentityId);
                if (balance < amount)
                {
                    Context.Respond("You dont have that much money.");
                    return;
                }
                if (amount < 0 || amount == 0)
                {
                    Context.Respond("No.");
                    return;
                }
                if (player == null)
                {
                    Context.Respond("Console cant withdraw money.....");
                    return;
                }
                MyCubeBlock container = null;
                VRage.Game.ModAPI.IMyInventory invent = null;
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(player.Character);
                if (gridWithSubGrids.Count < 1)
                {
                    Context.Respond("Couldnt find a grid");
                    return;
                }
                MyItemType itemType = new MyInventoryItemFilter("MyObjectBuilder_PhysicalObject/SpaceCredit").ItemType;
                foreach (var item in gridWithSubGrids)
                {
                    foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                    {
                        MyCubeGrid grid = groupNodes.NodeData;
                        if (!FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                            continue;
                        else
                        {

                            foreach (VRage.Game.ModAPI.IMySlimBlock block in grid.GetBlocks())
                            {
                                if (block != null && block.BlockDefinition.Id.SubtypeName.Contains("Container"))
                                {
                                    if (block.FatBlock.GetInventory().CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), itemType))
                                    {

                                        switch (block.FatBlock.GetUserRelationToOwner(this.Context.Player.IdentityId))
                                        {
                                            case MyRelationsBetweenPlayerAndBlock.Owner:
                                                container = block.FatBlock as MyCubeBlock;
                                                invent = container.GetInventory();

                                                break;
                                            case MyRelationsBetweenPlayerAndBlock.FactionShare:
                                                container = block.FatBlock as MyCubeBlock;
                                                invent = container.GetInventory();

                                                break;
                                            case MyRelationsBetweenPlayerAndBlock.Neutral:
                                                container = block.FatBlock as MyCubeBlock;
                                                invent = container.GetInventory();
                                                break;
                                            case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                                                Context.Respond("You dont own this.");
                                                break;
                                            case MyRelationsBetweenPlayerAndBlock.Enemies:
                                                Context.Respond("You dont own this.");
                                                break;
                                        }

                                        break;
                                    }
                                }

                            }
                            if (container == null)
                            {
                                Context.Respond("No container has free space for that many credits.");
                            }
                        }
                    }



                    if (invent != null)
                    {

                        if (invent.CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), itemType))
                        {
                            invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), new MyObjectBuilder_PhysicalObject() { SubtypeName = "SpaceCredit" });
                            EconUtils.takeMoney(player.IdentityId, amount);

                            Context.Respond("Added the credits to " + container.DisplayNameText);
                        }
                        else
                        {
                            Context.Respond("Cant add that many");
                        }
                    }
                }
            }
            else
            {
                Context.Respond("Withdraw not enabled.");
            }
        }



        [Command("giveitem", "Give target player an item")]
        [Permission(MyPromoteLevel.Admin)]
        public void PlayerWithdraw(string PlayerName, string type, string subtypeName, Int64 amount)
        {
            IMyPlayer player = MySession.Static.Players.GetPlayerByName(PlayerName);
            if (player == null)
            {
                Context.Respond("Cant find that player");
            }
            VRage.Game.ModAPI.IMyInventory invent = player.Character.GetInventory();
            switch (type.ToLower())
            {
                //Eventually add some checks to see if the item exists before adding it
                case "ore":
                    MyObjectBuilder_PhysicalObject item = new MyObjectBuilder_Ore() { SubtypeName = subtypeName };
                    MyItemType itemType = new MyInventoryItemFilter("MyObjectBuilder_Ore/" + subtypeName).ItemType;
                    if (invent.CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()),itemType))
                    {
                        invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), item);
                        Context.Respond("Giving " + player.DisplayName + " " + amount + " " + item.SubtypeName);
                    }
                    else
                    {
                        Context.Respond("Error : Inventory doesnt have room");
                    }
             
                    break;
                case "ingot":
                    MyObjectBuilder_PhysicalObject item2 = new MyObjectBuilder_Ingot() { SubtypeName = subtypeName };
                    MyItemType itemType2 = new MyInventoryItemFilter("MyObjectBuilder_Ingot/" + subtypeName).ItemType;
                    if (invent.CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), itemType2))
                    {
                        invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), item2);
                        Context.Respond("Giving " + player.DisplayName + " " + amount + " " + item2.SubtypeName);
                    }
                    else
                    {
                        Context.Respond("Error : Inventory doesnt have room");
                    }
                    break;
                case "component":
                    MyObjectBuilder_PhysicalObject item3 = new MyObjectBuilder_Component() { SubtypeName = subtypeName };
                    MyItemType itemType3 = new MyInventoryItemFilter("MyObjectBuilder_Component/" + subtypeName).ItemType;
                    if (invent.CanItemsBeAdded(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), itemType3))
                    {
                        invent.AddItems(VRage.MyFixedPoint.DeserializeStringSafe(amount.ToString()), item3);
                        Context.Respond("Giving " + player.DisplayName + " " + amount + " " + item3.SubtypeName);
                    }
                    else
                    {
                        Context.Respond("Error : Inventory doesnt have room");
                    }
                    break;
            }

        }


        //this command is broken af, might fix it eventually
        [Command("fac promote", "Broken command")]
        [Permission(MyPromoteLevel.Admin)]
        public void FactionPromoteFounder(string playerName)
        {
            Context.Respond("Doesnt work currently");
            return;
            if (Context.Player == null)
            {
                ; Context.Respond("Player command");
                return;
            }
            IMyFaction fac = MySession.Static.Factions.TryGetPlayerFaction(Context.Player.IdentityId);
            if (fac != null)
            {
                MyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(playerName);
                VRage.Collections.DictionaryReader<long, MyFactionMember> members = fac.Members;
                if (id == null)
                {
                    Context.Respond("Couldnt find that player");
                    return;
                }
                MyFactionMember currentFounder;
                MyFactionMember newFounder;
                bool foundPlayer = false;
                if (fac.IsFounder(Context.Player.IdentityId))
                {
                    foreach (VRage.Game.MyFactionMember key in members.Values)
                    {

                        if (id.IdentityId.Equals(key.PlayerId))
                        {
                            foundPlayer = true;
                            newFounder = key;
                        }
                        if (id.IdentityId.Equals(Context.Player.IdentityId))
                        {
                            currentFounder = key;
                        }
                    }

                }
                else
                {
                    Context.Respond("You need to be the founder to use this command.");
                }
            }
            else
            {
                Context.Respond("Error, no faction");
            }
        }

        [Command("broadcast", "Send a message in a noticable colour, false parameter will not show up in discord")]
        [Permission(MyPromoteLevel.Admin)]
        public void SendThisMessage(string message, string author = "Broadcast", int r = 50, int g = 168, int b = 168, Boolean global = true)
        {
            String context;
            if (global)
            {
                Color col = new Color(r, g, b);

                SendMessage(author, message, col, 0L);
                if (r == 50 && g == 168 && b == 168)
                {
                    Context.Respond("Sending to global");
                }
            }
            else
            {
                Context.Respond("Sending to players;");

                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {
                    MyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(player.Identity.IdentityId.ToString());
                    Color col = new Color(r, g, b);
                    SendMessage(author, message, col, (long)player.Id.SteamId);
                }
            }
        }

        [Command("eco take", "Admin command to take money")]
        [Permission(MyPromoteLevel.Admin)]
        public void TakeMoney(string type, string recipient, string inputAmount)
        {
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing into number");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Amount must be positive.");
                return;
            }
            type = type.ToLower();
            switch (type)
            {
                case "player":
                    //Context.Respond("Error Player not online");
                    IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(recipient);
                    if (id == null)
                    {
                        Context.Respond("Cant find that player.");
                        return;
                    }
                    if (EconUtils.getBalance(id.IdentityId) >= amount)
                    {
                        Context.Respond(id.DisplayName + " Balance Before Change : " + String.Format("{0:n0}", EconUtils.getBalance(id.IdentityId)));

                        EconUtils.takeMoney(id.IdentityId, amount);

                        Context.Respond(id.DisplayName + " Balance After Change : " + String.Format("{0:n0}", EconUtils.getBalance(id.IdentityId)));
                    }
                    else
                    {
                        Context.Respond("They cant afford that.");
                        Context.Respond(id.DisplayName + " Current Balance : " + String.Format("{0:n0}", EconUtils.getBalance(id.IdentityId)));
                    }
                    break;
                case "faction":
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(recipient);
                    if (fac == null)
                    {
                        Context.Respond("Cant find that faction");
                        return;
                    }
                    if (EconUtils.getBalance(fac.FactionId) >= amount)
                    {
                        Context.Respond(fac.Name + " FACTION Balance Before Change : " + String.Format("{0:n0}", EconUtils.getBalance(fac.FactionId)));
                        EconUtils.takeMoney(fac.FactionId, amount);
                        Context.Respond(fac.Name + " FACTION Balance After Change : " + String.Format("{0:n0}", EconUtils.getBalance(fac.FactionId)));
                    }
                    else
                    {
                        Context.Respond("They cant afford that.");
                        Context.Respond(fac.Name + " Current Balance : " + String.Format("{0:n0}", EconUtils.getBalance(fac.FactionId)));
                    }
                    break;

                default:
                    Context.Respond("Incorrect usage, example - !eco take player PlayerName amount or !eco take faction tag amount");
                    break;


            }
        }
        [Command("eco give", "Admin command to give money")]
        [Permission(MyPromoteLevel.Admin)]
        public void GiveMoney(string type, string recipient, string inputAmount)
        {
            Int64 amount;
            inputAmount = inputAmount.Replace(",", "");
            inputAmount = inputAmount.Replace(".", "");
            inputAmount = inputAmount.Replace(" ", "");
            try
            {
                amount = Int64.Parse(inputAmount);
            }
            catch (Exception)
            {
                Context.Respond("Error parsing into number");
                return;
            }
            if (amount < 0 || amount == 0)
            {
                Context.Respond("Amount must be positive.");
                return;
            }
            type = type.ToLower();
            switch (type)
            {
                case "player":
                    //Context.Respond("Error Player not online");
                    IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(recipient);
                    if (id == null)
                    {
                        Context.Respond("Cant find that player.");
                        return;
                    }
                    Context.Respond(id.DisplayName + " Balance Before Change : " + String.Format("{0:n0}", EconUtils.getBalance(id.IdentityId)));

                    EconUtils.addMoney(id.IdentityId, amount);

                    Context.Respond(id.DisplayName + " Balance After Change : " + String.Format("{0:n0}", EconUtils.getBalance(id.IdentityId)));

                    break;
                case "faction":
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(recipient);
                    if (fac == null)
                    {
                        Context.Respond("Cant find that faction");
                        return;
                    }
                    Context.Respond(fac.Name + " FACTION Balance Before Change : " + String.Format("{0:n0}", EconUtils.getBalance(fac.FactionId)));
                    EconUtils.addMoney(fac.FactionId, amount);
                    Context.Respond(fac.Name + " FACTION Balance After Change : " + String.Format("{0:n0}", EconUtils.getBalance(fac.FactionId)));
                    break;

                default:
                    Context.Respond("Incorrect usage, example - !eco give player PlayerName amount or !eco give faction tag amount");
                    break;


            }
        }

        [Command("eco pay", "Transfer money from one player to another")]
        [Permission(MyPromoteLevel.None)]
        public void PayPlayer(string type, string recipient, string inputAmount)
        {
            if (Context.Player == null)
            {
                Context.Respond("Only players can use this command");
                return;
            }
            if (CrunchUtilitiesPlugin.file.PlayerEcoPay)
            {
                Int64 amount;
                inputAmount = inputAmount.Replace(",", "");
                inputAmount = inputAmount.Replace(".", "");
                inputAmount = inputAmount.Replace(" ", "");
                try
                {
                    amount = Int64.Parse(inputAmount);
                }
                catch (Exception)
                {
                    SendMessage("[CrunchEcon]", "Error parsing amount", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                if (amount < 0 || amount == 0)
                {
                    SendMessage("[CrunchEcon]", "Must be a positive number", Color.Red, (long)Context.Player.SteamUserId);
                    return;
                }
                type = type.ToLower();
                switch (type)
                {
                    case "player":
                        //Context.Respond("Error Player not online");
                        IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(recipient);
                       // if (id == null)
                       // {
                        //    
                           // return;
                       // }
                        MyPlayer player = null;
                        bool found = false;
                        foreach (MyPlayer players in MySession.Static.Players.GetOnlinePlayers())
                        {
                            if (found)
                            {
                                break;
                            }
           
                            if (players.Id.SteamId.Equals(recipient))
                            {
                                found = true;
                                player = players;
                                break;
                            }
                            if (players.DisplayName.Equals(recipient))
                            {
                                player = players;
                                found = true;
                                break;
                            }
                            if (id != null)
                            {
                                if (players.Identity.IdentityId == id.IdentityId)
                                {
                                    found = true;
                                    player = players;
                                    break;
                                }
                            }

                        }
                        if (!found)
                        {
                            SendMessage("[CrunchEcon]", "Cant find that player", Color.Red, (long)Context.Player.SteamUserId);
                            return;
                        }
                        if (player == null)
                        {
                            SendMessage("[CrunchEcon]", "Cant pay offline players, pay faction instead.", Color.Red, (long)Context.Player.SteamUserId);
                            return;
                        }
                        if (EconUtils.getBalance(Context.Player.IdentityId) >= amount)
                        {
                            EconUtils.takeMoney(Context.Player.IdentityId, amount);
                            EconUtils.addMoney(id.IdentityId, amount);


                            SendMessage("[CrunchEcon]", Context.Player.DisplayName + " Has sent you : " + String.Format("{0:n0}", amount) + " SC", Color.Cyan, (long)player.Id.SteamId);
                            SendMessage("[CrunchEcon]", "You sent " + id.DisplayName + " : " + String.Format("{0:n0}", amount) + " SC", Color.Cyan, (long)Context.Player.SteamUserId);
                        }
                        else
                        {
                            SendMessage("[CrunchEcon]", "You too poor", Color.Red, (long)Context.Player.SteamUserId);
                        }
                        break;
                    case "faction":
                        IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(recipient);
                        if (fac == null)
                        {
                            SendMessage("[CrunchEcon]", "Cant find that faction", Color.Red, (long)Context.Player.SteamUserId);
                            return;
                        }
                        if (EconUtils.getBalance(Context.Player.IdentityId) >= amount)
                        {
                            //Probablty need to do some reflection/patching shit to add the transfer to the activity log
                            EconUtils.takeMoney(Context.Player.IdentityId, amount);
                            EconUtils.addMoney(fac.FactionId, amount);
                            //I can add to the activity log with this but its not a great idea, it sets the balances to insane values
                            // EconUtils.TransferToFactionAccount(Context.Player.IdentityId, fac.FactionId, amount);
                            List<ulong> temp = new List<ulong>();
                            foreach (MyFactionMember mb in fac.Members.Values)
                            {


                                ulong steamid = MySession.Static.Players.TryGetSteamId(mb.PlayerId);
                                if (temp.Contains(steamid))
                                {
                                    break;
                                }
                                if (steamid == 0)
                                {
                                    break;
                                }
                                SendMessage("[CrunchEcon]", Context.Player.DisplayName + " Has sent : " + String.Format("{0:n0}", amount) + " SC to the faction bank.", Color.DarkGreen, (long)steamid);
                                temp.Add(steamid);

                            }


                            SendMessage("[CrunchEcon]", "You sent " + fac.Name + " : " + String.Format("{0:n0}", amount) + " SC", Color.Cyan, (long)Context.Player.SteamUserId);
                        }
                        else
                        {
                            SendMessage("[CrunchEcon]", "You too poor", Color.Red, (long)Context.Player.SteamUserId);
                        }
                        break;
                    case "steam":
                        //Context.Respond("Error Player not online");
                        IMyIdentity id2 = CrunchUtilitiesPlugin.GetIdentityByNameOrId(recipient);
                        if (id2 == null)
                        {
                            SendMessage("[CrunchEcon]", "Cant find that player", Color.Red, (long)Context.Player.SteamUserId);
                            return;
                        }
                        if (EconUtils.getBalance(Context.Player.IdentityId) >= amount)
                        {
                            EconUtils.takeMoney(Context.Player.IdentityId, amount);
                            EconUtils.addMoney(id2.IdentityId, amount);


                            //SendMessage("[CrunchEcon]", Context.Player.DisplayName + " Has sent you : " + String.Format("{0:n0}", amount) + " SC", Color.Cyan, (long)player.Id.SteamId);
                            SendMessage("[CrunchEcon]", "You sent " + id2.DisplayName + " : " + String.Format("{0:n0}", amount) + " SC", Color.Cyan, (long)Context.Player.SteamUserId);
                        }
                        else
                        {
                            SendMessage("[CrunchEcon]", "You too poor", Color.Red, (long)Context.Player.SteamUserId);
                        }
                        break;
                    default:
                        SendMessage("[CrunchEcon]", "Incorrect usage, example - !eco pay player PlayerName amount or !eco pay faction tag amount", Color.Red, (long)Context.Player.SteamUserId);
                        break;

                }
            }
            else
            {
                SendMessage("[CrunchEcon]", "Player pay not enabled", Color.Red, (long)Context.Player.SteamUserId);
            }
        }
        public static void SendMessage(string author, string message, Color color, long steamID)
        {


            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId((ulong)steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }

        [Command("eco giveplayer", "gibs money to a player")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddMoneysPlayer(string playerNameOrId, Int64 amount)
        {
            Context.Respond("Legacy command. Use !eco give player PlayerName amount or !eco give faction tag amount");
            return;
            //Context.Respond("Error Player not online");
            IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(playerNameOrId);
            if (id == null)
            {
                Context.Respond("Error cant find that guy");
                return;
            }
            Context.Respond(id.DisplayName + " Balance Before Change : " + EconUtils.getBalance(id.IdentityId));

            //could use EconUtils.addMoney here
            MyBankingSystem.ChangeBalance(id.IdentityId, amount);

            Context.Respond(id.DisplayName + " Balance After Change : " + EconUtils.getBalance(id.IdentityId));
            return;
        }

        [Command("eco resetplayer", "set a players balance to 0")]
        [Permission(MyPromoteLevel.Admin)]
        public void ResetMoneysPlayer(string playerNameOrId)
        {
            //Context.Respond("Error Player not online");
            IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(playerNameOrId);
            if (id == null)
            {
                Context.Respond("Error cant find that guy");
                return;
            }
            Context.Respond(id.DisplayName + " Balance Before Change : " + EconUtils.getBalance(id.IdentityId));

            EconUtils.takeMoney(id.IdentityId, EconUtils.getBalance(id.IdentityId));

            Context.Respond(id.DisplayName + " Balance After Change : " + EconUtils.getBalance(id.IdentityId));
            return;
        }

        [Command("eco resetfac", "Reset a factions balance")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddMoneysFaction(string tag)
        {
            IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac != null)
            {
                Context.Respond(fac.Name + " FACTION Balance Before Change : " + fac.GetBalanceShortString());
                EconUtils.takeMoney(fac.FactionId, EconUtils.getBalance(fac.FactionId));
                Context.Respond(fac.Name + " FACTION Balance After Change : " + fac.GetBalanceShortString());
                return;
            }
            else
            {
                Context.Respond("Error faction not found");
            }
            return;

        }


        [Command("eco takeplayer", "removes money from a player")]
        [Permission(MyPromoteLevel.Admin)]
        public void RemoveMoneysPlayer(string playerNameOrId, Int64 amount)
        {
            Context.Respond("Legacy command. Use !eco take player PlayerName amount or !eco take faction tag amount");
            return;
            IMyIdentity id = CrunchUtilitiesPlugin.GetIdentityByNameOrId(playerNameOrId);
            if (id == null)
            {
                Context.Respond("Error cant find that guy");
                return;
            }
            long Balance = EconUtils.getBalance(id.IdentityId);
            if (Balance >= amount)
            {
                amount = amount * -1;
                Context.Respond(id.DisplayName + " Balance Before Change : " + EconUtils.getBalance(id.IdentityId));
                //could use EconUtils.takeMoney here
                MyBankingSystem.ChangeBalance(id.IdentityId, amount);
                Context.Respond(id.DisplayName + " Balance After Change : " + EconUtils.getBalance(id.IdentityId));
                return;
            }
            else
            {
                Context.Respond("Player doesnt have that much, player balance : " + EconUtils.getBalance(id.IdentityId));
            }

            return;

        }

        [Command("eco givefac", "gibs money to a faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddMoneysFaction(string tag, Int64 amount)
        {
            Context.Respond("Legacy command. Use !eco give player PlayerName amount or !eco give faction tag amount");
            return;
            IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac != null)
            {
                if (amount > 0)
                {
                    Context.Respond(fac.Name + " FACTION Balance Before Change : " + fac.GetBalanceShortString());
                    fac.RequestChangeBalance(amount);
                    Context.Respond(fac.Name + " FACTION Balance After Change : " + fac.GetBalanceShortString());
                    return;
                }
                else
                {
                    Context.Respond("Error must be a positive number");
                }
            }
            else
            {
                Context.Respond("Error faction not found");
            }
            return;

        }

        [Command("faction rep change", "Change repuation between factions")]
        [Permission(MyPromoteLevel.Admin)]
        public void ChangeFactionRep(string tag, string tag2, Int64 amount)
        {
            IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(tag);
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag2);
            if (fac != null && fac2 != null)
            {
                Context.Respond(fac.Name + " FACTION Reputation Before Change : " + MySession.Static.Factions.GetRelationBetweenFactions(fac.FactionId, fac2.FactionId));
                MySession.Static.Factions.SetReputationBetweenFactions(fac.FactionId, fac2.FactionId, int.Parse(amount.ToString()));
                Context.Respond(fac.Name + " FACTION Reputation After Change : " + MySession.Static.Factions.GetRelationBetweenFactions(fac.FactionId, fac2.FactionId));
            }
            else
            {
                Context.Respond("Error faction not found");
            }
            return;

        }

        [Command("player rep change", "Change repuation between faction and player")]
        [Permission(MyPromoteLevel.Admin)]
        public void ChangePlayerRep(string playerNameOrId, string tag, Int64 amount)
        {
            MyIdentity player = CrunchUtilitiesPlugin.GetIdentityByNameOrId(playerNameOrId);
            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (player != null && fac2 != null)
            {
                Context.Respond(player.DisplayName + " FACTION Reputation Before Change : " + MySession.Static.Factions.GetRelationBetweenPlayerAndFaction(Context.Player.IdentityId, fac2.FactionId));
                MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(player.IdentityId, fac2.FactionId, int.Parse(amount.ToString()));
                Context.Respond(player.DisplayName + " FACTION Reputation After Change : " + MySession.Static.Factions.GetRelationBetweenPlayerAndFaction(Context.Player.IdentityId, fac2.FactionId));
            }
            else
            {
                Context.Respond("Error faction not found");
            }
            return;

        }

        [Command("eco takefac", "remove money from a faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void removeMoneysFaction(string tag, Int64 amount)
        {
            Context.Respond("Legacy command. Use !eco take player PlayerName amount or !eco take faction tag amount");
            return;
            IMyFaction fac = MySession.Static.Factions.TryGetFactionByTag(tag);
            if (fac != null)
            {
                if (amount > 0)
                {
                    string temp = fac.GetBalanceShortString();
                    temp = temp.Replace("SC", "");
                    temp = temp.Replace(",", "");
                    temp = temp.Replace(" ", "");

                    //could maybe use econUtils.getBalance but i havent tested with a faction
                    long Balance = long.Parse(temp);
                    if (Balance >= amount)
                    {
                        amount = amount * -1;
                        Context.Respond(fac.Name + " FACTION Balance Before Change : " + fac.GetBalanceShortString());

                        fac.RequestChangeBalance(amount);
                        Context.Respond(fac.Name + " FACTION Balance After Change : " + fac.GetBalanceShortString());
                        return;
                    }
                    else
                    {
                        Context.Respond("Error deducting too much, Faction Balance :  " + fac.GetBalanceShortString());
                    }
                }
                else
                {
                    Context.Respond("Error must be a positive number");
                }
            }
            else
            {
                Context.Respond("Error faction not found");
            }
            return;

        }
    }
}
