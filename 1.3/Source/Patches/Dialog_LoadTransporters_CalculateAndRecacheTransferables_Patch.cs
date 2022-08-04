using HarmonyLib;
using RimWorld;

namespace SalvagedStart
{
    [HarmonyPatch(typeof(Dialog_LoadTransporters), nameof(Dialog_LoadTransporters.CalculateAndRecacheTransferables))]
	public static class Dialog_LoadTransporters_CalculateAndRecacheTransferables_Patch
    {
		public static bool Prefix(Dialog_LoadTransporters __instance)
        {
			if (__instance is Dialog_LoadShips dialog_LoadShips)
            {
				dialog_LoadShips.CalculateAndRecacheTransferablesOverride();
				return false;
			}
			return true;
        }
    }
}
