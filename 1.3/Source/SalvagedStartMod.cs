using HarmonyLib;
using UnityEngine;
using Verse;

namespace SalvagedStart
{
    public class SalvagedStartMod : Mod
    {
        public static SalvagedStartSettings settings;
        public SalvagedStartMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<SalvagedStartSettings>();
            new Harmony("SalvagedStart.Mod").PatchAll();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }
    }

    public class SalvagedStartSettings : ModSettings
    {
        public float shipCrashChance;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shipCrashChance, "shipCrashChance");
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.SliderLabeled("SS.CrashChanceOfShips".Translate(), ref shipCrashChance, shipCrashChance.ToStringPercent());
            listingStandard.End();
        }
    }
}
