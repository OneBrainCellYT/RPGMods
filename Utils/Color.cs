namespace RPGMods.Utils
{
    public class Color
    {
        private static string ColorText(string color, string text)
        {
            return $Plugin.getTranslation("<color={color}>") + text + Plugin.getTranslation("</color>");
        }

        public static string White(string text)
        {
            return ColorText(Plugin.getTranslation("#fffffffe"), text);
        }
        public static string Black(string text)
        {
            return ColorText(Plugin.getTranslation("#000000"), text);
        }
        public static string Gray(string text)
        {
            return ColorText(Plugin.getTranslation("#404040"), text);
        }
        public static string Orange(string text)
        {
            return ColorText(Plugin.getTranslation("#c98332"), text);
        }
        public static string Yellow(string text)
        {
            return ColorText(Plugin.getTranslation("#cfc14a"), text);
        }
        public static string Green(string text)
        {
            return ColorText(Plugin.getTranslation("#56ad3b"), text);
        }
        public static string Teal(string text)
        {
            return ColorText(Plugin.getTranslation("#3b8dad"), text);
        }
        public static string Blue(string text)
        {
            return ColorText(Plugin.getTranslation("#3444a8"), text);
        }
        public static string Purple(string text)
        {
            return ColorText(Plugin.getTranslation("#8b3691"), text);
        }
        public static string Pink(string text)
        {
            return ColorText(Plugin.getTranslation("#b53c8ffe"), text);
        }
        public static string Red(string text)
        {
            return ColorText(Plugin.getTranslation("#ff0000"), text);
        }
        public static string SoftRed(string text)
        {
            return ColorText(Plugin.getTranslation("#b53c40"), text);
        }
    }
}
