using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SalvagedStart
{
    public class GameComponent_DestroyThings : GameComponent
    {
		public List<Thing> shipsToDestroy = new List<Thing>();
		public GameComponent_DestroyThings(Game game)
        {

        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
			for (var i = shipsToDestroy.Count - 1; i >= 0; i--)
            {
				var ship = shipsToDestroy[i];
				if (ship.Spawned)
                {
					var shipName = ship.LabelCap;
					shipsToDestroy.RemoveAt(i);
					var map = ship.Map;
					var basePosition = ship.OccupiedRect().CenterCell;
					var shipSize = ship.def.size.z;
					if (ship.def.leaveResourcesWhenKilled)
					{
						ship.Kill();
					}
					else
					{
						ship.Destroy();
						for (var j = 0; j < shipSize; j++)
						{
							GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), ship.OccupiedRect().RandomCell, map, ThingPlaceMode.Direct);
						}
					}

					var pilotCandidates = new List<Pawn>();
					foreach (var cell in ship.OccupiedRect().ExpandedBy(1))
					{
						foreach (var otherThing in map.thingGrid.ThingsListAt(cell).ListFullCopy())
						{
							if (otherThing is Pawn pawn)
							{
								if (pawn.IsColonist)
                                {
									pilotCandidates.Add(pawn);
								}
								if (pawn != ScenPart_ConfigPage_SalvagedStart.safePawn)
                                {
									if (Rand.Chance(SalvagedStartMod.settings.chanceOfDowningPawnUponCrash))
                                    {
										HealthUtility.DamageUntilDowned(pawn);
                                    }
                                }
							}
						}
					}

					if (Rand.Chance(SalvagedStartMod.settings.chanceOfOfExplosionUponCrash))
					{
						if (pilotCandidates.TryRandomElement(out var pilot))
                        {
							Find.LetterStack.ReceiveLetter("SS.ShipCrashed".Translate(), "SS.ShipCrashedExplosionDesc".Translate(shipName, pilot.Named("PAWN")), LetterDefOf.NegativeEvent, new LookTargets(basePosition, map));
						}
						GenExplosion.DoExplosion(basePosition, map, shipSize, DamageDefOf.Bomb, ship, ignoredThings: new List<Thing> { ScenPart_ConfigPage_SalvagedStart.safePawn });
					}
					else
                    {
						if (pilotCandidates.TryRandomElement(out var pilot))
                        {
							Find.LetterStack.ReceiveLetter("SS.ShipCrashed".Translate(), "SS.ShipCrashedDesc".Translate(pilot.Named("PAWN"), shipName), LetterDefOf.NegativeEvent, new LookTargets(basePosition, map));
						}
					}
				}
			}
        }


        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Collections.Look(ref shipsToDestroy, "shipsToDestroy", LookMode.Reference);
        }
    }
}
