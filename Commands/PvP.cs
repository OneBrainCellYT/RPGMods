using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace RPGMods.Commands
{
    [Command(("pvp"), Usage = ("pvp [<on>|<off>|<top>]"), Description = ("Display your PvP statistics or toggle PvP/Castle Siege state"))]
    public static class PvP
    {
        public static void Initialize(Context ctx)
        {
            var user = ctx.Event.User;
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;
            var CharName = user.CharacterName.ToString();
            var SteamID = user.PlatformId;

            if (ctx.Args.Length == 0)
            {
                Database.PvPStats.TryGetValue(SteamID, out var PvPStats);

                Output.SendSystemMessage(ctx, Plugin.getTranslation("Name: ")+Color.White(CharName));
                if (PvPSystem.isHonorSystemEnabled) {
                    Database.SiegeState.TryGetValue(SteamID, out var siegeState);
                    Cache.HostilityState.TryGetValue(charEntity, out var hostilityState);

                    double tLeft = 0;
                    if (siegeState.IsSiegeOn) {
                        TimeSpan TimeLeft = siegeState.SiegeEndTime - DateTime.Now;
                        tLeft = Math.Round(TimeLeft.TotalHours, 2);
                        if (PvPStats.Reputation <= -20000) {
                            tLeft = -1;
                        }
                    }

                    string hostilityText = hostilityState.IsHostile ? Plugin.getTranslation("Aggresive") : Plugin.getTranslation("Passive");
                    string siegeText = siegeState.IsSiegeOn ? Plugin.getTranslation("Sieging") : Plugin.getTranslation("Defensive");

                    Cache.ReputationLog.TryGetValue(SteamID, out var RepLog);
                    TimeSpan ReputationSpan = DateTime.Now - RepLog.TimeStamp;

                    var TimeLeftUntilRefresh = PvPSystem.HonorGainSpanLimit - ReputationSpan.TotalMinutes;
                    if (TimeLeftUntilRefresh > 0) {
                        TimeLeftUntilRefresh = Math.Round(TimeLeftUntilRefresh, 2);
                    }
                    else {
                        TimeLeftUntilRefresh = 0;
                        RepLog.TotalGained = 0;
                    }

                    int HonorGainLeft = PvPSystem.MaxHonorGainPerSpan - RepLog.TotalGained;

                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Reputation: ") + Color.White(PvPStats.Reputation.ToString()) + Plugin.getTranslation(""));
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Time Left Until Refresh: ") + Color.White(TimeLeftUntilRefresh.ToString()) + Plugin.getTranslation(" minute(s)"));
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Available Reputation Gain: ") + Color.White(HonorGainLeft.ToString()) + Plugin.getTranslation(" point(s)"));
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Hostility: ") + Color.White(hostilityText) + Plugin.getTranslation(""));
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Siege: ") + Color.White(siegeText) + Plugin.getTranslation(""));
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Time Left: ") + Color.White(tLeft.ToString()) + Plugin.getTranslation(" hour(s)"));
                }
                Output.SendSystemMessage(ctx, Plugin.getTranslation("K/D: ")+Color.White(PvPStats.KD.ToString())+ Plugin.getTranslation(" [")+Color.White(PvPStats.Kills.ToString())+ Plugin.getTranslation("/")+Color.White(PvPStats.Deaths.ToString())+ Plugin.getTranslation("]"));
            }

            if (ctx.Args.Length > 0)
            {
                var isPvPShieldON = false;

                if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("on"))) isPvPShieldON = false;
                else if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("off"))) isPvPShieldON = true;

                if (ctx.Args.Length == 1)
                {
                    if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("top")))
                    {
                        if (PvPSystem.isLadderEnabled)
                        {
                            _ = PvPSystem.TopRanks(ctx);
                            return;
                        }
                        else
                        {
                            Output.CustomErrorMessage(ctx, Plugin.getTranslation("Leaderboard is not enabled."));
                            return;
                        }
                    }

                    if (PvPSystem.isHonorSystemEnabled)
                    {
                        if (Helper.IsPlayerInCombat(charEntity))
                        {
                            Output.CustomErrorMessage(ctx, Plugin.getTranslation("Unable to change state, you are in combat!"));
                            return;
                        }

                        Database.PvPStats.TryGetValue(SteamID, out var PvPStats);
                        Database.SiegeState.TryGetValue(SteamID, out var siegeState);

                        if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("on")))
                        {
                            PvPSystem.HostileON(SteamID, charEntity, userEntity);
                            Output.SendSystemMessage(ctx, Plugin.getTranslation("Entering aggresive state!"));
                            return;
                        }
                        else if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("off")))
                        {
                            if (PvPStats.Reputation <= -1000)
                            {
                                Output.CustomErrorMessage(ctx, Plugin.getTranslation("You're [")+PvPSystem.GetHonorTitle(PvPStats.Reputation).Title + Plugin.getTranslation("], aggresive state is enforced."));
                                return;
                            }

                            if (siegeState.IsSiegeOn)
                            {
                                Output.CustomErrorMessage(ctx, Plugin.getTranslation("You're in siege mode, aggressive state is enforced."));
                                return;
                            }
                            PvPSystem.HostileOFF(SteamID, charEntity);
                            Output.SendSystemMessage(ctx, Plugin.getTranslation("Entering passive state!"));
                            return;
                        }
                    }
                    else
                    {
                        if (!PvPSystem.isPvPToggleEnabled)
                        {
                            Output.CustomErrorMessage(ctx, Plugin.getTranslation("PvP toggling is not enabled!"));
                            return;
                        }
                        if (Helper.IsPlayerInCombat(charEntity))
                        {
                            Output.CustomErrorMessage(ctx, Plugin.getTranslation("Unable to change PvP Toggle, you are in combat!"));
                            return;
                        }
                        Helper.SetPvPShield(charEntity, isPvPShieldON);
                        string s = isPvPShieldON ? Plugin.getTranslation("OFF") : Plugin.getTranslation("ON");
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("PvP is now {s}"));
                    }
                    return;
                }
                else if (ctx.Args.Length >= 2 && (ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, Plugin.getTranslation("pvp_args"))))
                {
                    if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("rep")) && PvPSystem.isHonorSystemEnabled)
                    {
                        if (int.TryParse(ctx.Args[1], out var value))
                        {
                            if (value > 9999) value = 9999;
                            string name = CharName;
                            if (ctx.Args.Length == 3)
                            {
                                name = ctx.Args[2];
                                if (Helper.FindPlayer(name, false, out _, out var targetUser))
                                {
                                    SteamID = Plugin.Server.EntityManager.GetComponentData<User>(targetUser).PlatformId;
                                }
                                else
                                {
                                    Output.CustomErrorMessage(ctx, Plugin.getTranslation("Unable to find the specified player!"));
                                    return;
                                }
                            }
                            Database.PvPStats.TryGetValue(SteamID, out var PvPData);
                            PvPData.Reputation = value;
                            Database.PvPStats[SteamID] = PvPData;
                            Output.SendSystemMessage(ctx, Plugin.getTranslation("Player \"")+name+Plugin.getTranslation("\"'s reputation is now set to ")+value);
                        }
                    }
                    else
                    {
                        if (PvPSystem.isHonorSystemEnabled)
                        {
                            string name = ctx.Args[1];
                            if (Helper.FindPlayer(name, false, out Entity targetChar, out Entity targetUser))
                            {
                                SteamID = Plugin.Server.EntityManager.GetComponentData<User>(targetUser).PlatformId;
                                Database.PvPStats.TryGetValue(SteamID, out var PvPStats);
                                if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("on")))
                                {
                                    PvPSystem.HostileON(SteamID, targetChar, targetUser);
                                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Vampire \"")+name+Plugin.getTranslation("\" is now in aggresive state!"));
                                    return;
                                }
                                else if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("off")))
                                {
                                    if (PvPStats.Reputation <= -1000)
                                    {
                                        Output.CustomErrorMessage(ctx, Plugin.getTranslation("Vampire \"")+name+Plugin.getTranslation("\" is [")+PvPSystem.GetHonorTitle(PvPStats.Reputation).Title + Plugin.getTranslation("], aggresive state is enforced."));
                                        return;
                                    }
                                    PvPSystem.HostileOFF(SteamID, targetChar);
                                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Vampire \"")+name+Plugin.getTranslation("\" is now in passive state!"));
                                    return;
                                }
                                return;
                            }
                            else
                            {
                                Output.CustomErrorMessage(ctx, Plugin.getTranslation("Unable to find the specified player!"));
                                return;
                            }
                        }
                        else
                        {
                            string name = ctx.Args[1];
                            if (Helper.FindPlayer(name, false, out Entity targetChar, out _))
                            {
                                Helper.SetPvPShield(targetChar, isPvPShieldON);
                                string s = isPvPShieldON ? Plugin.getTranslation("OFF") : Plugin.getTranslation("ON");
                                Output.SendSystemMessage(ctx, Plugin.getTranslation("Player \"")+name+Plugin.getTranslation(" PvP is now ")+s);
                                return;
                            }
                            else
                            {
                                Output.CustomErrorMessage(ctx, Plugin.getTranslation("Unable to find the specified player!"));
                                return;
                            }
                        }
                    }
                }
                else
                {
                    Output.InvalidArguments(ctx);
                    return;
                }
            }
        }
    }
}
