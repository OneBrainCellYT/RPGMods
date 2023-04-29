using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using Unity.Entities;

namespace RPGMods.Commands
{
    [Command(Plugin.getTranslation("experience, exp, xp"), Usage = Plugin.getTranslation("experience [<log> <on>|<off>]"), Description = Plugin.getTranslation("Shows your currect experience and progression to next level, or toggle the exp gain notification."))]
    public static class Experience
    {
        private static EntityManager entityManager = Plugin.Server.EntityManager;
        public static void Initialize(Context ctx) {
            var user = ctx.Event.User;
            var CharName = user.CharacterName.ToString();
            var SteamID = user.PlatformId;
            var PlayerCharacter = ctx.Event.SenderCharacterEntity;
            var UserEntity = ctx.Event.SenderUserEntity;

            if (!ExperienceSystem.isEXPActive) {
                Output.CustomErrorMessage(ctx, Plugin.getTranslation("Experience system is not enabled."));
                return;
            }

            if (ctx.Args.Length >= 2) {
                bool isAllowed = ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, Plugin.getTranslation("experience_args"));
                if (ctx.Args[0].Equals(Plugin.getTranslation("set")) && isAllowed && int.TryParse(ctx.Args[1], out int value)) {
                    if (ctx.Args.Length == 3) {
                        string name = ctx.Args[2];
                        if (Helper.FindPlayer(name, true, out var targetEntity, out var targetUserEntity)) {
                            CharName = name;
                            SteamID = entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
                            PlayerCharacter = targetEntity;
                            UserEntity = targetUserEntity;
                        }
                        else {
                            Output.CustomErrorMessage(ctx, Plugin.getTranslation("Could not find specified player \"") + name + Plugin.getTranslation("\""));
                            return;
                        }
                    }
                    Database.player_experience[SteamID] = value;
                    ExperienceSystem.SetLevel(PlayerCharacter, UserEntity, SteamID);
                    Output.SendSystemMessage(ctx, Plugin.getTranslation("Player \"") + CharName + Plugin.getTranslation("\" Experience is now set to be<color =#fffffffe> ") + ExperienceSystem.getXp(SteamID) + Plugin.getTranslation("</color>"));
                }
                else if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("log"))) {
                    if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("on"))) {
                        Database.player_log_exp[SteamID] = true;
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Experience gain is now logged."));
                        return;
                    }
                    else if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("off"))) {
                        Database.player_log_exp[SteamID] = false;
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Experience gain is no longer being logged."));
                        return;
                    }
                }
                else {
                    Output.InvalidArguments(ctx);
                    return;
                }
            }
            else {
                int userLevel = ExperienceSystem.getLevel(SteamID);
                Output.SendSystemMessage(ctx, Plugin.getTranslation("-- <color=#fffffffe>") + CharName + Plugin.getTranslation("</color> --"));
                Output.SendSystemMessage(ctx,
                    Plugin.getTranslation("Level:<color=#fffffffe> ") + userLevel + Plugin.getTranslation("</ color > (< color =#fffffffe>")+ExperienceSystem.getLevelProgress(SteamID)+Plugin.getTranslation(" %</color>) ") +
                    Plugin.getTranslation(" [ XP:<color=#fffffffe> ")+ ExperienceSystem.getXp(SteamID)+Plugin.getTranslation("</color>/<color=#fffffffe>")+ExperienceSystem.convertLevelToXp(userLevel + 1)+Plugin.getTranslation(" </color> ]"));
            }
        }
    }
}
