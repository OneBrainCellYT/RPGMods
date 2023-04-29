using RPGMods.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RPGMods.Commands
{
    [Command(Plugin.getTranslation("help, h"), Usage = Plugin.getTranslation("help [<command>]"), Description = Plugin.getTranslation("Shows a list of commands, or details about a command."), ReqPermission = 0)]
    public static class Help
    {
        public static void Initialize(Context ctx)
        {
            List<string> commands = new List<string>();
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0).ToArray();
            try
            {
                if (types.Any(x => x.GetAttributeValue((CommandAttribute cmd) => cmd.Aliases.First() == ctx.Args[0].ToLower())))
                {
                    var type = types.First(x => x.GetAttributeValue((CommandAttribute cmd) => cmd.Aliases.First() == ctx.Args[0].ToLower()));

                    List<string> aliases = type.GetAttributeValue((CommandAttribute cmd) => cmd.Aliases);
                    if (CommandHandler.DisabledCommands.Split(',').Any(x => x.ToLower() == aliases.First().ToLower()))
                    {
                        Output.SendSystemMessage(ctx, $Plugin.getTranslation("Specified command not found."));
                        return;
                    }
                    string usage = type.GetAttributeValue((CommandAttribute cmd) => cmd.Usage);
                    string description = type.GetAttributeValue((CommandAttribute cmd) => cmd.Description);
                    if (!Database.command_permission.TryGetValue(aliases[0], out var reqPermission)) reqPermission = 100;
                    if (!Database.user_permission.TryGetValue(ctx.Event.User.PlatformId, out var userPermission)) userPermission = 0;

                    if (userPermission < reqPermission && !ctx.Event.User.IsAdmin)
                    {
                        Output.SendSystemMessage(ctx, $Plugin.getTranslation("Specified command not found."));
                        return;
                    }
                    Output.SendSystemMessage(ctx, $Plugin.getTranslation("Help for <color=#00ff00>{ctx.Prefix}{aliases.First()}</color>"));
                    Output.SendSystemMessage(ctx, $Plugin.getTranslation("<color=#fffffffe>Aliases: {string.Join("), Plugin.getTranslation(", aliases)}</color>"));
                    Output.SendSystemMessage(ctx, $Plugin.getTranslation("<color=#fffffffe>Description: {description}</color>"));
                    Output.SendSystemMessage(ctx, $Plugin.getTranslation("<color=#fffffffe>Usage: {ctx.Prefix}{usage}</color>"));
                    return;
                }
                else
                {
                    Output.SendSystemMessage(ctx, $Plugin.getTranslation("Specified command not found."));
                    return;
                }
            }
            catch
            {
                Output.SendSystemMessage(ctx, Plugin.getTranslation("List of all commands:"));
                foreach (Type type in types)
                {
                    List<string> aliases = type.GetAttributeValue((CommandAttribute cmd) => cmd.Aliases);
                    if (CommandHandler.DisabledCommands.Split(',').Any(x => x.ToLower() == aliases.First().ToLower())) continue;
                    string description = type.GetAttributeValue((CommandAttribute cmd) => cmd.Description);
                    if (!Database.command_permission.TryGetValue(aliases[0], out var reqPermission)) reqPermission = 100;
                    if (!Database.user_permission.TryGetValue(ctx.Event.User.PlatformId, out var userPermission)) userPermission = 0;

                    string s = "";
                    bool send = false;
                    if (userPermission < reqPermission && ctx.Event.User.IsAdmin)
                    {
                        s = $Plugin.getTranslation("<color=#00ff00>{ctx.Prefix}{string.Join("), Plugin.getTranslation(", aliases)}</color> - <color=#ff0000>[{reqPermission}]</color> <color=#fffffffe>{description}</color>");
                        //s = $Plugin.getTranslation("<color=#00ff00ff>{ctx.Prefix}{aliases.First()}/{string.Join("), Plugin.getTranslation(", aliases)}</color> - <color=#ff0000ff>[ADMIN]</color> <color=#ffffffff>{description}</color>");
                        send = true;
                    }
                    else if (userPermission >= reqPermission)
                    {
                        s = $Plugin.getTranslation("<color=#00ff00>{ctx.Prefix}{string.Join("), Plugin.getTranslation(", aliases)}</color> - <color=#fffffffe>{description}</color>");
                        //s = $Plugin.getTranslation("<color=#00ff00ff>{ctx.Prefix}{aliases.First()}/{string.Join("), Plugin.getTranslation(", aliases)}</color> - <color=#ffffffff>{description}</color>");
                        send = true;
                    }
                    if (send) Output.SendSystemMessage(ctx, s);
                }
            }
        }
    }
}
