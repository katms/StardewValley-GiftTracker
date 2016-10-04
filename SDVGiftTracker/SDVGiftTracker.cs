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
        private static GiftTasteManager GiftManager;

        public override void Entry(params object[] objects)
        {
            PlayerEvents.InventoryChanged += OnInventoryChanged;
            GameEvents.GameLoaded += OnGameLoaded;

            Log.Out("Gift Tracker entry");
        }

        private static void OnInventoryChanged(object sender, EventArgsInventoryChanged e)
        {
            Log.Out("Player inventory changed");

            List<ItemStackChange> Removed = e.Removed;

            if (Game1.activeClickableMenu is DialogueBox)
            {
                DialogueBox dbox = (DialogueBox)Game1.activeClickableMenu;
                NPC recipient = Game1.currentSpeaker;

                // check if at least one item was removed
                // and if the dialogue box's current text is among the speaker's possible reactions to a gift
                // i.e. this isn't a delivery
                if (dbox != null && recipient != null && Removed.Count() > 0 &&
                    Game1.NPCGiftTastes[recipient.name].Contains(dbox.getCurrentString()))
                {
                    GiftManager.Add(recipient.name, Removed[0].Item);
                }
            }
        }

        private void OnGameLoaded(object sender, EventArgs e)
        {
            GiftManager = new GiftTasteManager();
        }
    }
}
