using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SalvagedStart
{
    [HarmonyPatch(typeof(PageUtility), nameof(PageUtility.StitchedPages))]
	public static class PageUtility_StitchedPages_Patch
	{
		public static void Prefix(ref IEnumerable<Page> pages)
		{
			var list = pages.ToList();
			var loadShipsPage = list.FirstOrDefault(x => x is Page_LoadShips); 
			if (loadShipsPage != null)
            {
				list.Remove(loadShipsPage);
				list.Insert(0, loadShipsPage);
				list.Add(new Page_Stub());
			}
			pages = list;
		}
	}
}
