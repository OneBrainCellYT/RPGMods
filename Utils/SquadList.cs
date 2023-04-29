﻿using ProjectM;
using RPGMods.Systems;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace RPGMods.Utils
{
    public class SquadList
    {
        public static Dictionary<int, string> bandit_units = new Dictionary<int, string>()
        {
            { 1, Plugin.getTranslation("CHAR_Bandit_Bomber") },
            { 2, Plugin.getTranslation("CHAR_Bandit_Deadeye") },
            { 3, Plugin.getTranslation("CHAR_Bandit_Hunter") },
            { 4, Plugin.getTranslation("CHAR_Bandit_Mugger") },
            { 5, Plugin.getTranslation("CHAR_Bandit_Stalker") },
            { 6, Plugin.getTranslation("CHAR_Bandit_Thief") },
            { 7, Plugin.getTranslation("CHAR_Bandit_Thug") },
            { 8, Plugin.getTranslation("CHAR_Bandit_Trapper") },
            { 9, Plugin.getTranslation("CHAR_Bandit_Wolf") }
        };
        public static Dictionary<int, string> militia_units = new Dictionary<int, string>()
        {
            { 1, Plugin.getTranslation("CHAR_Militia_Bomber") },
            { 2, Plugin.getTranslation("CHAR_Militia_Crossbow") },
            { 3, Plugin.getTranslation("CHAR_Militia_Devoted") },
            { 4, Plugin.getTranslation("CHAR_Militia_Guard") },
            { 5, Plugin.getTranslation("CHAR_Militia_Heavy") },
            { 6, Plugin.getTranslation("CHAR_Militia_Hound") },
            { 7, Plugin.getTranslation("CHAR_Militia_Light") },
            { 8, Plugin.getTranslation("CHAR_Militia_Longbowman") },
            { 9, Plugin.getTranslation("CHAR_Militia_Torchbearer") },
            { 10, Plugin.getTranslation("CHAR_Farmlands_Nun") }
        };
        public static Dictionary<int, string> soldier_units = new Dictionary<int, string>()
        {
            { 1, Plugin.getTranslation("CHAR_Town_Archer") },
            { 2, Plugin.getTranslation("CHAR_Town_CardinalAide") },
            { 3, Plugin.getTranslation("CHAR_Town_Footman") },
            { 4, Plugin.getTranslation("CHAR_Town_Knight_2H") },
            { 5, Plugin.getTranslation("CHAR_Town_Knight_Shield") },
            { 6, Plugin.getTranslation("CHAR_Town_Rifleman") },
            { 7, Plugin.getTranslation("CHAR_Militia_Bomber") },
            { 8, Plugin.getTranslation("CHAR_Militia_Torchbearer") }
        };
        public static Dictionary<int, string> holy_units = new Dictionary<int, string>()
        {
            { 1, Plugin.getTranslation("CHAR_Town_Cleric") },
            { 2, Plugin.getTranslation("CHAR_Town_Knight_2H") },
            { 3, Plugin.getTranslation("CHAR_Town_Priest") },
            { 4, Plugin.getTranslation("CHAR_Town_Rifleman") },
            { 5, Plugin.getTranslation("CHAR_Town_Lightweaver") },
            { 6, Plugin.getTranslation("CHAR_Town_Paladin") }
        };
        public static Dictionary<int, string> hunter_units = new Dictionary<int, string>()
        {
            { 1, Plugin.getTranslation("CHAR_Town_Paladin") },
            { 2, Plugin.getTranslation("CHAR_Town_Lightweaver") },
            { 3, Plugin.getTranslation("CHAR_Town_Cleric") },
            { 4, Plugin.getTranslation("CHAR_Town_Knight_2H") }
        };
        public static Dictionary<int, string> hunter_leader = new Dictionary<int, string>()
        {
            { 1, Plugin.getTranslation("CHAR_VHunter_Leader_VBlood") },
            { 2, Plugin.getTranslation("CHAR_VHunter_Jade_VBlood") },
            { 3, Plugin.getTranslation("CHAR_Paladin_DivineAngel") }
        };

        private static EntityManager entityManager = Plugin.Server.EntityManager;
        private static Entity empty_entity = new Entity();
        private static string unitName = Plugin.getTranslation("CHAR_Banding_Thug");
        private static System.Random generate = new System.Random();
        public static void SpawnSquad(Entity player, int group, int maxUnits)
        {
            int total_units = 0;

            var UniqueDuration = HunterHuntedSystem.ambush_despawn_timer+0.1231f;

            var f3pos = entityManager.GetComponentData<LocalToWorld>(player).Position;
            while (total_units < maxUnits)
            {
                int unitSpawn = generate.Next(1, maxUnits - total_units);
                if (unitSpawn > 3) unitSpawn = 3;

                if (group == 0) bandit_units.TryGetValue(generate.Next(1, bandit_units.Count), out unitName);
                else if (group == 1) militia_units.TryGetValue(generate.Next(1, militia_units.Count), out unitName);
                else if (group == 2) soldier_units.TryGetValue(generate.Next(1, soldier_units.Count), out unitName);
                else if (group == 3) holy_units.TryGetValue(generate.Next(1, holy_units.Count), out unitName);
                else if (group == 4) hunter_units.TryGetValue(generate.Next(1, hunter_units.Count), out unitName);
                else if (group == 5)
                {
                    hunter_leader.TryGetValue(generate.Next(1, hunter_leader.Count), out unitName);
                    unitSpawn = 1;
                }

                var isFound = Database.database_units.TryGetValue(unitName, out var unit);
                if (isFound)
                {
                    Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, unit, f3pos, unitSpawn, 1, 5, HunterHuntedSystem.ambush_despawn_timer);
                    total_units = total_units + unitSpawn;
                }
            }
        }
    }
}
