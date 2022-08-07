using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            try
            {
                var saveFilePath = Path.GetFullPath(GenFilePaths.FilePathForSavedGame(saveFileName));
                var doc = new XmlDocument();
                doc.Load(saveFilePath);
                var root = doc.DocumentElement;
                var factionNodes = root.SelectNodes("//savegame/game/world/factionManager/allFactions/li");
                var ideoNodes = root.SelectNodes("//savegame/game/world/ideoManager/ideos/li");
                var thingNodes = root.SelectNodes("//savegame/game/maps/li/things/thing");
                var researchNode = root.SelectSingleNode("//savegame/game/researchManager");
                Scribe.mode = LoadSaveMode.LoadingVars;
                Log_Error_Patch.suppressMessages = true;

                parent.oldWorld = Current.CreatingWorld;
                var newWorld = new World();
                Current.CreatingWorld = newWorld;
                newWorld.worldObjects = new WorldObjectsHolder();
                newWorld.factionManager = new FactionManager();
                newWorld.ideoManager = new IdeoManager();
                newWorld.worldPawns = new WorldPawns();
                newWorld.gameConditionManager = new GameConditionManager(newWorld);
                newWorld.storyState = new StoryState(newWorld);
                newWorld.renderer = new WorldRenderer();
                newWorld.UI = new WorldInterface();
                newWorld.grid = new WorldGrid();
                newWorld.pathGrid = new WorldPathGrid();
                newWorld.FillComponents();
                parent.oldFaction = Find.GameInitData.playerFaction;
                Find.GameInitData.playerFaction = parent.loadedPlayerFaction;
                ScenPart_KeepResearchProgress.prevResearchManager = ScribeExtractor.SaveableFromNode<ResearchManager>(researchNode, null);
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
                newWorld.factionManager.Add(parent.loadedPlayerFaction);

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
            catch (Exception ex)
            {
                Log.Error("Exception in " + ex);
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

        private static int CleanupList<T>(List<T> things, Predicate<T> predicate = null)
        {
            if (things is null) return -1;

            if (predicate is null)
            {
                predicate = (x => x is null || typeof(T).GetField("def")?.GetValue(x) is null);
            }

            int numRemoved = 0;
            for (int i = things.Count - 1; i >= 0; i--)
            {
                if (predicate(things[i]))
                {
                    things.RemoveAt(i);
                    numRemoved++;
                }
            }
            return numRemoved;
        }

        private static void DoPawnCleanup(Pawn pawn)
        {
            int numRemoved;
            if ((numRemoved = CleanupList(pawn.apparel?.WornApparel)) > 0)
            {
                Messages.Message("SS.LostDefList".Translate("SS.WornClothing".Translate(), numRemoved, pawn.LabelShort), MessageTypeDefOf.CautionInput);
            }
            if ((numRemoved = CleanupList(pawn.health.hediffSet.hediffs)) > 0)
            {
                Messages.Message("SS.LostDefList".Translate("SS.InjuryOrDisease".Translate(), numRemoved, pawn.LabelShort), MessageTypeDefOf.CautionInput);
            }
            if ((numRemoved = CleanupList(pawn.equipment?.equipment.innerList)) > 0)
            {
                Messages.Message("SS.LostDefList".Translate("SS.Equipment".Translate(), numRemoved, pawn.LabelShort), MessageTypeDefOf.CautionInput);
            }
            if ((numRemoved = CleanupList(pawn.inventory?.innerContainer.innerList, x => CanBeTransferred(x) is false)) > 0)
            {
                Messages.Message("SS.LostDefList".Translate("SS.InventoryItems".Translate(), numRemoved, pawn.LabelShort), MessageTypeDefOf.CautionInput);
            }
            if ((numRemoved = CleanupList(pawn.relations?.directRelations, x => x.def is null || x.otherPawn is null)) > 0) {
                Messages.Message("SS.LostDefList".Translate("SS.DirectRelationships".Translate(), numRemoved, pawn.LabelShort), MessageTypeDefOf.CautionInput);
            }
            if ((numRemoved = CleanupList(pawn.needs.mood?.thoughts.memories?.memories, x => x.def is null)) > 0) {
                Messages.Message("SS.LostDefList".Translate("SS.Memories".Translate(), numRemoved, pawn.LabelShort), MessageTypeDefOf.CautionInput);
            }
			pawn.jobs = new Pawn_JobTracker(pawn);
			pawn.pather = new Pawn_PathFollower(pawn);
			pawn.roping = new Pawn_RopeTracker(pawn);
            pawn.mindState = new Pawn_MindState(pawn);
            pawn.needs = new Pawn_NeedsTracker(pawn);
            pawn.meleeVerbs = new Pawn_MeleeVerbs(pawn);
            pawn.verbTracker = new VerbTracker(pawn);
            pawn.carryTracker = new Pawn_CarryTracker(pawn);
            pawn.ownership = new Pawn_Ownership(pawn);
            pawn.connections = new Pawn_ConnectionsTracker(pawn);

            if (pawn.RaceProps.Humanlike)
            {
                if (pawn.guest == null)
                {
                    pawn.guest = new Pawn_GuestTracker(pawn);
                }
                if (pawn.guilt == null)
                {
                    pawn.guilt = new Pawn_GuiltTracker(pawn);
                }
                if (pawn.workSettings == null)
                {
                    pawn.workSettings = new Pawn_WorkSettings(pawn);
                }
                if (pawn.styleObserver == null)
                {
                    pawn.styleObserver = new Pawn_StyleObserverTracker(pawn);
                }
                if (pawn.surroundings == null)
                {
                    pawn.surroundings = new Pawn_SurroundingsTracker(pawn);
                }
            }
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
                    Messages.Message("SS.LostDef".Translate("SS.Hair".Translate(), pawn.LabelShort), MessageTypeDefOf.CautionInput);
                    pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
				}
				if (pawn.style != null)
				{
					if (pawn.style.beardDef is null)
                    {
                        Messages.Message("SS.LostDef".Translate("SS.Beard".Translate(), pawn.LabelShort), MessageTypeDefOf.CautionInput);
                        pawn.style.beardDef = ((pawn.gender == Gender.Male) ? PawnStyleItemChooser.ChooseStyleItem<BeardDef>(pawn) : BeardDefOf.NoBeard);
					}
					if (ModsConfig.IdeologyActive)
					{
						if (pawn.style.bodyTattoo is null)
                        {
                            Messages.Message("SS.LostDef".Translate("SS.FaceTattoo".Translate(), pawn.LabelShort), MessageTypeDefOf.CautionInput);
                            pawn.style.faceTattoo = PawnStyleItemChooser.ChooseStyleItem<TattooDef>(pawn, TattooType.Face);
						}
						if (pawn.style.bodyTattoo is null)
                        {
                            Messages.Message("SS.LostDef".Translate("SS.BodyTattoo".Translate(), pawn.LabelShort), MessageTypeDefOf.CautionInput);
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
                if (thing.stackCount <= 0)
                {
                    return false;
                }
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
                    Messages.Message("SS.LostDef".Translate("SS.Ingredients".Translate(), thing.LabelShort), MessageTypeDefOf.CautionInput);
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
}
