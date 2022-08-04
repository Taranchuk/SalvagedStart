using HarmonyLib;
using RimWorld;

namespace SalvagedStart
{
    [HarmonyPatch(typeof(Dialog_LoadTransporters), nameof(Dialog_LoadTransporters.TryAccept))]
	public static class Dialog_LoadTransporters_TryAccept_Patch
	{
		public static bool Prefix(Dialog_LoadTransporters __instance, ref bool __result)
		{
			if (__instance is Dialog_LoadShips dialog_LoadShips)
			{
				__result = dialog_LoadShips.TryAcceptOverride();
				return false;
			}
			return true;
		}
	}
}
