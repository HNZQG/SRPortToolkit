using Hacknet;
using HarmonyLib;
using System;

namespace SRPortToolkit.Patches
{

    [HarmonyPatch]
public class ObjectSerializerPatcher
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ObjectSerializer), "GetTypeForName")]
    public static void Postfix(ref Type __result, string name)
    {
        if (__result != null)
        {
            return;
        }
        __result = Type.GetType("SRPortToolkit.NameType." + name, false, true);
    }
}

}