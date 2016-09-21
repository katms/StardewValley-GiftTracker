using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Inheritance;
using StardewValley;
using StardewValley.Menus;

namespace SDVGiftTracker
{
    public class SDVGiftTracker : Mod
    {
        private static Dictionary<string, List<Item>> KnownTastes = new Dictionary<string, List<Item>>();
        private static Dictionary<string, List<Item>> AllTastes = new Dictionary<string, List<Item>>();
        private static bool log = true;


        public override void Entry(params object[] objects)
        {
            PlayerEvents.InventoryChanged += OnInventoryChanged;

            Log.Out("Gift Tracker entry");
        }

        private static void OnInventoryChanged(object sender, EventArgs e)
        {
            if (log)
            {
                LogTasteDict();
                log = false;
            }
            if (e is EventArgsInventoryChanged)
            {
                Log.Out("Player inventory changed");
                EventArgsInventoryChanged eIC = (EventArgsInventoryChanged)e;
                List<ItemStackChange> Removed = eIC.Removed;
                foreach(var isc in Removed)
                {
                    Log.Out(isc.Item.Name);
                }
                if(Game1.activeClickableMenu is DialogueBox)
                {
                    Log.Out("Dialogue box is open on inventory removed");
                }
            }
            else
            {
                Log.Out("Player inventory changed, wrong e-type");
            }
        }

        static private void LogTasteDict()
        {
            foreach (var pair in Game1.NPCGiftTastes)
            {
                Log.Out("Taste key: " + pair.Key + "\n" + "Taste value: " + pair.Value);
            }
        }
    }
}
