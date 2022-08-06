using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SalvagedStart
{
    [HotSwappable]
	public class Dialog_SaveFileList_SearchForShips : Dialog_SaveFileList
	{
		public Page_LoadShips parent;
		public Dialog_SaveFileList_SearchForShips(Page_LoadShips parent)
		{
			interactButLabel = "LoadGameButton".Translate();
			this.parent = parent;
		}

        public string toBeParsed;
        public int frameLoad;
        public override void DoWindowContents(Rect inRect)
        {
            if (toBeParsed.NullOrEmpty() is false && Time.frameCount >= frameLoad)
            {
                DoLoad(toBeParsed);
                toBeParsed = null;
                this.Close();
            }
            base.DoWindowContents(inRect);
        }
        public override void DoFileInteraction(string saveFileName)
        {
            Reset();
            frameLoad = Time.frameCount + 1;
            Messages.Message("SS.ParsingSave".Translate(), MessageTypeDefOf.CautionInput);
            toBeParsed = saveFileName;
        }

        private void DoLoad(string saveFileName)
        {
            var saveFilePath = Path.GetFullPath(GenFilePaths.FilePathForSavedGame(saveFileName));
            var doc = new XmlDocument();
            doc.Load(saveFilePath);
            var root = doc.DocumentElement;
            var factionNodes = root.SelectNodes("//savegame/game/world/factionManager/allFactions/li");
            var ideoNodes = root.SelectNodes("//savegame/game/world/ideoManager/ideos/li");
            var thingNodes = root.SelectNodes("//savegame/game/maps/li/things/thing");
            Scribe.mode = LoadSaveMode.LoadingVars;
            Log_Error_Patch.suppressMessages = true;

            var ideos = new List<Ideo>();
            foreach (XmlNode ideoNode in ideoNodes)
            {
                var ideo = ScribeExtractor.SaveableFromNode<Ideo>(ideoNode, null);
                if (ideo != null)
                {
                    ideos.Add(ideo);
                }
            }

            var factions = new List<Faction>();

            foreach (XmlNode factionNode in factionNodes)
            {
                var faction = ScribeExtractor.SaveableFromNode<Faction>(factionNode, null);
                if (faction?.def != null)
                {
                    factions.Add(faction);
                }
            }

            var things = new Dictionary<Thing, XmlNode>();
            foreach (XmlNode thingNode in thingNodes)
            {
                var thing = ScribeExtractor.SaveableFromNode<Thing>(thingNode, null);
                if (thing != null)
                {
                    things[thing] = thingNode;
                }
            }
            Scribe.loader.FinalizeLoading();
            parent.loadedPlayerFaction = factions.FirstOrDefault(x => x.def.isPlayer);
            if (parent.loadedPlayerFaction is null)
            {
                parent.loadedPlayerFaction = new Faction
                {
                    def = FactionDefOf.PlayerColony
                };
            }

            foreach (var thingItem in things)
            {
                var thing = thingItem.Key;
                var thingNode = thingItem.Value;
                var factionId = GetFactionId(thingNode);
                var thingFaction = thing.Faction ?? factions.FirstOrDefault(x => x.loadID == factionId);
                if (thing is Pawn pawn)
                {
                    var shuttle = GetPlayerShuttle(thing, thingFaction);
                    if (shuttle != null)
                    {
                        parent.ships[shuttle.TryGetComp<CompTransporter>()] = true;
                    }
                    else
                    {
                        if (pawn.kindDef is null)
                        {
                            if (pawn.RaceProps.Humanlike)
                            {
                                pawn.kindDef = PawnKindDefOf.Colonist;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (thingFaction != null && thingFaction.IsPlayer)
                        {
                            if (pawn.Faction is null)
                            {
                                pawn.SetFactionDirect(thingFaction);
                            }
                            DoPawnCleanup(pawn);
                            parent.pawns.Add(pawn);
                        }
                    }
                }
                else
                {
                    var shuttle = GetPlayerShuttle(thing, thingFaction);
                    if (shuttle != null)
                    {
                        parent.ships[shuttle.TryGetComp<CompTransporter>()] = true;
                    }
                    else if (CanBeTransferred(thing))
                    {
                        parent.items.Add(thing);
                    }
                }
            }


            Log_Error_Patch.suppressMessages = false;
            var shipsToBeLaunched = parent.ships.Where(x => x.Value).Select(x => x.Key).ToList();
            if (shipsToBeLaunched.Any() is false)
            {
                var window = new Dialog_MessageBox("SS.NoUsableShipsFound".Translate());
                Find.WindowStack.Add(window);
            }
            else
            {
                var window = new Dialog_LoadShips(null, parent.ships.Keys.ToList(), parent.pawns, parent.items, parent.loadedPlayerFaction);
                Find.WindowStack.Add(window);
            }
        }

        private static Thing GetPlayerShuttle(Thing thing, Faction thingFaction)
        {
			var shuttle = GetShuttle(thing);
			if (shuttle != null)
            {
				if (shuttle.Faction != thingFaction && thingFaction != null)
                {
					shuttle.SetFactionDirect(thingFaction);
				}
				if (shuttle.Faction?.def.isPlayer ?? false)
                {
					return shuttle;
				}
			}
			return null;
		}

        private static Thing GetShuttle(Thing thing)
        {
            var comp = thing.TryGetComp<CompTransporter>();
            if (comp != null)
            {
                return thing;
            }
            else if (ModsConfig.IsActive("kentington.saveourship2"))
            {
                return SoSHelper.GetShuttleAsBuilding(thing);
            }
            return null;
        }

        private static int GetFactionId(XmlNode thingNode)
        {
			var node = thingNode.SelectSingleNode("faction");
			if (node != null)
            {
				return int.Parse(node.InnerText.Replace("Faction_", ""));
			}
			return -1;
        }

        private static void DoPawnCleanup(Pawn pawn)
        {
			pawn.apparel?.WornApparel.RemoveAll(x => x is null || x.def is null);
			pawn.health.hediffSet.hediffs.RemoveAll(x => x is null || x.def is null);
			pawn.equipment?.equipment.RemoveAll(x => x is null || x.def is null);
			pawn.inventory?.innerContainer.RemoveAll(x => CanBeTransferred(x) is false);
			pawn.jobs = new Pawn_JobTracker(pawn);
			pawn.pather = new Pawn_PathFollower(pawn);
			pawn.roping = new Pawn_RopeTracker(pawn);

			if (pawn.RaceProps.Humanlike)
            {
				pawn.ideo = new Pawn_IdeoTracker(pawn);
			}
			if (pawn.royalty != null)
            {
				pawn.royalty = new Pawn_RoyaltyTracker(pawn);
			}
			PawnComponentsUtility.CreateInitialComponents(pawn);
			PawnComponentsUtility.AddComponentsForSpawn(pawn);
			if (pawn.RaceProps.Humanlike)
            {
				if (pawn.story.hairDef is null)
				{
					pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
				}
				if (pawn.style != null)
				{
					if (pawn.style.beardDef is null)
					{
						pawn.style.beardDef = ((pawn.gender == Gender.Male) ? PawnStyleItemChooser.ChooseStyleItem<BeardDef>(pawn) : BeardDefOf.NoBeard);
					}
					if (ModsConfig.IdeologyActive)
					{
						if (pawn.style.bodyTattoo is null)
						{
							pawn.style.bodyTattoo = PawnStyleItemChooser.ChooseStyleItem<TattooDef>(pawn, TattooType.Face);
						}
						if (pawn.style.bodyTattoo is null)
						{
							pawn.style.bodyTattoo = PawnStyleItemChooser.ChooseStyleItem<TattooDef>(pawn, TattooType.Body);
						}
					}
					else
					{
						if (pawn.style.faceTattoo is null)
						{
							pawn.style.faceTattoo = TattooDefOf.NoTattoo_Face;
						}
						if (pawn.style.bodyTattoo is null)
						{
							pawn.style.bodyTattoo = TattooDefOf.NoTattoo_Body;
						}
					}
				}
			}

		}


		private static bool CanBeTransferred(Thing thing)
        {
			if (thing?.def?.category == ThingCategory.Item)
            {
				if (thing is Corpse)
                {
					return false;
                }
				if (thing is MinifiedThing minifiedThing && minifiedThing.InnerThing is null)
                {
					return false;
                }
				if (thing is UnfinishedThing unfinishedThing && (unfinishedThing.ingredients is null || unfinishedThing.ingredients.Any(x => x?.def is null)))
                {
					return false;
				}

				var comp = thing.TryGetComp<CompIngredients>();
				if (comp != null && (comp.ingredients is null || comp.ingredients.Any(x => x is null)))
                {
					return false;
                }
				return true;
            }
			return false;
        }

        private void Reset()
        {
            parent.ships.Clear();
			parent.pawns.Clear();
			parent.items.Clear();
        }
    }

    [HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo))]
	public static class FIXIT
    {
		public static void Postfix(Pawn ___pawn)
        {
			if (___pawn.IsColonist)
            {
				Log.Message("Setting ideo for " + ___pawn);
            }
        }
    }
}
