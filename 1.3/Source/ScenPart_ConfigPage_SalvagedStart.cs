using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SalvagedStart
{
    public class ScenPart_ConfigPage_SalvagedStart : ScenPart_ConfigPage
    {
		public override void PostIdeoChosen()
		{
			var pawnCount = 1;
			Find.GameInitData.startingPawnCount = pawnCount;
			if (ModsConfig.IdeologyActive && Faction.OfPlayerSilentFail?.ideos?.PrimaryIdeo != null)
			{
				foreach (Precept item in Faction.OfPlayerSilentFail.ideos.PrimaryIdeo.PreceptsListForReading)
				{
					if (item.def.defaultDrugPolicyOverride != null)
					{
						Current.Game.drugPolicyDatabase.MakePolicyDefault(item.def.defaultDrugPolicyOverride);
					}
				}
			}
			int num = 0;
			do
			{
				StartingPawnUtility.ClearAllStartingPawns();
				for (int i = 0; i < pawnCount; i++)
				{
					Find.GameInitData.startingAndOptionalPawns.Add(StartingPawnUtility.NewGeneratedStartingPawn());
				}
				num++;
			}
			while (num <= 20 && !StartingPawnUtility.WorkTypeRequirementsSatisfied());
			while (Find.GameInitData.startingAndOptionalPawns.Count < pawnCount)
			{
				Find.GameInitData.startingAndOptionalPawns.Add(StartingPawnUtility.NewGeneratedStartingPawn());
			}
		}
		public override string Summary(Scenario scen)
		{
			return "SS.ScenPart_SalvagedStart".Translate();
		}

		public static List<CompTransporter> transporters = new List<CompTransporter>();
		public override void GenerateIntoMap(Map map)
		{
			foreach (var transporter in transporters)
            {
				transporter.parent.SetFactionDirect(Faction.OfPlayer);
				foreach (var pawn in transporter.innerContainer.OfType<Pawn>())
                {
					pawn.SetFactionDirect(Faction.OfPlayer);
					if (pawn.ideo != null)
                    {
						if (pawn.Faction != null && pawn.Faction.ideos.PrimaryIdeo != null)
						{
							pawn.ideo.SetIdeo(pawn.Faction.ideos.PrimaryIdeo);
						}
						else
						{
							pawn.ideo.SetIdeo(Find.IdeoManager.IdeosListForReading.RandomElement());
						}
					}
				}

				if (SRTSHelper.TryArrive(transporter, map) || ModsConfig.IsActive("kentington.saveourship2") && SoSHelper.TryArrive(transporter, map))
				{
					continue;
				}
			}
		}
	}
}
