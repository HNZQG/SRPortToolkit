using Microsoft.Xna.Framework;
using Hacknet;
using Hacknet.Gui;

namespace SRPortToolkit.Patches
{
    [HarmonyLib.HarmonyPatch(typeof(MainMenu), nameof(MainMenu.DrawBackgroundAndTitle))]
    public class MainMenuDrawTitle
    {
        static void Prefix(MainMenu __instance)
        {
            TextItem.doFontLabel(new Vector2(__instance.State == MainMenu.MainMenuState.Normal ? 634 : 496, 220), "+ SRPortToolkit " + SRPortToolkit.ModVer, GuiData.smallfont, new Color(255,200,0));
        }
    }
}
