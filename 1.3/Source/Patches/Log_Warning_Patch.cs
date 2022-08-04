using HarmonyLib;
using System;
using Verse;

namespace SalvagedStart
{
    [HarmonyPatch(typeof(Log), nameof(Log.Warning), new Type[] { typeof(string) })]
	public static class Log_Warning_Patch
	{
		public static bool Prefix()
		{
			if (Log_Error_Patch.suppressMessages)
			{
				return false;
			}
			return true;
		}
	}
}
