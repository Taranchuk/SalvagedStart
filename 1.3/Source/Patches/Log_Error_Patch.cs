using HarmonyLib;
using System;
using Verse;

namespace SalvagedStart
{
    [HarmonyPatch(typeof(Log), nameof(Log.Error), new Type[] { typeof(string) })]
	public static class Log_Error_Patch
	{
		public static bool suppressMessages;
		public static bool Prefix()
		{
			if (suppressMessages)
			{
				return false;
			}
			return true;
		}
	}
}
