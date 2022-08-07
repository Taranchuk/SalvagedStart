using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SalvagedStart
{
	[HotSwappable]
	[HarmonyPatch(typeof(TransferableOneWayWidget), nameof(TransferableOneWayWidget.FillMainRect))]
	public static class TransferableOneWayWidget_FillMainRect_Patch
    {
		public static bool Prefix(TransferableOneWayWidget __instance, Rect mainRect, out bool anythingChanged)
        {
			anythingChanged = false;
			var window = Find.WindowStack.WindowOfType<Dialog_LoadShips>();
			if (window != null)
            {
				FillMainRect(window, __instance, mainRect, out anythingChanged);
				return false;
			}
			return true;
		}
		private static void FillMainRect(Dialog_LoadShips window, TransferableOneWayWidget __instance, Rect mainRect, out bool anythingChanged)
		{
			anythingChanged = false;
			Text.Font = GameFont.Small;
			if (__instance.AnyTransferable)
			{
				float num = 6f;
				for (int i = 0; i < __instance.sections.Count; i++)
				{
					num += (float)__instance.sections[i].cachedTransferables.Count * 30f;
					if (__instance.sections[i].title != null)
					{
						num += 30f;
					}
				}
				float curY = 6f;
				float availableMass = ((__instance.availableMassGetter != null) ? __instance.availableMassGetter() : float.MaxValue);
				Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, num);
				Widgets.BeginScrollView(mainRect, ref __instance.scrollPosition, viewRect);
				float num2 = __instance.scrollPosition.y - 30f;
				float num3 = __instance.scrollPosition.y + mainRect.height;
				for (int j = 0; j < __instance.sections.Count; j++)
				{
					List<TransferableOneWay> cachedTransferables = __instance.sections[j].cachedTransferables;
					if (!cachedTransferables.Any())
					{
						continue;
					}
					var takeAllButtonRect = new Rect(viewRect.width - 190, curY - 2, 75, 24);
					if (Widgets.ButtonText(takeAllButtonRect, "SS.TakeAll".Translate()))
					{
						for (int k = 0; k < cachedTransferables.Count; k++)
						{
							var transferrable = cachedTransferables[k];

							// non working version here
							//var toTransfer = availableMass + __instance.GetMass(transferrable.AnyThing) * (float)transferrable.CountToTransfer;
							//int threshold = ((!(toTransfer <= 0f)) ? Mathf.FloorToInt(toTransfer / __instance.GetMass(transferrable.AnyThing)) : 0);
							//if (transferrable.CanAdjustBy(threshold))
							//{
							//	transferrable.AdjustBy(threshold);
							//	window.CountToTransferChanged();
							//}

							if (transferrable.CanAdjustBy(transferrable.MaxCount) && (transferrable.ThingDef.GetStatValueAbstract(StatDefOf.Mass) * transferrable.MaxCount <  window.MassCapacity))
							{
								transferrable.AdjustBy(transferrable.MaxCount);
								window.CountToTransferChanged();
							}
						}
					}
					if (__instance.sections[j].title != null)
					{
						Widgets.ListSeparator(ref curY, viewRect.width, __instance.sections[j].title);
						curY += 5f;
					}

					for (int k = 0; k < cachedTransferables.Count; k++)
					{
						if (curY > num2 && curY < num3)
						{
							Rect rect = new Rect(0f, curY, viewRect.width, 30f);
							int countToTransfer = cachedTransferables[k].CountToTransfer;
							__instance.DoRow(rect, cachedTransferables[k], k, availableMass);
							if (countToTransfer != cachedTransferables[k].CountToTransfer)
							{
								anythingChanged = true;
							}
						}
						curY += 30f;
					}
				}
				Widgets.EndScrollView();
			}
			else
			{
				GUI.color = Color.gray;
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(mainRect, "NoneBrackets".Translate());
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
		}
	}

}
