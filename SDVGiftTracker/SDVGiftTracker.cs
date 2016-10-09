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

using System.IO;


namespace SDVGiftTracker
{
    public class SDVGiftTracker : Mod
    {
        private static GiftTasteManager GiftManager;

        private GiftTrackerConfig ModConfig { get; set; }

        private string DirectoryName => Path.Combine(PathOnDisk, "giftdata");

        public override void Entry(params object[] objects)
        {
            PlayerEvents.InventoryChanged += OnInventoryChanged;
            PlayerEvents.LoadedGame += OnGameLoaded;

            // save learned gift tastes at the end of the day, when the game saves
            TimeEvents.OnNewDay += (object sender, EventArgsNewDay e) =>
            {
                // only save once per day (newDay will be true the second time, and on new games)
                if (!Game1.newDay && null != GiftManager)
                {
                    GiftManager.UpdateGiftData();
                }
            };

            ModConfig = new GiftTrackerConfig().InitializeConfig(BaseConfigPath);

            Command.RegisterCommand("list_gifttastes", "List all learned gift tastes").CommandFired += list_gifttastes;

            if(!Directory.Exists(DirectoryName))
            {
                Directory.CreateDirectory(DirectoryName);
            }

            Log.Out("Gift Tracker entry");
        }

        private static void OnInventoryChanged(object sender, EventArgsInventoryChanged e)
        {
            Log.Out("Player inventory changed");

            Item gift;

            // if an item was deducted (will be either removed or a negative quantity change)
            // assume it's the first item in either list, gifts only get given one at a time
            if(e.Removed.Count() > 0 ||
                (e.QuantityChanged.Count() > 0 && e.QuantityChanged[0].StackChange < 0))
            {
                // get the first item
                gift = (e.Removed.Count() > 0 ? e.Removed : e.QuantityChanged)[0].Item;
            }

            else
            {
                return;
            }
            if (Game1.activeClickableMenu is DialogueBox)
            {
                DialogueBox dbox = (DialogueBox)Game1.activeClickableMenu;
                NPC recipient = Game1.currentSpeaker;

                // check if the dialogue box's current text
                // is among the speaker's possible reactions to a gift
                // i.e. this isn't a delivery
                if (dbox != null && recipient != null &&
                    Game1.NPCGiftTastes[recipient.name].Contains(dbox.getCurrentString()))
                {
                    GiftManager.Add(recipient.name, gift);
                }
            }
        }

        private void OnGameLoaded(object sender, EventArgs e)
        {
            // initialize manager
            GiftManager = new GiftTasteManager(ModConfig, 
                Path.Combine(DirectoryName, Constants.SaveFolderName + ".json"));
        }

        // todo: can't build against XNA, so no location-based stuff but make this an in-game thing
        private void list_gifttastes(object sender, EventArgsCommand e)
        {
            Log.Out(GiftManager.GetGiftData(e.Command.CalledArgs));
        }
    }
}
