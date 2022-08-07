using HarmonyLib;
using RimWorld;
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
				Log.Message("Should remove " + __result);
				if (Rand.Chance(SalvagedStartMod.settings.shipCrashChance))
                {
					LongEventHandler.toExecuteWhenFinished.Add(delegate
					{
						var map = __result.Map;
						__result.Destroy();
						var size = __result.OccupiedRect().Area;
						for (var i = 0; i < size; i++)
						{
							GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), __result.OccupiedRect().RandomCell, map, ThingPlaceMode.Direct);
						}
					});
                }
			}
		}
	}
}
