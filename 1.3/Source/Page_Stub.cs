using RimWorld;
using UnityEngine;

namespace SalvagedStart
{
    public class Page_Stub : Page
    {
        public override void DoWindowContents(Rect inRect)
        {
			this.DoNext();
		}
        public override void PostOpen()
        {
            base.PostOpen();
			this.DoNext();
        }
    }
}
