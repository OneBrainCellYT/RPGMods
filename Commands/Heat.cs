using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using System;
using Unity.Entities;

namespace RPGMods.Commands
{
    [Command(("heat"), Usage = ("heat"), Description = ("Shows your current wanted level."))]
    public static class Heat
    {
        private static EntityManager entityManager = Plugin.Server.EntityManager;
        public static void Initialize(Context ctx)
        {
            var user = ctx.Event.User;
            var SteamID = user.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (!HunterHuntedSystem.isActive)
            {
                Output.CustomErrorMessage(ctx, Plugin.getTranslation("HunterHunted system is not enabled."));
                return;
            }

            bool isAllowed = ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, Plugin.getTranslation("heat_args"));
            if (ctx.Args.Length >= 2 && isAllowed)
            {
                string CharName = ctx.Event.User.CharacterName.ToString();
                if (ctx.Args.Length == 3)
                {
                    string name = ctx.Args[2];
                    if (Helper.FindPlayer(name, true, out var targetEntity, out var targetUserEntity))
                    {
                        SteamID = entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
                        CharName = name;
                        userEntity = targetUserEntity;
                        charEntity = targetEntity;
                    }
                    else
                    {
                        Output.CustomErrorMessage(ctx, Plugin.getTranslation("Could not find specified player \"")+name+Plugin.getTranslation("\"."));
                        return;
                    }
                }
                if (int.TryParse(ctx.Args[0], out var n)) Cache.heatlevel[SteamID] = n;
                if (int.TryParse(ctx.Args[1], out var nm)) Cache.bandit_heatlevel[SteamID] = nm;
                Output.SendSystemMessage(ctx, Plugin.getTranslation("Player \"")+CharName+Plugin.getTranslation("\" heat value changed."));
                Output.SendSystemMessage(ctx, Plugin.getTranslation("Human: <color=#ffff00>")+Cache.heatlevel[SteamID] + Plugin.getTranslation("</color> | Bandit: <color=#ffff00>") +Cache.bandit_heatlevel[SteamID]+Plugin.getTranslation("</color>"));
                HunterHuntedSystem.HeatManager(userEntity);
                return;
            }

            HunterHuntedSystem.HeatManager(userEntity);

            Cache.heatlevel.TryGetValue(SteamID, out var human_heatlevel);
            if (human_heatlevel >= 1500) Output.SendLore(userEntity,Plugin.getTranslation("<color=#0048ffff>[Humans]</color> <color=#c90e21ff>YOU ARE A MENACE...</color>"));
            else if (human_heatlevel >= 1000) Output.SendLore(userEntity, Plugin.getTranslation("<color=#0048ffff>[Humans]</color> <color=#c90e21ff>The Vampire Hunters are hunting you...</color>"));
            else if (human_heatlevel >= 500) Output.SendLore(userEntity, Plugin.getTranslation("<color=#0048ffff>[Humans]</color> <color=#c90e21ff>Humans elite squads are hunting you...</color>"));
            else if (human_heatlevel >= 250) Output.SendLore(userEntity, Plugin.getTranslation("<color=#0048ffff>[Humans]</color> <color=#c4515cff>Humans soldiers are hunting you...</color>"));
            else if (human_heatlevel >= 150) Output.SendLore(userEntity, Plugin.getTranslation("<color=#0048ffff>[Humans]</color> <color=#c9999eff>The humans are hunting you...</color>"));
            else Output.SendLore(userEntity, Plugin.getTranslation("<color=#0048ffff>[Humans]</color> <color=#ffffffff>You're currently anonymous...</color>"));

            Cache.bandit_heatlevel.TryGetValue(SteamID, out var bandit_heatlevel);
            if (bandit_heatlevel >= 650) Output.SendLore(userEntity, Plugin.getTranslation("<color=#ff0000ff>[Bandits]</color> <color=#c90e21ff>The bandits really wants you dead...</color>"));
            else if (bandit_heatlevel >= 450) Output.SendLore(userEntity, Plugin.getTranslation("<color=#ff0000ff>[Bandits]</color> <color=#c90e21ff>A large bandit squads are hunting you...</color>"));
            else if (bandit_heatlevel >= 250) Output.SendLore(userEntity, Plugin.getTranslation("<color=#ff0000ff>[Bandits]</color> <color=#c4515cff>A small bandit squads are hunting you...</color>"));
            else if (bandit_heatlevel >= 150) Output.SendLore(userEntity,Plugin.getTranslation("<color=#ff0000ff>[Bandits]</color> <color=#c9999eff>The bandits are hunting you...</color>"));
            else Output.SendLore(userEntity, Plugin.getTranslation("<color=#ff0000ff>[Bandits]</color> <color=#ffffffff>The bandits doesn't recognize you...</color>"));

            if (ctx.Args.Length == 1 && user.IsAdmin)
            {
                if (!ctx.Args[0].Equals(Plugin.getTranslation("debug")) && ctx.Args.Length != 2) return;

                Cache.player_last_ambushed.TryGetValue(SteamID, out var last_ambushed);
                TimeSpan since_ambush = DateTime.Now - last_ambushed;
                int NextAmbush = (int)(HunterHuntedSystem.ambush_interval - since_ambush.TotalSeconds);
                if (NextAmbush < 0) NextAmbush = 0;

                Output.SendSystemMessage(ctx, Plugin.getTranslation("Next Possible Ambush in ")+Color.White(NextAmbush.ToString()) + Plugin.getTranslation(" seconds"));
                Output.SendSystemMessage(ctx, Plugin.getTranslation("Ambush Chance: ")+Color.White(HunterHuntedSystem.ambush_chance.ToString())+Plugin.getTranslation("%"));
                Output.SendSystemMessage(ctx, Plugin.getTranslation("Human: ")+Color.White(human_heatlevel.ToString()) + Plugin.getTranslation(" | Bandit: ")+Color.White(bandit_heatlevel.ToString()));
            }
        }
    }
}
