using ProjectM;
using RPGMods.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Unity.Entities;

namespace RPGMods.Systems
{
    public static class WorldDynamicsSystem
    {
        public static EntityManager em = Plugin.Server.EntityManager;
        public static bool isFactionDynamic = false;

        private static bool loopInProgress = false;
        public static bool growOnKill = false;
        public static void OnDayCycle()
        {
            if (Plugin.isInitialized == false || loopInProgress == true) return;

            loopInProgress = true;
            if (Object.ReferenceEquals(Database.FactionStats, null)){ Plugin.Logger.LogWarning(Plugin.getTranslation("Faction Stats loaded as Null, check the json file.")); return;}
            foreach (var faction in Database.FactionStats)
            {
                if (faction.Value.Active == false) continue;

                var factionStats = faction.Value;
                if(factionStats.RequiredPower < 0) {
                    Plugin.Logger.LogWarning(factionStats.Name + Plugin.getTranslation("'s required power to levelup is negative, fixing it now."));
                    factionStats.RequiredPower *= -1;
                    Database.FactionStats[faction.Key] = factionStats;
                }
                if (growOnKill){
                    factionStats.StoredPower -= factionStats.DailyPower;
                }
                else{
                    //-- Calculate total stored power.
                    factionStats.StoredPower += factionStats.ActivePower;
                    //-- Reset the active power.
                    factionStats.ActivePower = factionStats.DailyPower;
                }

                //-- Calculate if faction level should change.
                checkForLevelup(factionStats, faction.Key);

                //Database.FactionStats[faction.Key] = factionStats;
            }
            loopInProgress = false;
        }

        public static void checkForLevelup(FactionData factionStats, int factionKey){
            if (factionStats.StoredPower >= factionStats.RequiredPower){
                factionStats.Level += 1;
                factionStats.StoredPower = 0;
                if (factionStats.Level > factionStats.MaxLevel) factionStats.Level = factionStats.MaxLevel;
            }
            else if (factionStats.StoredPower < 0){
                factionStats.Level -= 1;
                if (growOnKill){
                    factionStats.StoredPower = factionStats.RequiredPower + factionStats.StoredPower;
                }
                else{
                    factionStats.StoredPower = 0;
                }
                if (factionStats.Level < factionStats.MinLevel) factionStats.Level = factionStats.MinLevel;
            }
            Database.FactionStats[factionKey] = factionStats;

        }

        public static void MobKillMonitor(Entity entity){
            if (!em.HasComponent<FactionReference>(entity)) return;

            var factionID = em.GetComponentData<FactionReference>(entity).FactionGuid._Value.GetHashCode();
            if (Object.ReferenceEquals(Database.FactionStats, null)){ Plugin.Logger.LogWarning(Plugin.getTranslation("Faction Stats loaded as Null, check the json file.")); return;}
            if (!Database.FactionStats.TryGetValue(factionID, out FactionData factionStats)) return;

            if (factionStats.Active == false) return;

            if (growOnKill) {
                factionStats.StoredPower += 1;
            }
            else {
                factionStats.ActivePower -= 1;
            }
            checkForLevelup(factionStats, factionID);
            //Database.FactionStats[factionID] = factionStats;
        }

        public static void MobReceiver(Entity entity)
        {
            if (!em.HasComponent<UnitLevel>(entity) && !em.HasComponent<UnitStats>(entity)) return;

            var mobGUID = em.GetComponentData<PrefabGUID>(entity);
            if (Database.IgnoredMonstersGUID.Contains(mobGUID)) return;

            var factionID = em.GetComponentData<FactionReference>(entity).FactionGuid._Value.GetHashCode();
            if (Object.ReferenceEquals(Database.FactionStats, null)){ Plugin.Logger.LogWarning(Plugin.getTranslation("Faction Stats loaded as Null, check the json file.")); return;}
            if (!Database.FactionStats.TryGetValue(factionID, out var factionStats)) return;

            if (factionStats.Active == false) return;

            if (factionStats.Level == 0) return;

            if (factionStats.Level > factionStats.MaxLevel) factionStats.Level = factionStats.MaxLevel;
            else if (factionStats.Level < factionStats.MinLevel) factionStats.Level = factionStats.MinLevel;

            //-- Unit Buffers for stats modification
            var floatBuffer = em.GetBuffer<FloatModificationBuffer>(entity);
            var boolBuffer = em.GetBuffer<BoolModificationBuffer>(entity);
            var intBuffer = em.GetBuffer<IntModificationBuffer>(entity);

            //-- Unit Stats
            var unitHealth = em.GetComponentData<Health>(entity);
            var unitStats = em.GetComponentData<UnitStats>(entity);
            var unitLevel = em.GetComponentData<UnitLevel>(entity);

            //-- Calculate Modifications
            int Level = (int) Math.Ceiling((float)factionStats.FactionBonus.Level_Int * factionStats.Level);
            float HP = (float) Math.Ceiling((float)factionStats.FactionBonus.HP_Float * factionStats.Level);
            float PhysicalPower = (float)Math.Ceiling((float)factionStats.FactionBonus.PhysicalPower_Float * factionStats.Level);
            float PhysicalResistance = (float)Math.Ceiling((float)factionStats.FactionBonus.PhysicalResistance_Float * factionStats.Level);
            float PhysicalCriticalStrikeChance = (float)Math.Ceiling((float)factionStats.FactionBonus.PhysicalCriticalStrikeChance_Float * factionStats.Level);
            float PhysicalCriticalStrikeDamage = (float)Math.Ceiling((float)factionStats.FactionBonus.PhysicalCriticalStrikeDamage_Float * factionStats.Level);
            float SpellPower = (float)Math.Ceiling((float)factionStats.FactionBonus.SpellPower_Float * factionStats.Level);
            float SpellResistance = (float)Math.Ceiling((float)factionStats.FactionBonus.SpellResistance_Float * factionStats.Level);
            float SpellCriticalStrikeChance = (float)Math.Ceiling((float)factionStats.FactionBonus.SpellCriticalStrikeChance_Float * factionStats.Level);
            float SpellCriticalStrikeDamage = (float)Math.Ceiling((float)factionStats.FactionBonus.SpellCriticalStrikeDamage_Float * factionStats.Level);
            float DamageVsPlayerVampires = (float)Math.Ceiling((float)factionStats.FactionBonus.DamageVsPlayerVampires_Float * factionStats.Level);
            float ResistVsPlayerVampires = (float)Math.Ceiling((float)factionStats.FactionBonus.ResistVsPlayerVampires_Float * factionStats.Level);
            int FireResistance = (int)Math.Ceiling((float)factionStats.FactionBonus.FireResistance_Int * factionStats.Level);

            //-- Do Modifications
            if (Level != 0)
            {
                unitLevel.Level += Level;
                em.SetComponentData(entity, unitLevel);
            }

            if (HP != 0)
            {
                unitHealth.MaxHealth.Set(unitHealth.MaxHealth._Value + HP, floatBuffer);
                unitHealth.Value = unitHealth.MaxHealth._Value + HP;
                em.SetComponentData(entity, unitHealth);
            }

            if (PhysicalPower != 0) unitStats.PhysicalPower.Set(unitStats.PhysicalPower._Value + PhysicalPower, floatBuffer);
            if (PhysicalResistance != 0) unitStats.PhysicalResistance.Set(PhysicalResistance, floatBuffer);
            if (PhysicalCriticalStrikeChance != 0) unitStats.PhysicalCriticalStrikeChance.Set(unitStats.PhysicalCriticalStrikeChance._Value + PhysicalCriticalStrikeChance, floatBuffer);
            if (PhysicalCriticalStrikeDamage != 0) unitStats.PhysicalCriticalStrikeDamage.Set(unitStats.PhysicalCriticalStrikeDamage._Value + PhysicalCriticalStrikeDamage, floatBuffer);
            if (SpellPower != 0) unitStats.SpellPower.Set(unitStats.SpellPower._Value + SpellPower, floatBuffer);
            if (SpellResistance != 0) unitStats.SpellResistance.Set(SpellResistance, floatBuffer);
            if (SpellCriticalStrikeChance != 0) unitStats.SpellCriticalStrikeChance.Set(unitStats.SpellCriticalStrikeChance._Value + SpellCriticalStrikeChance, floatBuffer);
            if (SpellCriticalStrikeDamage != 0) unitStats.SpellCriticalStrikeDamage.Set(unitStats.SpellCriticalStrikeDamage._Value + SpellCriticalStrikeDamage, floatBuffer);
            if (DamageVsPlayerVampires != 0) unitStats.DamageVsPlayerVampires.Set(DamageVsPlayerVampires, floatBuffer);
            if (ResistVsPlayerVampires != 0) unitStats.ResistVsPlayerVampires.Set(ResistVsPlayerVampires, floatBuffer);
            if (FireResistance != 0) unitStats.FireResistance.Set(unitStats.FireResistance._Value + FireResistance, intBuffer);
            unitStats.PvPProtected.Set(false, boolBuffer);
            em.SetComponentData(entity, unitStats);
        }

        public static void SaveFactionStats()
        {
            File.WriteAllText(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/factionstats.json"), JsonSerializer.Serialize(Database.FactionStats, Database.Pretty_JSON_options));
        }

        public static void SaveIgnoredMobs()
        {
            File.WriteAllText(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/ignoredmonsters.json"), JsonSerializer.Serialize(Database.IgnoredMonsters, Database.Pretty_JSON_options));
        }

        public static void LoadIgnoredMobs()
        {
            if (!File.Exists(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/ignoredmonsters.json")))
            {
                var stream = File.Create(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/ignoredmonsters.json"));
                stream.Dispose();
            }
            string content = File.ReadAllText(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/ignoredmonsters.json"));
            try
            {
                Database.IgnoredMonsters = JsonSerializer.Deserialize<HashSet<string>>(content);
                Database.IgnoredMonstersGUID = new HashSet<PrefabGUID>();
                foreach (var item in Database.IgnoredMonsters)
                {
                    if (Database.database_units.TryGetValue(item, out var GUID))
                    {
                        Database.IgnoredMonstersGUID.Add(GUID);
                    }
                }
                Plugin.Logger.LogWarning(Plugin.getTranslation("IgnoredMonsters DB Populated."));
            }
            catch
            {
                Database.IgnoredMonsters = new HashSet<string>();
                Database.IgnoredMonstersGUID = new HashSet<PrefabGUID>();

                Database.IgnoredMonsters.Add(Plugin.getTranslation("CHAR_Undead_Banshee"));
                Database.IgnoredMonstersGUID.Add(Database.database_units[Plugin.getTranslation("CHAR_Undead_Banshee")]);

                SaveIgnoredMobs();

                Plugin.Logger.LogWarning(Plugin.getTranslation("IgnoredMonsters DB Created."));
            }
        }

        public static void LoadFactionStats()
        {
            if (!File.Exists(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/factionstats.json")))
            {
                var stream = File.Create(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/factionstats.json"));
                stream.Dispose();
            }
            string content = File.ReadAllText(Plugin.getTranslation("BepInEx/config/RPGMods/Saves/factionstats.json"));
            try
            {
                Database.FactionStats = JsonSerializer.Deserialize<ConcurrentDictionary<int, FactionData>>(content);
                Plugin.Logger.LogWarning(Plugin.getTranslation("FactionStats DB Populated."));
            }
            catch
            {
                Database.FactionStats = new ConcurrentDictionary<int, FactionData>();
                Database.FactionStats.TryAdd(-1632475814, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Ashfolk"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(-413163549, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Bandits"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1344481611, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Bear"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1094603131, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_ChurchOfLum"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(2395673, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_ChurchOfLum_SpotShapeshiftVampire"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(10678632, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Critters"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1522496317, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Cursed"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1513046884, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Elementals"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1731533561, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Harpy"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1057375699, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Militia"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(1597367490, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_NatureSpirit"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(-1414061934, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Plants"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(-1632009503, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Spiders"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(887347866, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Traders"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(929074293, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Undead"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(2120169232, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_VampireHunters"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(-2024618997, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Werewolf"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });

                Database.FactionStats.TryAdd(-1671358863, new FactionData()
                {
                    Name = Plugin.getTranslation("Faction_Wolves"),
                    Active = false,
                    Level = 0,
                    MaxLevel = 0,
                    MinLevel = 0,
                    ActivePower = 0,
                    StoredPower = 0,
                    DailyPower = 0,
                    RequiredPower = 0,
                    FactionBonus = new StatsBonus()
                    {
                        Level_Int = 0,
                        HP_Float = 0,
                        PhysicalPower_Float = 0,
                        PhysicalResistance_Float = 0,
                        PhysicalCriticalStrikeChance_Float = 0,
                        PhysicalCriticalStrikeDamage_Float = 0,
                        SpellPower_Float = 0,
                        SpellResistance_Float = 0,
                        SpellCriticalStrikeChance_Float = 0,
                        SpellCriticalStrikeDamage_Float = 0,
                        DamageVsPlayerVampires_Float = 0,
                        ResistVsPlayerVampires_Float = 0,
                        FireResistance_Int = 0,
                    }
                });
                SaveFactionStats();
                Plugin.Logger.LogWarning(Plugin.getTranslation("FactionStats DB Created."));
            }
        }
    }
}
