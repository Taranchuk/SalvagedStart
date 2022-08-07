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
    public class ScenPart_KeepResearchProgress : ScenPart
    {
		public override string Summary(Scenario scen)
		{
			return "SS.ScenPart_SalvagedStart_KeepResearch".Translate();
		}

		public static ResearchManager prevResearchManager;
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
			Find.ResearchManager.currentProj = prevResearchManager.currentProj;
			foreach (var progress in prevResearchManager.progress)
			{
				if (progress.Key != null)
				{
					Find.ResearchManager.progress[progress.Key] = progress.Value;
				}
			}

			foreach (var techprint in prevResearchManager.techprints)
			{
				if (techprint.Key != null)
				{
					Find.ResearchManager.techprints[techprint.Key] = techprint.Value;
				}
			}
		}
    }
}
