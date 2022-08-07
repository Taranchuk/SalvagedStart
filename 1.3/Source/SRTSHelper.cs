using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SalvagedStart
{
    public static class SRTSHelper
    {
        public static bool TryArrive(CompTransporter transporter, Map map)
        {
			var srtsLauncher = transporter.parent.GetComp<SRTS.CompLaunchableSRTS>();
			if (srtsLauncher != null)
			{
				ThingOwner directlyHeldThings = transporter.GetDirectlyHeldThings();
				Thing thing = ThingMaker.MakeThing(ThingDef.Named(transporter.parent.def.defName));
				GenSpawn_Spawn_Patch.arrivingThings.Add(thing);
				thing.SetFactionDirect(Faction.OfPlayer);
				thing.Rotation = Rot4.South;
				CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
				compRefuelable.GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(compRefuelable, compRefuelable.Props.fuelCapacity / 2f);
				thing.stackCount = 1;
				ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(transporter.parent.def.defName + "_Active"));
				activeDropPod.Contents = new ActiveDropPodInfo();
				directlyHeldThings.TryAddOrTransfer(thing);
				activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, canMergeWithExistingStacks: true, destroyLeftover: true);
				var arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(map.Parent, DropCellFinder.RandomDropSpot(map));
				arrivalAction.Arrived(new List<ActiveDropPodInfo> { activeDropPod.Contents }, map.Tile);
				return true;
			}
			return false;
		}
    }
}
