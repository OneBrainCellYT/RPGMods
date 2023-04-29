using RPGMods.Utils;

namespace RPGMods.Commands
{
    [Command(Plugin.getTranslation("shutdown, quit, exit"), Usage = Plugin.getTranslation("shutdown"), Description = Plugin.getTranslation("Trigger the exit signal & shutdown the server."))]
    public static class Shutdown
    {
        public static void Initialize(Context ctx)
        {
            UnityEngine.Application.Quit();
        }
    }
}
