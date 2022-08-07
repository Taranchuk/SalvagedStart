using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SalvagedStart
{
    [HotSwappable]
	public class Page_LoadShips : Page
    {
		public World oldWorld;
		public Faction oldFaction;
		public Faction loadedPlayerFaction;
		public Dictionary<CompTransporter, bool> ships = new Dictionary<CompTransporter, bool>();
		public List<Pawn> pawns = new List<Pawn>();
		public List<Thing> items = new List<Thing>();
		public override string PageTitle => "SS.SalvagedStart".Translate();
        public override void PostOpen()
        {
            base.PostOpen();
			ScenPart_ConfigPage_SalvagedStart.transporters.Clear();
		}

		public float scrollHeight;
		public Vector2 scrollPosition;
		public override void DoWindowContents(Rect rect)
		{
			DrawPageTitle(rect);
			rect.yMin += 45f;
			DoBottomButtons(rect, "Start".Translate(), null, null, showNext: true, doNextOnKeypress: false);
			var buttonRect = new Rect(rect.x, rect.y, 250, 32);
			if (Widgets.ButtonText(buttonRect, "SS.LoadSave".Translate()))
            {
				var window = new Dialog_SaveFileList_SearchForShips(this);
				Find.WindowStack.Add(window);
			}
			var shipsToBeLaunched = this.ships.Where(x => x.Value).Select(x => x.Key).ToList();
			if (shipsToBeLaunched.Any() && Widgets.ButtonText(new Rect(buttonRect.xMax + 15, rect.y, 250, 32), "SS.SetLoadout".Translate()))
			{
				var window = new Dialog_LoadShips(null, shipsToBeLaunched, this.pawns, this.items, this.loadedPlayerFaction);
				Find.WindowStack.Add(window);
			}

			Vector2 pos = new Vector2(buttonRect.x, buttonRect.yMax + 15);
			var titleRect = new Rect(pos.x, pos.y, 250, 24);
			var inRect = new Rect(pos.x, pos.y, rect.width, 550);
			var totalRect = new Rect(pos.x, pos.y, rect.width - 16, scrollHeight);
			Widgets.BeginScrollView(inRect, ref scrollPosition, totalRect);
			scrollHeight = 0;
			Text.Anchor = TextAnchor.MiddleLeft;
			if (this.ships.Any())
            {
				Widgets.Label(titleRect, "SS.ShipsToBeLaunched".Translate());
				pos.y += 24;
				foreach (var ship in this.ships.Keys.ToList())
				{
					var shipRect = new Rect(pos.x, pos.y, 32, 32);
					try
					{
						Widgets.ThingIcon(shipRect, ship.parent);
					}
					catch (Exception ex) { Log.Message("Can't draw " + ship + " exception: " + ex); }
					var shuttleLabel = new Rect(shipRect.xMax + 15, shipRect.y, 250, 32);
					Widgets.Label(shuttleLabel, ship.parent.LabelCap);
					var value = this.ships[ship];
					Widgets.Checkbox(shuttleLabel.xMax, shuttleLabel.y, ref value);
					this.ships[ship] = value;
					pos.y += 32;
				}
				pos.y += 12;

				titleRect = new Rect(pos.x, pos.y, 250, 24);
				var thingsToBeLoaded = shipsToBeLaunched.SelectMany(x => x.GetDirectlyHeldThings()).ToList();
				var colonists = thingsToBeLoaded.Where(x => x is Pawn pawn && pawn.RaceProps.Humanlike).ToList();
				Widgets.Label(titleRect, "SS.ColonistsToBeLoaded".Translate(Mathf.Min(shipsToBeLaunched.Count, colonists.Count)));
				pos.y += 24;
				foreach (var pawn in colonists)
				{
					var entryRect = new Rect(pos.x, pos.y, 32, 32);
					try
					{
						Widgets.ThingIcon(entryRect, pawn);
					}
					catch (Exception ex) { Log.Message("Can't draw " + pawn + " exception: " + ex); }
					var labe = new Rect(entryRect.xMax + 15, entryRect.y, 250, 32);
					Widgets.Label(labe, pawn.LabelCap);
					pos.y += 32;
				}
				var animals = thingsToBeLoaded.Where(x => x is Pawn pawn && !pawn.RaceProps.Humanlike).ToList();
				if (animals.Any())
				{
					pos.y += 12;
					titleRect = new Rect(pos.x, pos.y, 250, 24);
					Widgets.Label(titleRect, "SS.AnimalsToBeLoaded".Translate());
					pos.y += 24;
					foreach (var pawn in animals)
					{
						var entryRect = new Rect(pos.x, pos.y, 32, 32);
						try
						{
							Widgets.ThingIcon(entryRect, pawn);
						}
						catch (Exception ex) { Log.Message("Can't draw " + pawn + " exception: " + ex); }
						var labe = new Rect(entryRect.xMax + 15, entryRect.y, 250, 32);
						Widgets.Label(labe, pawn.LabelCap);
						pos.y += 32;
					}
				}
			}
			Text.Anchor = TextAnchor.UpperLeft;

			scrollHeight = pos.y - buttonRect.yMax + 15;
			Widgets.EndScrollView();
		}
        public override bool CanDoNext()
		{
			ScenPart_ConfigPage_SalvagedStart.transporters = this.ships.Where(x => x.Value).Select(x => x.Key).ToList();
			return base.CanDoNext() && ScenPart_ConfigPage_SalvagedStart.transporters.Any() 
				&& ScenPart_ConfigPage_SalvagedStart.transporters.SelectMany(x => x.GetDirectlyHeldThings()).Count(x => x is Pawn pawn 
				&& pawn.RaceProps.Humanlike) >= ScenPart_ConfigPage_SalvagedStart.transporters.Count;
        }
        public override void DoNext()
        {
			Current.CreatingWorld = oldWorld;
			Find.GameInitData.playerFaction = oldFaction;
			Find.GameInitData.startingAndOptionalPawns.Clear();
			foreach (var pawn in ScenPart_ConfigPage_SalvagedStart.transporters.SelectMany(x => x.GetDirectlyHeldThings()).OfType<Pawn>())
            {
				if (pawn.RaceProps.Humanlike)
                {
					Find.GameInitData.startingAndOptionalPawns.Add(pawn);
				}
			}

			foreach (var loadedPawn in Find.GameInitData.startingAndOptionalPawns)
            {
				foreach (Pawn potentiallyRelatedPawn in loadedPawn.relations.PotentiallyRelatedPawns)
				{
					if (Find.GameInitData.startingAndOptionalPawns.Contains(potentiallyRelatedPawn) || loadedPawn.needs == null || loadedPawn.needs.mood == null || !PawnUtility.ShouldGetThoughtAbout(loadedPawn, potentiallyRelatedPawn))
					{
						continue;
					}
					PawnRelationDef mostImportantRelation = potentiallyRelatedPawn.GetMostImportantRelation(loadedPawn);
					if (mostImportantRelation != null)
					{
						ThoughtDef genderSpecificThought = mostImportantRelation.GetGenderSpecificThought(potentiallyRelatedPawn, PawnDiedOrDownedThoughtsKind.Lost);
						if (genderSpecificThought != null)
						{
							var thought = new IndividualThoughtToAdd(genderSpecificThought, loadedPawn, potentiallyRelatedPawn);
							thought.Add();
						}
					}
				}
			}

			Find.GameInitData.startingPawnCount = Find.GameInitData.startingAndOptionalPawns.Count;
			base.DoNext();

        }
    }
}
