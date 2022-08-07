using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace SalvagedStart
{
    public static class SoSHelper
    {
		public static Thing GetShuttleAsBuilding(Thing thing)
        {
			var comp = thing.TryGetComp<CompBecomeBuilding>();
			if (comp != null && comp.Props.buildingDef.GetCompProperties<CompProperties_Transporter>() != null)
            {
				var shuttle = ThingMaker.MakeThing(comp.Props.buildingDef);
				return shuttle;
			}
			return null;
        }
		public static bool TryArrive(CompTransporter transporter, Map map)
        {
			var comp = transporter.parent.TryGetComp<CompBecomePawn>();
			if (comp != null)
			{
				Pawn meAsAPawn = CompBecomePawn.myPawn(transporter.parent, default(IntVec3), 99999);
				if (Rand.Chance(SalvagedStartMod.settings.shipCrashChance))
				{
					Current.Game.GetComponent<GameComponent_DestroyThings>().shipsToDestroy.Add(meAsAPawn);
				}
				Find.WorldPawns.PassToWorld(meAsAPawn, PawnDiscardDecideMode.KeepForever);
				meAsAPawn.SetFaction(transporter.parent.Faction);
				ThingOwner directlyHeldThings = transporter.GetDirectlyHeldThings();
				ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
				activeDropPod.Contents = new ActiveDropPodInfo();
				activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, canMergeWithExistingStacks: true, destroyLeftover: true);
				activeDropPod.Contents.innerContainer.TryAddOrTransfer(meAsAPawn);
				var arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(map.Parent, DropCellFinder.RandomDropSpot(map));
				arrivalAction.Arrived(new List<ActiveDropPodInfo> { activeDropPod.Contents }, map.Tile);
				return true;
			}
			return false;
		}
    }
}
