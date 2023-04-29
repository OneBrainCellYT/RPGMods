﻿using RPGMods.Commands;
using RPGMods.Systems;

namespace RPGMods.Utils
{
    public static class AutoSaveSystem
    {
        //-- AutoSave is now directly hooked into the Server game save activity.
        public static void SaveDatabase()
        {
            PermissionSystem.SaveUserPermission(); //-- Nothing new to save.
            SunImmunity.SaveImmunity();
            Waypoint.SaveWaypoints();
            NoCooldown.SaveCooldown();
            GodMode.SaveGodMode();
            Speed.SaveSpeed();
            AutoRespawn.SaveAutoRespawn();
            //Kit.SaveKits();   //-- Nothing to save here for now.
            PowerUp.SavePowerUp();

            //-- System Related
            ExperienceSystem.SaveEXPData();
            PvPSystem.SavePvPStat();
            WeaponMasterSystem.SaveWeaponMastery();
            BanSystem.SaveBanList();
            WorldDynamicsSystem.SaveFactionStats();
            WorldDynamicsSystem.SaveIgnoredMobs();

            Plugin.saveLocalization();

            Plugin.Logger.LogInfo(Plugin.getTranslation("All database saved to JSON file."));
        }

        public static void LoadDatabase()
        {
            //-- Commands Related
            PermissionSystem.LoadPermissions();
            SunImmunity.LoadSunImmunity();
            Waypoint.LoadWaypoints();
            NoCooldown.LoadNoCooldown();
            GodMode.LoadGodMode();
            Speed.LoadSpeed();
            AutoRespawn.LoadAutoRespawn();
            Kit.LoadKits();
            PowerUp.LoadPowerUp();

            //-- System Related
            PvPSystem.LoadPvPStat();
            ExperienceSystem.LoadEXPData();
            WeaponMasterSystem.LoadWeaponMastery();
            BanSystem.LoadBanList();
            WorldDynamicsSystem.LoadFactionStats();
            WorldDynamicsSystem.LoadIgnoredMobs();

            Plugin.Logger.LogInfo(Plugin.getTranslation("All database is now loaded."));
        }
    }
}
