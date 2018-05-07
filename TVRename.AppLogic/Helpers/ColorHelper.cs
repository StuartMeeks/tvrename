using System;
using System.Drawing;

namespace TVRename.AppLogic.Helpers
{
    public static class ColorHelper
    {
        public static Color WarningColor() => Color.FromArgb(255, 210, 210);

        public static string TranslateColorToHtml(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
