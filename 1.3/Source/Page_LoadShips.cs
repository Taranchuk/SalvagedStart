using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SalvagedStart
{

    [HotSwappable]
	public class Page_LoadShips : Page
    {
		public List<Thing> shuttles = new List<Thing>();
		public List<Pawn> pawns = new List<Pawn>();
		public List<Thing> items = new List<Thing>();
		public override string PageTitle => "SS.SalvagedStart".Translate();

        public override void PostOpen()
        {
            base.PostOpen();
			ScenPart_ConfigPage_SalvagedStart.transporters.Clear();
		}
        public override void DoWindowContents(Rect rect)
		{
			DrawPageTitle(rect);
			rect.yMin += 45f;
			DoBottomButtons(rect, "Start".Translate(), null, null, showNext: true, doNextOnKeypress: false);
			var buttonRect = new Rect(rect.x, rect.y, 250, 32);
			if (Widgets.ButtonText(buttonRect, "LoadSave"))
            {
				var window = new Dialog_SaveFileList_SearchForShips(this);
				Find.WindowStack.Add(window);
			}
			Vector2 pos = new Vector2(buttonRect.x, buttonRect.yMax);
			foreach (var shuttle in shuttles)
            {
				var shuttleRect = new Rect(pos.x, pos.y, 24, 24);
				try
				{
					Widgets.ThingIcon(shuttleRect, shuttle);
				}
				catch { Log.Message("Can't draw " + shuttle); }
				var shuttleLabel = new Rect(shuttleRect.xMax, shuttleRect.y, 250, 24);
				Widgets.Label(shuttleLabel, shuttle.LabelCap);
				pos.y += 24;
			}
		}

        public override bool CanDoNext()
        {
            return base.CanDoNext() && ScenPart_ConfigPage_SalvagedStart.transporters.Any() && pawns.Any();
        }

        public override void DoNext()
        {
			Find.GameInitData.startingAndOptionalPawns.Clear();
			foreach (var pawn in pawns)
            {
				if (pawn.RaceProps.Humanlike)
                {
					Find.GameInitData.startingAndOptionalPawns.Add(pawn);
				}
			}
			Find.GameInitData.startingPawnCount = Find.GameInitData.startingAndOptionalPawns.Count;
			base.DoNext();

        }
    }
}
