using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace SalvagedStart
{
    [HarmonyPatch(typeof(GenSpawn), "Spawn", new System.Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) })]
	public static class GenSpawn_Spawn_Patch
	{
		public static List<Thing> arrivingThings = new List<Thing>();
		public static void Postfix(Thing __result, bool respawningAfterLoad)
		{
			if (arrivingThings.Contains(__result))
            {
				arrivingThings.Remove(__result);
				if (Rand.Chance(SalvagedStartMod.settings.shipCrashChance))
                {
					Current.Game.GetComponent<GameComponent_DestroyThings>().thingsToDestroy.Add(__result);
                }
			}
		}
	}
}
