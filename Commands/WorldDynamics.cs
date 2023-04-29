using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Entities;

namespace RPGMods.Commands
{
    [Command(("worlddynamics, wd"), Usage = ("wd [<faction>] [<stats>|<save>|<load>|<ignore>|<unignore>] [<npc prefab name>]"), Description = ("List all faction stats. Save them, or load from the json file."))]
    public static class WorldDynamics
    {
        public static void Initialize(Context ctx)
        {
            if (WorldDynamicsSystem.isFactionDynamic == false)
            {
                Output.CustomErrorMessage(ctx, Plugin.getTranslation("World dynamics system is not enabled."));
                return;
            }

            if (ctx.Args.Length < 2)
            {
                Output.MissingArguments(ctx);
                return;
            }

            if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("faction")))
            {
                if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("ignore")))
                {
                    if (ctx.Args.Length < 3)
                    {
                        Output.MissingArguments(ctx);
                        return;
                    }

                    string mobName = ctx.Args[2];
                    if (Database.database_units.TryGetValue(mobName, out var mobGUID))
                    {
                        Database.IgnoredMonsters.Add(mobName);
                        Database.IgnoredMonstersGUID.Add(mobGUID);
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("NPC \"")+mobName+Plugin.getTranslation("\" is now ignored for faction buffing."));
                        return;
                    }
                    else
                    {
                        Output.CustomErrorMessage(ctx, Plugin.getTranslation("Specified NPC not found."));
                        return;
                    }
                }
                if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("unignore")))
                {
                    if (ctx.Args.Length < 3)
                    {
                        Output.MissingArguments(ctx);
                        return;
                    }

                    string mobName = ctx.Args[2];
                    if (Database.database_units.TryGetValue(mobName, out var mobGUID))
                    {
                        Database.IgnoredMonsters.Remove(mobName);
                        Database.IgnoredMonstersGUID.Remove(mobGUID);
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("NPC \"")+mobName+Plugin.getTranslation("\" is removed from faction buff ignore list."));
                    }
                    else
                    {
                        Output.CustomErrorMessage(ctx, Plugin.getTranslation("Specified NPC not found."));
                        return;
                    }
                }
                if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("stats")))
                {
                    int i = 0;
                    foreach (var item in Database.FactionStats)
                    {
                        if (!item.Value.Active) continue;
                        i++;
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Name: ")+Color.Green(item.Value.Name)+ Plugin.getTranslation(" [Lv.")+Color.Yellow(item.Value.Level.ToString())+ Plugin.getTranslation("]"));
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Active Pwr: [")+Color.White(item.Value.ActivePower.ToString())+ Plugin.getTranslation("] Stored Pwr: [")+Color.Yellow(item.Value.StoredPower.ToString())+ Plugin.getTranslation("]"));
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Daily Pwr: [")+Color.Teal(item.Value.DailyPower.ToString())+ Plugin.getTranslation("] Req. Pwr: [")+Color.SoftRed(item.Value.RequiredPower.ToString())+ Plugin.getTranslation("]"));
                    }
                    if (i == 0) Output.SendSystemMessage(ctx, Plugin.getTranslation("No active facton."));
                    return;
                }
                if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("save")))
                {
                    WorldDynamicsSystem.SaveFactionStats();
                    WorldDynamicsSystem.SaveIgnoredMobs();
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Factions data & ignored mobs saved."));
                    return;
                }
                if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("load")))
                {
                    WorldDynamicsSystem.LoadFactionStats();
                    WorldDynamicsSystem.LoadIgnoredMobs();
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Factions & ignored mobs json data loaded."));
                    return;
                }
            }
        }
    }
}
