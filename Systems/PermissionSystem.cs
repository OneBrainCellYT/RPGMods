using ProjectM;
using ProjectM.Network;
using RPGMods;
using RPGMods.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Entities;

namespace RPGMods.Systems
{
    public static class PermissionSystem
    {
        public static bool isVIPSystem = true;
        public static bool isVIPWhitelist = true;
        public static int VIP_Permission = 10;

        public static double VIP_OutCombat_ResYield = -1.0;
        public static double VIP_OutCombat_DurabilityLoss = -1.0;
        public static double VIP_OutCombat_MoveSpeed = -1.0;
        public static double VIP_OutCombat_GarlicResistance = -1.0;
        public static double VIP_OutCombat_SilverResistance = -1.0;

        public static double VIP_InCombat_ResYield = -1.0;
        public static double VIP_InCombat_DurabilityLoss = -1.0;
        public static double VIP_InCombat_MoveSpeed = -1.0;
        public static double VIP_InCombat_GarlicResistance = -1.0;
        public static double VIP_InCombat_SilverResistance = -1.0;

        private static EntityManager em = Plugin.Server.EntityManager;

        public static bool IsUserVIP(ulong steamID)
        {
            bool isVIP = GetUserPermission(steamID) >= VIP_Permission;
            return isVIP;
        }

        public static int GetUserPermission(ulong steamID)
        {
            bool isExist = Database.user_permission.TryGetValue(steamID, out var permission);
            if (isExist) return permission;
            return 0;
        }

        public static int GetCommandPermission(string command)
        {
            var isExist = Database.command_permission.TryGetValue(command, out int requirement);
            if (isExist) return requirement;
            else
            {
                Database.command_permission[command] = 100;
                SavePermissions();
            }
            return 100;
        }

        public static bool PermissionCheck(ulong steamID, string command)
        {
            bool isAllowed = GetUserPermission(steamID) >= GetCommandPermission(command);
            return isAllowed;
        }

        private static object SendPermissionList(Context ctx, List<string> messages)
        {
            foreach(var m in messages)
            {
                Output.SendSystemMessage(ctx, m);
            }
            return new object();
        }

        public static async Task PermissionList(Context ctx)
        {
            await Task.Yield();

            List<string> messages = new List<string>();

            var SortedPermission = Database.user_permission.ToList();
            SortedPermission.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            var ListPermission = SortedPermission;
            messages.Add(Plugin.getTranslation("==================================="));
            if (ListPermission.Count == 0) messages.Add(Plugin.getTranslation("<color=#fffffffe>No Result</color>"));
            else
            {
                int i = 0;
                foreach (var result in ListPermission)
                {
                    i++;
                    messages.Add(Plugin.getTranslation("{i}. <color=#fffffffe>")+Helper.GetNameFromSteamID(result.Key) + Plugin.getTranslation(" : ")+result.Value + Plugin.getTranslation("</color>"));
                }
            }
            messages.Add(Plugin.getTranslation("==================================="));

            TaskRunner.Start(taskWorld => SendPermissionList(ctx, messages), false);
        }

        public static void BuffReceiver(Entity buffEntity, PrefabGUID GUID)
        {
            if (!GUID.Equals(Database.Buff.OutofCombat) && !em.HasComponent<InCombatBuff>(buffEntity)) return;
            var Owner = em.GetComponentData<EntityOwner>(buffEntity).Owner;
            if (!em.HasComponent<PlayerCharacter>(Owner)) return;

            var userEntity = em.GetComponentData<PlayerCharacter>(Owner).UserEntity._Entity;
            var SteamID = em.GetComponentData<User>(userEntity).PlatformId;

            if (IsUserVIP(SteamID))
            {
                var Buffer = em.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
                //-- Out of Combat Buff
                if (GUID.Equals(Database.Buff.OutofCombat))
                {
                    if (VIP_OutCombat_ResYield > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.ResourceYield,
                            Value = (float)VIP_OutCombat_ResYield,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_OutCombat_DurabilityLoss > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.ReducedResourceDurabilityLoss,
                            Value = (float)VIP_OutCombat_DurabilityLoss,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_OutCombat_MoveSpeed > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.MovementSpeed,
                            Value = (float)VIP_OutCombat_MoveSpeed,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_OutCombat_GarlicResistance > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.GarlicResistance,
                            Value = (float)VIP_OutCombat_GarlicResistance,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_OutCombat_SilverResistance > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.SilverResistance,
                            Value = (float)VIP_OutCombat_SilverResistance,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                }
                //-- In Combat Buff
                else if (em.HasComponent<InCombatBuff>(buffEntity))
                {
                    if (VIP_InCombat_ResYield > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.ResourceYield,
                            Value = (float)VIP_InCombat_ResYield,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_InCombat_DurabilityLoss > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.ReducedResourceDurabilityLoss,
                            Value = (float)VIP_InCombat_DurabilityLoss,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_InCombat_MoveSpeed > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.MovementSpeed,
                            Value = (float)VIP_InCombat_MoveSpeed,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_InCombat_GarlicResistance > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.GarlicResistance,
                            Value = (float)VIP_InCombat_GarlicResistance,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                    if (VIP_InCombat_SilverResistance > 0)
                    {
                        Buffer.Add(new ModifyUnitStatBuff_DOTS()
                        {
                            StatType = UnitStatType.SilverResistance,
                            Value = (float)VIP_InCombat_SilverResistance,
                            ModificationType = ModificationType.Multiply,
                            Id = ModificationId.NewId(0)
                        });
                    }
                }
            }
        }

        public static void SavePermissions()
        {
            File.WriteAllText("BepInEx/config/RPGMods/command_permission.json", JsonSerializer.Serialize(Database.command_permission, Database.Pretty_JSON_options));
        }

        public static void SaveUserPermission()
        {
            File.WriteAllText("BepInEx/config/RPGMods/user_permission.json", JsonSerializer.Serialize(Database.user_permission, Database.Pretty_JSON_options));
        }

        public static void LoadPermissions()
        {
            if (!File.Exists("BepInEx/config/RPGMods/user_permission.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/user_permission.json");
                stream.Dispose();
            }
            string json = File.ReadAllText("BepInEx/config/RPGMods/user_permission.json");
            try
            {
                Database.user_permission = JsonSerializer.Deserialize<Dictionary<ulong, int>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("UserPermissions DB Populated"));
            }
            catch
            {
                Database.user_permission = new Dictionary<ulong, int>();
                Plugin.Logger.LogWarning(Plugin.getTranslation("UserPermission DB Created."));
            }

            if (!File.Exists("BepInEx/config/RPGMods/command_permission.json"))
            {
                FileStream stream = File.Create("BepInEx/config/RPGMods/command_permission.json");
                stream.Dispose();
            }
            json = File.ReadAllText("BepInEx/config/RPGMods/command_permission.json");
            try
            {
                Database.command_permission = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                Plugin.Logger.LogWarning(Plugin.getTranslation("CommandPermissions DB Populated"));
            }
            catch
            {
                Database.command_permission = new Dictionary<string, int>();
                Database.command_permission[Plugin.getTranslation("help")] = 0;
                Database.command_permission[Plugin.getTranslation("ping")] = 0;
                Database.command_permission[Plugin.getTranslation("myinfo")] = 0;
                Database.command_permission[Plugin.getTranslation("pvp")] = 0;
                Database.command_permission[Plugin.getTranslation("pvp_args")] = 100;
                Database.command_permission[Plugin.getTranslation("siege")] = 0;
                Database.command_permission[Plugin.getTranslation("siege_args")] = 100;
                Database.command_permission[Plugin.getTranslation("heat")] = 0;
                Database.command_permission[Plugin.getTranslation("heat_args")] = 100;
                Database.command_permission[Plugin.getTranslation("experience")] = 0;
                Database.command_permission[Plugin.getTranslation("experience_args")] = 100;
                Database.command_permission[Plugin.getTranslation("mastery")] = 0;
                Database.command_permission[Plugin.getTranslation("mastery_args")] = 100;
                Database.command_permission[Plugin.getTranslation("autorespawn")] = 100;
                Database.command_permission[Plugin.getTranslation("autorespawn_args")] = 100;
                Database.command_permission[Plugin.getTranslation("waypoint")] = 100;
                Database.command_permission[Plugin.getTranslation("waypoint_args")] = 100;
                Database.command_permission[Plugin.getTranslation("ban")] = 100;
                Database.command_permission[Plugin.getTranslation("bloodpotion")] = 100;
                Database.command_permission[Plugin.getTranslation("blood")] = 100;
                Database.command_permission[Plugin.getTranslation("customspawn")] = 100;
                Database.command_permission[Plugin.getTranslation("give")] = 100;
                Database.command_permission[Plugin.getTranslation("godmode")] = 100;
                Database.command_permission[Plugin.getTranslation("health")] = 100;
                Database.command_permission[Plugin.getTranslation("kick")] = 100;
                Database.command_permission[Plugin.getTranslation("kit")] = 100;
                Database.command_permission[Plugin.getTranslation("nocooldown")] = 100;
                Database.command_permission[Plugin.getTranslation("permission")] = 100;
                Database.command_permission[Plugin.getTranslation("playerinfo")] = 100;
                Database.command_permission[Plugin.getTranslation("punish")] = 100;
                Database.command_permission[Plugin.getTranslation("rename")] = 100;
                Database.command_permission[Plugin.getTranslation("adminrename")] = 100;
                Database.command_permission[Plugin.getTranslation("resetcooldown")] = 100;
                Database.command_permission[Plugin.getTranslation("save")] = 100;
                Database.command_permission[Plugin.getTranslation("shutdown")] = 100;
                Database.command_permission[Plugin.getTranslation("spawnnpc")] = 100;
                Database.command_permission[Plugin.getTranslation("speed")] = 100;
                Database.command_permission[Plugin.getTranslation("sunimmunity")] = 100;
                Database.command_permission[Plugin.getTranslation("teleport")] = 100;
                Database.command_permission[Plugin.getTranslation("worlddynamics")] = 100;
                SavePermissions();
                Plugin.Logger.LogWarning(Plugin.getTranslation("CommandPermissions DB Created."));
            }
        }
    }
}
