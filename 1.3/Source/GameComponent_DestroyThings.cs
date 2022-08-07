using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SalvagedStart
{
    public class GameComponent_DestroyThings : GameComponent
    {
		public List<Thing> thingsToDestroy = new List<Thing>();
		public GameComponent_DestroyThings(Game game)
        {

        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
			for (var i = thingsToDestroy.Count - 1; i >= 0; i--)
            {
				var thing = thingsToDestroy[i];
				if (thing.Spawned)
                {
					thingsToDestroy.RemoveAt(i);
					var map = thing.Map;
					var shipSize = thing.def.size.z;
					if (thing.def.leaveResourcesWhenKilled)
					{
						thing.Kill();
					}
					else
					{
						thing.Destroy();
						for (var j = 0; j < shipSize; j++)
						{
							GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), thing.OccupiedRect().RandomCell, map, ThingPlaceMode.Direct);
						}
					}

					foreach (var cell in thing.OccupiedRect().ExpandedBy(1))
					{
						foreach (var otherThing in map.thingGrid.ThingsListAt(cell).ListFullCopy())
						{
							if (otherThing is Pawn pawn)
							{
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
						GenExplosion.DoExplosion(thing.OccupiedRect().CenterCell, map, shipSize, DamageDefOf.Bomb, thing, ignoredThings: new List<Thing> { ScenPart_ConfigPage_SalvagedStart.safePawn });
					}
				}
			}
        }
    }
}
