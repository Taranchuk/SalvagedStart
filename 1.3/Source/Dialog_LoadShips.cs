using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using static RimWorld.Planet.CaravanUIUtility;

namespace SalvagedStart
{

    [HotSwappable]
	public class Dialog_LoadShips : Dialog_LoadTransporters
	{
		public List<Pawn> pawns;
		public List<Thing> items;
		public Faction playerFaction;
		public Dialog_LoadShips(Map map, List<CompTransporter> transporters, List<Pawn> pawns, List<Thing> items, Faction playerFaction) : base(map, transporters)
        {

            this.pawns = pawns;
            this.items = items;
            this.playerFaction = playerFaction;
        }
        public override void DoWindowContents(Rect inRect)
		{
			Rect rect = new Rect(0f, 0f, inRect.width, 35f);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, "LoadTransporters".Translate(TransportersLabel));
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			if (transporters[0].Props.showOverallStats)
			{
				DrawCaravanInfoOverride(new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, "", -1, cachedTilesPerDayExplanation, default, default, default, Visibility, cachedVisibilityExplanation, CaravanMassUsage, CaravanMassCapacity, cachedCaravanMassCapacityExplanation), null, -1, null, lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), lerpMassColor: false);
				inRect.yMin += 52f;
			}
			tabsList.Clear();
			tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
			{
				tab = Tab.Pawns;
			}, tab == Tab.Pawns));
			tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
			{
				tab = Tab.Items;
			}, tab == Tab.Items));
			inRect.yMin += 67f;
			Widgets.DrawMenuSection(inRect);
			TabDrawer.DrawTabs(inRect, tabsList);
			inRect = inRect.ContractedBy(17f);
			Widgets.BeginGroup(inRect);
			Rect rect2 = inRect.AtZero();
			DoBottomButtonsOverride(rect2);
			Rect inRect2 = rect2;
			inRect2.yMax -= 59f;
			bool anythingChanged = false;
			switch (tab)
			{
				case Tab.Pawns:
					pawnsTransfer.OnGUI(inRect2, out anythingChanged);
					break;
				case Tab.Items:
					itemsTransfer.OnGUI(inRect2, out anythingChanged);
					break;
			}
			if (anythingChanged)
			{
				CountToTransferChanged();
			}
			Widgets.EndGroup();
		}

		private void DoBottomButtonsOverride(Rect rect)
		{
			Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(rect2, autoLoot ? "LoadSelected".Translate() : "AcceptButton".Translate()))
			{
				if (TryAccept())
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
					Close(doCloseSound: false);
				}
			}
			if (Widgets.ButtonText(new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
			{
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				CalculateAndRecacheTransferables();
			}
			if (Widgets.ButtonText(new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate()))
			{
				Close();
			}
		}

		public static void DrawCaravanInfoOverride(CaravanInfo info, CaravanInfo? info2, int currentTile, int? ticksToArrive, float lastMassFlashTime, Rect rect, bool lerpMassColor = true, string extraDaysWorthOfFoodTipInfo = null, bool multiline = false)
		{
			tmpInfo.Clear();
			TaggedString taggedString = info.massUsage.ToStringEnsureThreshold(info.massCapacity, 0) + " / " + info.massCapacity.ToString("F0") + " " + "kg".Translate();
			TaggedString taggedString2 = (info2.HasValue ? (info2.Value.massUsage.ToStringEnsureThreshold(info2.Value.massCapacity, 0) + " / " + info2.Value.massCapacity.ToString("F0") + " " + "kg".Translate()) : ((TaggedString)null));
			tmpInfo.Add(new TransferableUIUtility.ExtraInfo("Mass".Translate(), taggedString, GetMassColor(info.massUsage, info.massCapacity, lerpMassColor), GetMassTip(info.massUsage, info.massCapacity, info.massCapacityExplanation, info2.HasValue ? new float?(info2.Value.massUsage) : null, info2.HasValue ? new float?(info2.Value.massCapacity) : null, info2.HasValue ? info2.Value.massCapacityExplanation : null), taggedString2, info2.HasValue ? GetMassColor(info2.Value.massUsage, info2.Value.massCapacity, lerpMassColor) : Color.white, lastMassFlashTime));
			TransferableUIUtility.DrawExtraInfo(tmpInfo, rect);
		}

		public void CalculateAndRecacheTransferablesOverride()
		{
			transferables = new List<TransferableOneWay>();
			AddPawnsToTransferablesOverride();
			AddItemsToTransferablesOverride();
			pawnsTransfer = new TransferableOneWayWidget(null, null, null, "FormCaravanColonyThingCountTip".Translate(), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: true, () => MassCapacity - MassUsage, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, -1, drawMarketValue: true, drawEquippedWeapon: true, drawNutritionEatenPerDay: true, drawItemNutrition: false, drawForagedFoodPerDay: true);
			AddPawnSections(pawnsTransfer, transferables);
			itemsTransfer = new TransferableOneWayWidget(null, null, null, "FormCaravanColonyThingCountTip".Translate(), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: true, () => MassCapacity - MassUsage, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, -1, drawMarketValue: true, drawEquippedWeapon: false, drawNutritionEatenPerDay: false, drawItemNutrition: true, drawForagedFoodPerDay: false, drawDaysUntilRot: false);
			AddItemSection(itemsTransfer, transferables);
			CountToTransferChanged();
		}

        public override void PostOpen()
        {
            base.PostOpen();
			SetLoadedItemsToLoad();
		}

        public bool TryAcceptOverride()
		{
			List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
			if (!CheckForErrorsOverride(pawnsFromTransferables))
			{
				return false;
			}
			foreach (var transporter in transporters)
			{
				transporter.GetDirectlyHeldThings().Clear();
			}
			int i;
			for (i = 0; i < transferables.Count; i++)
			{
				TransferableUtility.Transfer(transferables[i].things, transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
				{
					transporters[i % transporters.Count].GetDirectlyHeldThings().TryAdd(splitPiece.TryMakeMinified());
				});
			}
			return true;
		}

		private bool CheckForErrorsOverride(List<Pawn> pawns)
		{
			if (!CanChangeAssignedThingsAfterStarting && !transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
			{
				if (transporters[0].Props.max1PerGroup)
				{
					Messages.Message("CantSendEmptyTransporterSingle".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					Messages.Message("CantSendEmptyTransportPods".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
			if (transporters[0].Props.max1PerGroup)
			{
				CompShuttle shuttle = transporters[0].Shuttle;
				if (shuttle != null && shuttle.requiredColonistCount > 0 && pawns.Count > shuttle.requiredColonistCount)
				{
					Messages.Message("TransporterSingleTooManyColonists".Translate(shuttle.requiredColonistCount), MessageTypeDefOf.RejectInput, historical: false);
					return false;
				}
			}
			if (MassUsage > MassCapacity)
			{
				FlashMass();
				if (transporters[0].Props.max1PerGroup)
				{
					Messages.Message("TooBigTransporterSingleMassUsage".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
			return true;
		}

		public void AddPawnSections(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
		{
			IEnumerable<TransferableOneWay> source = transferables.Where((TransferableOneWay x) => x.ThingDef.category == ThingCategory.Pawn);
			widget.AddSection("ColonistsSection".Translate(), source.Where((TransferableOneWay x) => ((Pawn)x.AnyThing).IsFreeNonSlaveColonist));
			widget.AddSection("AnimalsSection".Translate(), source.Where((TransferableOneWay x) => ((Pawn)x.AnyThing).RaceProps.Animal));
		}

		public void AddItemSection(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
		{
			widget.AddSection("SS.Items".Translate(), transferables.Where(x => x.ThingDef.category != ThingCategory.Pawn));
		}

		private void AddPawnsToTransferablesOverride()
		{
			foreach (Pawn item in this.pawns)
			{
				AddToTransferables(item);
			}
		}

		private void AddItemsToTransferablesOverride()
		{
			foreach (Thing item in this.items)
			{
				if (item.stackCount > 0)
                {
					AddToTransferables(item);
				}
			}
		}
	}
}
