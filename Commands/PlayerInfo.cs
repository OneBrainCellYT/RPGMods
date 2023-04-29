using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using System;
using Unity.Transforms;

namespace RPGMods.Commands
{
    [Command(("playerinfo, i"), Usage = ("playerinfo <Name>"), Description = ("Display the player information details."))]
    public static class PlayerInfo
    {
        public static void Initialize(Context ctx)
        {
            if (ctx.Args.Length < 1) 
            {
                Output.MissingArguments(ctx);
                return;
            }

            if (!Helper.FindPlayer(ctx.Args[0], false, out var playerEntity, out var userEntity))
            {
                Output.CustomErrorMessage(ctx, Plugin.getTranslation("Player not found.")); 
                return;
            }

            var userData = ctx.EntityManager.GetComponentData<User>(userEntity);

            ulong SteamID = userData.PlatformId;
            string Name = userData.CharacterName.ToString();
            string CharacterEntity = playerEntity.Index.ToString() + Plugin.getTranslation(":") + playerEntity.Version.ToString();
            string UserEntity = userEntity.Index.ToString() + Plugin.getTranslation(":") + userEntity.Version.ToString();
            var ping = (int) ctx.EntityManager.GetComponentData<Latency>(playerEntity).Value;
            var position = ctx.EntityManager.GetComponentData<Translation>(playerEntity).Value;

            Database.PvPStats.TryGetValue(SteamID, out var pvpStats);
            Database.player_experience.TryGetValue(SteamID, out var exp);

            Output.SendSystemMessage(ctx, Plugin.getTranslation("Name: ")+Color.White(Name));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("SteamID: ")+Color.White(SteamID.ToString())+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("Latency: ")+Color.White(ping.ToString()));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Position --"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("X: ")+Color.White(Math.Round(position.x,2).ToString())+ Plugin.getTranslation(" ") +
                Plugin.getTranslation("Y: ")+Color.White(Math.Round(position.y,2).ToString())+ Plugin.getTranslation(" ") +
                Plugin.getTranslation("Z: ")+Color.White(Math.Round(position.z,2).ToString())+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("-- ")+Color.White(")EntitiesPlugin.getTranslation(")+ Plugin.getTranslation(" --"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("Char Entity: ")+Color.White(CharacterEntity)+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("User Entity: ")+Color.White(UserEntity)+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("-- ")+Color.White(")ExperiencePlugin.getTranslation(")+ Plugin.getTranslation(" --"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("Level: ")+Color.White(ExperienceSystem.convertXpToLevel(exp).ToString())+ Plugin.getTranslation(" [")+Color.White(exp.ToString())+ Plugin.getTranslation("]"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("-- ")+Color.White(")PvP StatsPlugin.getTranslation(")+ Plugin.getTranslation(" --"));

            if (PvPSystem.isHonorSystemEnabled)
            {
                Database.SiegeState.TryGetValue(SteamID, out var siegeState);
                Cache.HostilityState.TryGetValue(playerEntity, out var hostilityState);

                double tLeft = 0;
                if (siegeState.IsSiegeOn)
                {
                    TimeSpan TimeLeft = siegeState.SiegeEndTime - DateTime.Now;
                    tLeft = Math.Round(TimeLeft.TotalHours, 2);
                }

                string hostilityText = hostilityState.IsHostile ? Plugin.getTranslation("Aggresive") : Plugin.getTranslation("Passive");
                string siegeText = siegeState.IsSiegeOn ? Plugin.getTranslation("Sieging") : Plugin.getTranslation("Defensive");

                Output.SendSystemMessage(ctx, Plugin.getTranslation("Reputation: {Color.White(pvpStats.Reputation.ToString())}"));
                Output.SendSystemMessage(ctx, Plugin.getTranslation("Hostility: ")+Color.White(hostilityText)+ Plugin.getTranslation(""));
                Output.SendSystemMessage(ctx, Plugin.getTranslation("Siege: ")+Color.White(siegeText)+ Plugin.getTranslation(""));
                Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Time Left: ")+Color.White(tLeft.ToString())+ Plugin.getTranslation(" hour(s)"));
            }

            Output.SendSystemMessage(ctx, Plugin.getTranslation("K/D: ")+Color.White(pvpStats.KD.ToString())+ Plugin.getTranslation(" ") +
                Plugin.getTranslation("Kill: ")+Color.White(pvpStats.Kills.ToString())+ Plugin.getTranslation(" ") +
                Plugin.getTranslation("Death: ")+Color.White(pvpStats.Deaths.ToString())+ Plugin.getTranslation(""));
        }
    }

    [Command(("myinfo, me"), Usage = ("myinfo"), Description = ("Display your information details."))]
    public static class MyInfo
    {
        public static void Initialize(Context ctx)
        {
            ulong SteamID = ctx.Event.User.PlatformId;
            string Name = ctx.Event.User.CharacterName.ToString();
            string CharacterEntity = ctx.Event.SenderCharacterEntity.Index.ToString() + Plugin.getTranslation(":") + ctx.Event.SenderCharacterEntity.Version.ToString();
            string UserEntity = ctx.Event.SenderUserEntity.Index.ToString() + Plugin.getTranslation(":") + ctx.Event.SenderUserEntity.Version.ToString();
            var ping = ctx.EntityManager.GetComponentData<Latency>(ctx.Event.SenderCharacterEntity).Value;
            var position = ctx.EntityManager.GetComponentData<Translation>(ctx.Event.SenderCharacterEntity).Value;

            Output.SendSystemMessage(ctx, Plugin.getTranslation("Name: ")+Color.White(Name)+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("SteamID: ")+Color.White(SteamID.ToString())+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("Latency: ")+Color.White(ping.ToString())+ Plugin.getTranslation("s"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Position --"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("X: ")+Color.White(Math.Round(position.x,2).ToString())+ Plugin.getTranslation(" ") +
                Plugin.getTranslation("Y: ")+Color.White(Math.Round(position.y,2).ToString())+ Plugin.getTranslation(" ") +
                Plugin.getTranslation("Z: ")+Color.White(Math.Round(position.z,2).ToString())+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("-- Entities --"));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("Char Entity: ")+Color.White(CharacterEntity)+ Plugin.getTranslation(""));
            Output.SendSystemMessage(ctx, Plugin.getTranslation("User Entity: ")+Color.White(UserEntity)+ Plugin.getTranslation(""));
        }
    }
}
