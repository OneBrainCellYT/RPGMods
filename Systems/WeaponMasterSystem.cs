using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Entities;
using RPGMods.Utils;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace RPGMods.Systems
{
    public class WeaponMasterSystem
    {
        public static EntityManager em = Plugin.Server.EntityManager;

        public static bool isMasteryEnabled = true;
        public static bool isDecaySystemEnabled = true;
        public static int MasteryCombatTick = 5;
        public static int MaxCombatTick = 12;
        public static float MasteryMultiplier = 1;
        public static int DecayInterval = 60;
        public static int Online_DecayValue = 0;
        public static int Offline_DecayValue = 1;
        public static int MaxMastery = 100000;
        public static float VBloodMultiplier = 15;
        // Shou Change - make options for spell mastery with weapons active.
        public static Boolean spellMasteryNeedsNoneToUse = true;
        public static Boolean spellMasteryNeedsNoneToLearn = true;
        public static Boolean linearCDR = false;
        public static Boolean CDRStacks = false;

        private static PrefabGUID vBloodType = new PrefabGUID(1557174542);

        private static readonly Random rand = new Random();

        // Idk how to do this elegantly and allow it to be bound to a config.
        public static int[] UnarmedStats = { 0, 5 };
        public static float[] UnarmedRates = { 0.25f, 0.01f };

        public static int[] SpearStats = { 0 };
        public static float[] SpearRates = { 0.25f };

        public static int[] SwordStats = { 0, 25 };
        public static float[] SwordRates = { 0.125f, 0.125f };

        public static int[] ScytheStats = { 0 , 29 };
        public static float[] ScytheRates = { 0.125f, 0.00125f };

        public static int[] CrossbowStats = { 29 };
        public static float[] CrossbowRates = { 0.0025f };

        public static int[] MaceStats = { 4 };
        public static float[] MaceRates = { 1f };

        public static int[] SlasherStats = { 29, 5 };
        public static float[] SlasherRates = { 0.00125f, 0.005f };

        public static int[] AxeStats = { 0, 4 };
        public static float[] AxeRates = { 0.125f, 0.5f };

        public static int[] FishingPoleStats = { };
        public static float[] FishingPoleRates = { };

        public static int[] SpellStats = { 7 };
        public static float[] SpellRates = { 100 };

        public static int[][] masteryStats = { SpellStats, UnarmedStats, SpearStats, SwordStats, ScytheStats, CrossbowStats, MaceStats, SlasherStats, AxeStats, FishingPoleStats };
        public static float[][] masteryRates = { SpellRates, UnarmedRates, SpearRates, SwordRates, ScytheRates, CrossbowRates, MaceRates, SlasherRates, AxeRates, FishingPoleRates };

        public static float maxEffectiveness = 10;
        public static bool effectivenessSubSystemEnabled = false;
        public static bool growthSubSystemEnabled = false;
        public static float minGrowth = 0.1f;
        public static float maxGrowth = 10;
        public static float growthPerEfficency = 1;

        public static void UpdateMastery(Entity Killer, Entity Victim)
        {
            if (Killer == Victim) return;
            if (em.HasComponent<Minion>(Victim)) return;

            Entity userEntity = em.GetComponentData<PlayerCharacter>(Killer).UserEntity._Entity;
            User User = em.GetComponentData<User>(userEntity);
            ulong SteamID = User.PlatformId;
            int weapon = (int)GetWeaponType(Killer)+1;

            int MasteryValue;
            int spellMasteryValue;
            var VictimStats = em.GetComponentData<UnitStats>(Victim);
            MasteryValue = (int)VictimStats.PhysicalPower;
            spellMasteryValue = (int)VictimStats.SpellPower;

            float weaponGrowth = 1;
            float spellGrowth = 1;

            if (growthSubSystemEnabled) {
                bool isGrowthFound = Database.playerWeaponGrowth.TryGetValue(SteamID, out WeaponMasterGrowthData growth);
                if (!isGrowthFound) {
                    growth = new WeaponMasterGrowthData();
                    Database.playerWeaponGrowth[SteamID] = growth;
                }
                weaponGrowth = growth.data[weapon];
                spellGrowth = growth.data[0];
            }

            MasteryValue = (int)(MasteryValue * (rand.Next(10, 100) * 0.01) * weaponGrowth);
            spellMasteryValue = (int)(MasteryValue * (rand.Next(10, 100) * 0.01) * spellGrowth);

            bool isVBlood;
            if (em.HasComponent<BloodConsumeSource>(Victim))
            {
                BloodConsumeSource BloodSource = em.GetComponentData<BloodConsumeSource>(Victim);
                isVBlood = BloodSource.UnitBloodType.Equals(vBloodType);
            }
            else
            {
                isVBlood = false;
            }

            if (isVBlood) {
                MasteryValue = (int)(MasteryValue * VBloodMultiplier);
                spellMasteryValue = (int)(spellMasteryValue * VBloodMultiplier);
            }

            if (em.HasComponent<PlayerCharacter>(Victim))
            {
                Equipment VictimGear = em.GetComponentData<Equipment>(Victim);
                var BonusMastery = VictimGear.ArmorLevel + VictimGear.WeaponLevel + VictimGear.SpellLevel;
                MasteryValue *= (int)(1 + (BonusMastery * 0.01));
                spellMasteryValue *= (int)(1 + (BonusMastery * 0.01));
            }

            MasteryValue = (int)(MasteryValue * MasteryMultiplier);
            spellMasteryValue = (int)(spellMasteryValue * MasteryMultiplier);
            SetMastery(SteamID, weapon, MasteryValue);
            if (weapon == (((int)WeaponType.None) + 1) || !spellMasteryNeedsNoneToLearn) {
                SetMastery(SteamID, 0, spellMasteryValue);
            }

            if (Database.player_log_mastery.TryGetValue(SteamID, out bool isLogging))
            {
                if (isLogging)
                {
                    Output.SendLore(userEntity, Plugin.getTranslation("<color=#ffb700>Weapon mastery has increased by ")+MasteryValue * 0.001 + Plugin.getTranslation("%</color>"));
                }
            }
        }

        public static void LoopMastery(Entity User, Entity Player)
        {
            User userData = em.GetComponentData<User>(User);
            ulong SteamID = userData.PlatformId;

            Cache.player_last_combat.TryGetValue(SteamID, out var LastCombat);
            TimeSpan elapsed_time = DateTime.Now - LastCombat;
            if (elapsed_time.TotalSeconds >= 10) Cache.player_combat_ticks[SteamID] = 0;
            if (elapsed_time.TotalSeconds * 0.2 < 1) return;

            Cache.player_last_combat[SteamID] = DateTime.Now;

            if (Cache.player_combat_ticks[SteamID] > MaxCombatTick) return;
            WeaponType WeaponType = GetWeaponType(Player);



            float weaponGrowth = 1;
            float spellGrowth = 1;

            if (growthSubSystemEnabled) {
                bool isGrowthFound = Database.playerWeaponGrowth.TryGetValue(SteamID, out WeaponMasterGrowthData growth);
                if (!isGrowthFound) {
                    growth = new WeaponMasterGrowthData();
                    growth.data = new float[masteryStats.Length];
                    for (int i = 0; i < growth.data.Length; i++) {
                        growth.data[i] = 1.0f;
                    }
                    Database.playerWeaponGrowth[SteamID] = growth;
                }
                weaponGrowth = growth.data[(int)WeaponType+1];
                spellGrowth = growth.data[0];
            }

            int MasteryValue = (int)(MasteryCombatTick * MasteryMultiplier * weaponGrowth);
            int spellMasteryValue = (int)(MasteryCombatTick * MasteryMultiplier * spellGrowth);
            Cache.player_combat_ticks[SteamID] += 1;
            
            SetMastery(SteamID, (int)WeaponType+1, MasteryValue);
            if(WeaponType == WeaponType.None || !spellMasteryNeedsNoneToLearn) {
                SetMastery(SteamID, 0, spellMasteryValue);

            }
        }

        public static void DecayMastery(Entity userEntity)
        {
            User Data = em.GetComponentData<User>(userEntity);
            var SteamID = Data.PlatformId;
            if (Database.player_decaymastery_logout.TryGetValue(SteamID, out var LastDecay)) {
                TimeSpan elapsed_time = DateTime.Now - LastDecay;
                if (elapsed_time.TotalSeconds < DecayInterval) return;

                int DecayTicks = (int)Math.Floor(elapsed_time.TotalSeconds / DecayInterval);
                if (DecayTicks > 0)
                {
                    int DecayValue = Offline_DecayValue * DecayTicks *-1;

                    Output.SendLore(userEntity, Plugin.getTranslation("You've been sleeping for ")+(int)elapsed_time.TotalMinutes + Plugin.getTranslation(" minute(s). Your mastery has decayed by ")+DecayValue * 0.001 + Plugin.getTranslation("%"));

                    for(int i = 0; i < masteryStats.Length; i++){
                        SetMastery(SteamID, i, DecayValue);
                    }
                }
            }
        }

        public static void BuffReceiver(Entity buffEntity, PrefabGUID GUID)
        {
            if (!GUID.Equals(Database.Buff.OutofCombat) && !GUID.Equals(Database.Buff.InCombat) && !GUID.Equals(Database.Buff.InCombat_PvP)) return;

            var Owner = em.GetComponentData<EntityOwner>(buffEntity).Owner;
            if (!em.HasComponent<PlayerCharacter>(Owner)) return;

            var userEntity = em.GetComponentData<PlayerCharacter>(Owner).UserEntity._Entity;
            var SteamID = em.GetComponentData<User>(userEntity).PlatformId;

            var Buffer = em.GetBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
            BuffReceiver(Buffer, Owner, SteamID);
        }

        public static void BuffReceiver(DynamicBuffer<ModifyUnitStatBuff_DOTS> Buffer, Entity Owner, ulong SteamID)
        {
            WeaponType WeaponType = GetWeaponType(Owner);
            if(WeaponType < 0 || (int)WeaponType >= masteryStats.Length-1){
                return;
            }
            var isMastered = ConvertMastery(SteamID, WeaponType, out var PMastery, out var SMastery);
            
            if (isMastered)
            {
                applyBuff(Buffer, (int)WeaponType+1, PMastery, SteamID);
                if(WeaponType == WeaponType.None || !spellMasteryNeedsNoneToUse){
                    applyBuff(Buffer, 0, SMastery, SteamID);
                }                 
            }
        }

        private static void applyBuff(DynamicBuffer<ModifyUnitStatBuff_DOTS> Buffer, int type, float mastery, ulong SteamID){
            for (int i = 0; i < masteryStats[type].Length; i++)
            {
                float value = calcBuffValue(type, mastery, SteamID, i);
                var modType = ModificationType.Add;
                if ((UnitStatType)masteryStats[type][i] == UnitStatType.CooldownModifier)
                {
                    value = 1.0f - value;
                    modType = ModificationType.Set;
                    if (CDRStacks)
                    {
                        modType = ModificationType.Multiply;
                    }
                }
                Buffer.Add(new ModifyUnitStatBuff_DOTS()
                {
                    StatType = (UnitStatType)masteryStats[type][i],
                    Value = value,
                    ModificationType = modType,
                    Id = ModificationId.NewId(0)
                }); ;
            }
        }

        public static float calcBuffValue(int type, float mastery, ulong SteamID, int stat){
            float effectiveness = 1;
            if (Database.playerWeaponEffectiveness.TryGetValue(SteamID, out WeaponMasterEffectivenessData effectivenessData) && effectivenessSubSystemEnabled)
                effectiveness = effectivenessData.data[type];
            effectiveness = Math.Max(1.0f, effectiveness);
            if (type >= masteryRates.Length){
                if (Helper.FindPlayer(SteamID, true, out var targetEntity, out var targetUserEntity))
                    Output.SendLore(targetUserEntity, Plugin.getTranslation("Type ")+type+Plugin.getTranslation(" out of bounds for masteryRates, max of ")+(masteryRates.Length - 1));
                return 0.0f;
            }
            if (stat >= masteryRates[type].Length){
                if (Helper.FindPlayer(SteamID, true, out var targetEntity, out var targetUserEntity))
                    Output.SendLore(targetUserEntity, Plugin.getTranslation("stat ")+stat + Plugin.getTranslation(" out of bounds for masteryRates for type ") +type + Plugin.getTranslation(", max of ") +(masteryRates[type].Length - 1));
                return 0.0f;
            }
            if (type >= masteryStats.Length){
                if (Helper.FindPlayer(SteamID, true, out var targetEntity, out var targetUserEntity))
                    Output.SendLore(targetUserEntity, Plugin.getTranslation("Type ")+type + Plugin.getTranslation(" out of bounds for masteryStats, max of ")+(masteryStats.Length - 1));
                return 0.0f;
            }
            if (stat >= masteryStats[type].Length){
                if (Helper.FindPlayer(SteamID, true, out var targetEntity, out var targetUserEntity))
                    Output.SendLore(targetUserEntity, Plugin.getTranslation("stat ")+stat + Plugin.getTranslation(" out of bounds for masteryStats for type ")+type + Plugin.getTranslation(", max of ")+(masteryStats[type].Length - 1));
                return 0.0f;
            }
            float value = mastery * masteryRates[type][stat] * effectiveness;
            if ((UnitStatType)masteryStats[type][stat] == UnitStatType.CooldownModifier){
                if (linearCDR){
                    value = mastery * effectiveness;
                    value = value / (value + masteryRates[type][stat]);
                }
                else{
                    value = (mastery*effectiveness)/(masteryRates[type][stat]*2);
                }
            }
            return value;
        }

        public static bool ConvertMastery(ulong SteamID, WeaponType weaponType, out float MasteryValue, out float MasterySpellValue){
            MasteryValue = 0;
            MasterySpellValue = 0;

            bool isFound = Database.player_weaponmastery.TryGetValue(SteamID, out WeaponMasterData Mastery);
            if (!isFound) return false;

            MasteryValue = Mastery.data[(int)weaponType + 1];
            MasterySpellValue = Mastery.data[0];

            if (MasteryValue > 0) MasteryValue = (float)(MasteryValue * 0.001);
            if (MasterySpellValue > 0) MasterySpellValue = (float)(MasterySpellValue * 0.001);
            return true;
        }

        public static void SetMastery(ulong SteamID, int Type, int Value)
        {
            int NoneExpertise = 0;
            if (Type == (int)WeaponType.None+1){
                if (Value > 0) Value = Value * 2;
            }
            WeaponMasterData Mastery;
            try {
                bool isPlayerFound = Database.player_weaponmastery.TryGetValue(SteamID, out Mastery);
                Mastery.data[Type] += Value;
                Mastery.data[Type] = Math.Min(Mastery.data[Type], MaxMastery);
            }
            catch (NullReferenceException e) {
                Mastery = new WeaponMasterData();
                Mastery.data = new int[masteryStats.Length];
                for (int i = 0; i < Mastery.data.Length; i++) {
                    Mastery.data[i] = 0;
                }

                if (NoneExpertise < 0) NoneExpertise = 0;
                if (Value < 0) Value = 0;
                Mastery.data[Type] += Value;
            }
            if(Mastery.data[Type] < 0) {
                Mastery.data[Type] = 0;
            }
            Database.player_weaponmastery[SteamID] = Mastery;
            return;
        }


        public static void resetMastery(ulong SteamID, int type) {
            if (!effectivenessSubSystemEnabled) {
                if (Helper.FindPlayer(SteamID, true, out var targetEntity, out var targetUserEntity)) {
                    Output.SendLore(targetUserEntity, Plugin.getTranslation("Effectiveness Subsystem disabled, not resetting mastery."));
                }
                return;
            }
            bool isPlayerFound = Database.player_weaponmastery.TryGetValue(SteamID, out WeaponMasterData Mastery);
            if (isPlayerFound) {
                if (type < 0) {
                    for (int i = 0; i < Mastery.data.Length; i++) {
                        addMasteryEffectiveness(SteamID, i, (float)Mastery.data[i] / 100000.0f);
                        adjustGrowth(SteamID, i, (float)Mastery.data[i] / 100000.0f);
                        Mastery.data[i] = 0;
                    }
                    
                }
                else {
                    addMasteryEffectiveness(SteamID, type, (float)Mastery.data[type] / 100000.0f);
                    adjustGrowth(SteamID, type, (float)Mastery.data[type] / 100000.0f);
                    Mastery.data[type] = 0;
                }
                Database.player_weaponmastery[SteamID] = Mastery;
            }
            return;
        }


        public static void adjustGrowth(ulong SteamID, int type, float value) {
            bool isPlayerFound = Database.playerWeaponGrowth.TryGetValue(SteamID, out WeaponMasterGrowthData growth);
            if (isPlayerFound) {
                if (type >= 0 && type < growth.data.Length) {
                    if(growthPerEfficency >= 0) {
                        growth.data[type] = Math.Min(maxGrowth, growth.data[type] + (value*growthPerEfficency));
                    }
                    else {
                        float gpe = -1 * growthPerEfficency;
                        value = value / (value + growthPerEfficency);
                        growth.data[type] = Math.Max(minGrowth, growth.data[type] * value );
                    }
                }
            }
            else {
                growth = new WeaponMasterGrowthData();
                growth.data = new float[masteryStats.Length];
                for (int i = 0; i < growth.data.Length; i++) {
                    growth.data[i] = 1;
                }
                if (type >= 0 && type < growth.data.Length) {
                    if (growthPerEfficency >= 0) {
                        growth.data[type] = Math.Min(maxGrowth, growth.data[type] + (value * growthPerEfficency));
                    }
                    else {
                        float gpe = -1 * growthPerEfficency;
                        value = value / (value + growthPerEfficency);
                        growth.data[type] = Math.Max(minGrowth, growth.data[type] * value);
                    }
                }

            }
            Database.playerWeaponGrowth[SteamID] = growth;
            return;
        }

        public static void addMasteryEffectiveness(ulong SteamID, int type, float value) {
            bool isPlayerFound = Database.playerWeaponEffectiveness.TryGetValue(SteamID, out WeaponMasterEffectivenessData effectiveness);
            if (isPlayerFound) {
                if (type >= 0 && type < effectiveness.data.Length) {
                    effectiveness.data[type] = Math.Min(maxEffectiveness, effectiveness.data[type] + value);
                }
            }
            else {
                effectiveness = new WeaponMasterEffectivenessData();
                effectiveness.data = new float[masteryStats.Length];
                for(int i = 0; i < effectiveness.data.Length; i++) {
                    effectiveness.data[i] = 1;
                }
                if (type >= 0 && type < effectiveness.data.Length) {
                    effectiveness.data[type] = Math.Min(maxEffectiveness, effectiveness.data[type] + value);
                }

            }
            Database.playerWeaponEffectiveness[SteamID] = effectiveness;
            return;
        }

        public static WeaponType GetWeaponType(Entity Player)
        {
            Entity WeaponEntity = em.GetComponentData<Equipment>(Player).WeaponSlotEntity._Entity;
            WeaponType WeaponType = WeaponType.None;
            if (em.HasComponent<EquippableData>(WeaponEntity))
            {
                EquippableData WeaponData = em.GetComponentData<EquippableData>(WeaponEntity);
                WeaponType = WeaponData.WeaponType;
            }
            return WeaponType;
        }

        public static string typeToName(int type)
        {
            type -= 1;
            string weaponName = Plugin.getTranslation("Unknown");
            switch (type)
            {
                case (int)WeaponType.None:
                    weaponName = Plugin.getTranslation("Unarmed");
                    break;
                case (int)WeaponType.Spear:
                    weaponName = Plugin.getTranslation("Spear");
                    break;
                case (int)WeaponType.Sword:
                    weaponName = Plugin.getTranslation("Sword");
                    break;
                case (int)WeaponType.Scythe:
                    weaponName = Plugin.getTranslation("Scythe");
                    break;
                case (int)WeaponType.Crossbow:
                    weaponName = Plugin.getTranslation("Crossbow");
                    break;
                case (int)WeaponType.Mace:
                    weaponName = Plugin.getTranslation("Mace");
                    break;
                case (int)WeaponType.Slashers:
                    weaponName = Plugin.getTranslation("Slashers");
                    break;
                case (int)WeaponType.Axes:
                    weaponName = Plugin.getTranslation("Axes");
                    break;
                case (int)WeaponType.FishingPole:
                    weaponName = Plugin.getTranslation("Fishing Rod");
                    break;
                case -1:
                    weaponName = Plugin.getTranslation("Spell");
                    break;
                case -2:
                    weaponName = Plugin.getTranslation("All");
                    break;
            }
            return weaponName;
        }

        public static int masteryDataByType(int type, ulong SteamID)
        {
            bool isFound = Database.player_weaponmastery.TryGetValue(SteamID, out WeaponMasterData Mastery);
            if (!isFound) return (MaxMastery*-10);
            return Mastery.data[type];
        }

        public static void SaveWeaponMastery()
        {
            File.WriteAllText("BepInEx/config/RPGMods/Saves/weapon_mastery_array.json", JsonSerializer.Serialize(Database.player_weaponmastery, Database.JSON_options));
            File.WriteAllText("BepInEx/config/RPGMods/Saves/mastery_decay.json", JsonSerializer.Serialize(Database.player_decaymastery_logout, Database.JSON_options));
            File.WriteAllText("BepInEx/config/RPGMods/Saves/player_log_mastery.json", JsonSerializer.Serialize(Database.player_log_mastery, Database.JSON_options));
            File.WriteAllText("BepInEx/config/RPGMods/Saves/weapon_Mastery_Effectiveness.json", JsonSerializer.Serialize(Database.playerWeaponEffectiveness, Database.JSON_options));
            File.WriteAllText("BepInEx/config/RPGMods/Saves/weapon_Mastery_Growth.json", JsonSerializer.Serialize(Database.playerWeaponGrowth, Database.JSON_options));
        }

        public static void LoadWeaponMastery() {
            bool update = false;
            bool updateGrowth = false;
            bool updateEfficency = false;
            if (!File.Exists("BepInEx/config/RPGMods/Saves/weapon_mastery_array.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/weapon_mastery_array.json");
                if (File.Exists("BepInEx/config/RPGMods/Saves/weapon_mastery.json")) {
                    update = true;
                    String temp = File.ReadAllText("BepInEx/config/RPGMods/Saves/weapon_mastery.json");
                    Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMastery DB needs updating, is as follows:"));
                    Plugin.Logger.LogWarning(temp);
                    Database.player_weaponmasteryOld = JsonSerializer.Deserialize<Dictionary<ulong, WeaponMasterDataOld>>(temp);
                }
                stream.Dispose();
            }
            string json = File.ReadAllText("BepInEx/config/RPGMods/Saves/weapon_mastery_array.json");
            try
            {
                Database.player_weaponmastery = JsonSerializer.Deserialize<Dictionary<ulong, WeaponMasterData>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMastery DB Populated."));
            }
            catch
            {
                Database.player_weaponmastery = new Dictionary<ulong, WeaponMasterData>();
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMastery DB Created."));
            }

            if (!File.Exists("BepInEx/config/RPGMods/Saves/mastery_decay.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/mastery_decay.json");
                stream.Dispose();
            }
            json = File.ReadAllText("BepInEx/config/RPGMods/Saves/mastery_decay.json");
            try
            {
                Database.player_decaymastery_logout = JsonSerializer.Deserialize<Dictionary<ulong, DateTime>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMasteryDecay DB Populated."));
            }
            catch
            {
                Database.player_decaymastery_logout = new Dictionary<ulong, DateTime>();
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMasteryDecay DB Created."));
            }

            if (!File.Exists("BepInEx/config/RPGMods/Saves/player_log_mastery.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/player_log_mastery.json");
                stream.Dispose();
            }
            json = File.ReadAllText("BepInEx/config/RPGMods/Saves/player_log_mastery.json");
            try
            {
                Database.player_log_mastery = JsonSerializer.Deserialize<Dictionary<ulong, bool>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("Player_LogMastery_Switch DB Populated."));
            }
            catch
            {
                Database.player_log_mastery = new Dictionary<ulong, bool>();
                Plugin.Logger.LogWarning(Plugin.getTranslation("Player_LogMastery_Switch DB Created."));
            }


            if (!File.Exists("BepInEx/config/RPGMods/Saves/weapon_Mastery_Effectiveness.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/weapon_Mastery_Effectiveness.json");
                stream.Dispose();
            }
            json = File.ReadAllText("BepInEx/config/RPGMods/Saves/weapon_Mastery_Effectiveness.json");
            try
            {
                Database.playerWeaponEffectiveness = JsonSerializer.Deserialize<Dictionary<ulong, WeaponMasterEffectivenessData>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMasteryEffectiveness DB Populated."));
                if (update) {
                    updateEfficency = true;
                }
            }
            catch
            {
                Database.playerWeaponEffectiveness = new Dictionary<ulong, WeaponMasterEffectivenessData>();
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMasteryEffectiveness DB Created."));
            }


            if (!File.Exists("BepInEx/config/RPGMods/Saves/weapon_Mastery_Growth.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/weapon_Mastery_Growth.json");
                stream.Dispose();
            }
            json = File.ReadAllText("BepInEx/config/RPGMods/Saves/weapon_Mastery_Growth.json");
            try
            {
                Database.playerWeaponGrowth = JsonSerializer.Deserialize<Dictionary<ulong, WeaponMasterGrowthData>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMasteryGrowth DB Populated."));
                if (update) {
                    updateGrowth = true;
                }
            }
            catch
            {
                Database.playerWeaponGrowth = new Dictionary<ulong, WeaponMasterGrowthData>();
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMasteryGrowth DB Created."));
            }
            if (update) {
                Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMastery DB needs updating"));
                
                foreach (var entry in Database.player_weaponmasteryOld) {
                    if (!Object.ReferenceEquals(entry, null)) {
                        if (!Object.ReferenceEquals(entry.Value, null)) {
                            WeaponMasterData mastery = new WeaponMasterData();
                            if (!Object.ReferenceEquals(mastery, null)) {
                                if (Object.ReferenceEquals(mastery.data, null)) {
                                    mastery.data = new int[masteryStats.Length];
                                }
                                mastery.data[0] = entry.Value.Spell;
                                mastery.data[1] = entry.Value.None;
                                mastery.data[2] = entry.Value.Spear;
                                mastery.data[3] = entry.Value.Sword;
                                mastery.data[4] = entry.Value.Scythe;
                                mastery.data[5] = entry.Value.Crossbow;
                                mastery.data[6] = entry.Value.Mace;
                                mastery.data[7] = entry.Value.Slashers;
                                mastery.data[8] = entry.Value.Axes;
                                mastery.data[9] = entry.Value.FishingPole;
                                Database.player_weaponmastery[entry.Key] = mastery;
                            }
                            Plugin.Logger.LogWarning(Plugin.getTranslation("WeaponMastery DB transitioned for ") + entry.Key);
                        }
                    }
                }
            }
            if (updateEfficency) {
                foreach (var entry in Database.playerWeaponEffectiveness) {
                    float spell = entry.Value.data[entry.Value.data.Length-1];
                    for(int i = entry.Value.data.Length-1; i > 0; i--) {
                        entry.Value.data[i] = entry.Value.data[i-1];
                    }
                    entry.Value.data[0] = spell;
                    Plugin.Logger.LogWarning(Plugin.getTranslation("mastery efficency DB transitioned for ") + entry.Key);
                }
            }
            if (updateGrowth) {
                foreach (var entry in Database.playerWeaponGrowth) {
                    float spell = entry.Value.data[entry.Value.data.Length - 1];
                    for (int i = entry.Value.data.Length-1; i > 0; i--) {
                        entry.Value.data[i] = entry.Value.data[i - 1];
                    }
                    entry.Value.data[0] = spell;
                    Plugin.Logger.LogWarning(Plugin.getTranslation("mastery growth DB transitioned for ") + entry.Key);
                }
            }
            if (update) {
                SaveWeaponMastery();
            }
        }
    }
}
