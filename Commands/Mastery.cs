using ProjectM;
using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using Unity.Entities;

namespace RPGMods.Commands
{
    [Command(("mastery, m"), Usage = ("mastery [<log> <on>|<off>] [<reset> all|(mastery type)]"), Description = ("Display your current mastery progression, toggle the gain notification, or reset your mastery to gain effectiveness."))]
    public static class Mastery
    {
        private static EntityManager entityManager = Plugin.Server.EntityManager;
        public static bool detailedStatements = true;
        public static void Initialize(Context ctx)
        {
            if (!WeaponMasterSystem.isMasteryEnabled)
            {
                Output.CustomErrorMessage(ctx, Plugin.getTranslation("Weapon Mastery system is not enabled."));
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (ctx.Args.Length > 1) {
                if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("set")) && ctx.Args.Length >= 3) {
                    bool isAllowed = ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, Plugin.getTranslation("mastery_args"));
                    if (!isAllowed) return;
                    if (int.TryParse(ctx.Args[2], out int value)) {
                        string CharName = ctx.Event.User.CharacterName.ToString();
                        var UserEntity = ctx.Event.SenderUserEntity;
                        var CharEntity = ctx.Event.SenderCharacterEntity;
                        if (ctx.Args.Length == 4) {
                            string name = ctx.Args[3];
                            if (Helper.FindPlayer(name, true, out var targetEntity, out var targetUserEntity)) {
                                SteamID = entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
                                CharName = name;
                                UserEntity = targetUserEntity;
                                CharEntity = targetEntity;
                            }
                            else {
                                Output.CustomErrorMessage(ctx, Plugin.getTranslation("Could not find specified player \"")+name+Plugin.getTranslation("\"."));
                                return;
                            }
                        }
                        string MasteryType = ctx.Args[1].ToLower();
                        int type = 0;
                        if (MasteryType.Equals(Plugin.getTranslation("sword"))) type = (int)WeaponType.Sword+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("none"))|| MasteryType.Equals(Plugin.getTranslation("unarmed"))) type = (int)WeaponType.None+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("spear"))) type = (int)WeaponType.Spear+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("crossbow"))) type = (int)WeaponType.Crossbow+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("slashers"))) type = (int)WeaponType.Slashers+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("scythe"))) type = (int)WeaponType.Scythe+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("fishingpole"))) type = (int)WeaponType.FishingPole+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("mace"))) type = (int)WeaponType.Mace+1;
                        else if (MasteryType.Equals(Plugin.getTranslation("axes"))) type = (int)WeaponType.Axes+1;
                        else {
                            Output.InvalidArguments(ctx);
                            return;
                        }
                        WeaponMasterSystem.SetMastery(SteamID, type, value);
                        Output.SendSystemMessage(ctx, ctx.Args[1].ToUpper()+Plugin.getTranslation(" Mastery for \"")+CharName+Plugin.getTranslation("\" adjusted by <color=#fffffffe>")+value * 0.001 + Plugin.getTranslation("%</color>"));
                        Helper.ApplyBuff(UserEntity, CharEntity, Database.Buff.Buff_VBlood_Perk_Moose);
                        return;

                    }
                    else {
                        Output.InvalidArguments(ctx);
                        return;
                    }
                }
                if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("log"))) {
                    if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("on"))) {
                        Database.player_log_mastery[SteamID] = true;
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Mastery gain is now logged."));
                        return;
                    }
                    else if (ctx.Args[1].ToLower().Equals(Plugin.getTranslation("off"))) {
                        Database.player_log_mastery[SteamID] = false;
                        Output.SendSystemMessage(ctx, Plugin.getTranslation("Mastery gain is no longer being logged."));
                        return;
                    }
                    else {
                        Output.InvalidArguments(ctx);
                        return;
                    }
                }

                if (ctx.Args[0].ToLower().Equals(Plugin.getTranslation("reset")) && ctx.Args.Length >= 2) {

                    string MasteryType = ctx.Args[1].ToLower();
                    int type = 0;
                    if (MasteryType.Equals(Plugin.getTranslation("sword"))) type = (int)WeaponType.Sword + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("none")) || MasteryType.Equals(Plugin.getTranslation("unarmed"))) type = (int)WeaponType.None + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("spear"))) type = (int)WeaponType.Spear + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("crossbow"))) type = (int)WeaponType.Crossbow + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("slashers"))) type = (int)WeaponType.Slashers + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("scythe"))) type = (int)WeaponType.Scythe + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("fishingpole"))) type = (int)WeaponType.FishingPole + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("mace"))) type = (int)WeaponType.Mace + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("axes"))) type = (int)WeaponType.Axes + 1;
                    else if (MasteryType.Equals(Plugin.getTranslation("spell"))) type = 0;
                    else if (MasteryType.Equals(Plugin.getTranslation("all"))) type = -1;
                    else {
                        Output.InvalidArguments(ctx);
                        return;
                    }
                    Output.CustomErrorMessage(ctx, Plugin.getTranslation("Resetting ") + WeaponMasterSystem.typeToName(type) + Plugin.getTranslation(" Mastery"));
                    WeaponMasterSystem.resetMastery(SteamID, type);
                }
            }
            else {
                bool isDataExist = Database.player_weaponmastery.TryGetValue(SteamID, out var MasteryData);
                if (!isDataExist) {
                    Output.CustomErrorMessage(ctx, Plugin.getTranslation("You haven't even tried to master anything..."));
                    return;
                }

                Output.SendSystemMessage(ctx, Plugin.getTranslation("-- <color=#ffffffff>Weapon Mastery</color> --"));


                if (ctx.Event.SenderCharacterEntity != null) {
                    int weapon;
                    string name;
                    double masteryPercent;
                    float effectiveness;
                    string print;
                    bool ed = Database.playerWeaponEffectiveness.TryGetValue(SteamID, out WeaponMasterEffectivenessData effectivenessData);

                    if (detailedStatements) {
                        for (weapon = 0; weapon < WeaponMasterSystem.masteryStats.Length; weapon++) {
                            if (weapon >= WeaponMasterSystem.masteryStats.Length)
                                Output.SendSystemMessage(ctx, Plugin.getTranslation("Weapon type ")+weapon + Plugin.getTranslation(" beyond mastery stats weapon type limit of ")+(WeaponMasterSystem.masteryStats.Length - 1));
                            name = WeaponMasterSystem.typeToName(weapon);
                            masteryPercent = WeaponMasterSystem.masteryDataByType(weapon, SteamID) * 0.001;
                            effectiveness = 1;
                            if (ed && WeaponMasterSystem.effectivenessSubSystemEnabled)
                                effectiveness = effectivenessData.data[weapon];
                            print = name + Plugin.getTranslation(":<color=#fffffffe> ")+ masteryPercent + Plugin.getTranslation("%</color> (");
                            for (int i = 0; i < WeaponMasterSystem.masteryStats[weapon].Length; i++) {
                                if (i > 0)
                                    print += Plugin.getTranslation(",");
                                print += Helper.statTypeToString((UnitStatType)WeaponMasterSystem.masteryStats[weapon][i]);
                                print += Plugin.getTranslation(" <color=#75FF33>");
                                print += WeaponMasterSystem.calcBuffValue(weapon, (float)masteryPercent, SteamID, i);
                                print += Plugin.getTranslation("</color>");
                            }
                            print += Plugin.getTranslation(") Effectiveness: ")+effectiveness * 100 + Plugin.getTranslation("%");
                            if (WeaponMasterSystem.masteryStats[weapon].Length != 0) {
                                Output.SendSystemMessage(ctx, print);
                            }
                        }
                    }

                    else {
                        weapon = (int)WeaponMasterSystem.GetWeaponType(ctx.Event.SenderCharacterEntity)+1;
                        if (weapon >= WeaponMasterSystem.masteryStats.Length)
                            Output.SendSystemMessage(ctx, Plugin.getTranslation("Weapon type ") + weapon + Plugin.getTranslation(" beyond mastery stats weapon type limit of ") + (WeaponMasterSystem.masteryStats.Length - 1));
                        name = WeaponMasterSystem.typeToName(weapon);
                        masteryPercent = WeaponMasterSystem.masteryDataByType(weapon, SteamID) * 0.001;
                        effectiveness = 1;
                        if (ed)
                            effectiveness = effectivenessData.data[weapon];
                        print = name + Plugin.getTranslation(":<color=#fffffffe> ") + masteryPercent + Plugin.getTranslation("%</color> (");
                        for (int i = 0; i < WeaponMasterSystem.masteryStats[weapon].Length; i++) {
                            if (i > 0)
                                print += Plugin.getTranslation(",");
                            print += Helper.statTypeToString((UnitStatType)WeaponMasterSystem.masteryStats[weapon][i]);
                            print += Plugin.getTranslation(" <color=#75FF33>");
                            print += WeaponMasterSystem.calcBuffValue(weapon, (float)masteryPercent, SteamID, i);
                            print += Plugin.getTranslation("</color>");
                        }
                        print += Plugin.getTranslation(") Effectiveness: ") + effectiveness * 100 + Plugin.getTranslation("%");
                        if (WeaponMasterSystem.masteryStats[weapon].Length != 0) {
                            Output.SendSystemMessage(ctx, print);
                        }

                        weapon = 0;
                        name = WeaponMasterSystem.typeToName(weapon);
                        masteryPercent = WeaponMasterSystem.masteryDataByType(weapon, SteamID) * 0.001;
                        effectiveness = 1;
                        if (ed)
                            effectiveness = effectivenessData.data[weapon];
                        print = name + Plugin.getTranslation(":<color=#fffffffe> ") + masteryPercent + Plugin.getTranslation("%</color> (");
                        for (int i = 0; i < WeaponMasterSystem.masteryStats[weapon].Length; i++) {
                            if (i > 0)
                                print += Plugin.getTranslation(",");
                            print += Helper.statTypeToString((UnitStatType)WeaponMasterSystem.masteryStats[weapon][i]);
                            print += Plugin.getTranslation(" <color=#75FF33>");
                            print += WeaponMasterSystem.calcBuffValue(weapon, (float)masteryPercent, SteamID, i);
                            print += Plugin.getTranslation("</color>");
                        }
                        print += Plugin.getTranslation(") Effectiveness: ") + effectiveness * 100 + Plugin.getTranslation("%");
                        if (WeaponMasterSystem.masteryStats[weapon].Length != 0) {
                            Output.SendSystemMessage(ctx, print);
                        }
                    }
                }
            }
        }
    }
}
